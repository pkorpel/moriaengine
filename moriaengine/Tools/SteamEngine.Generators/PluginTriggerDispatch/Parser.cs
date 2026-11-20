using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal static class Parser {
	internal static PluginTriggerParseResult? ParseCandidate(GeneratorSyntaxContext context) {
		// Syntax pre-filter keeps this fast, but keep an early guard for safety.
		if (context.Node is not ClassDeclarationSyntax declaration) {
			return null;
		}

		// Skip declarations that fail symbol binding (incomplete/invalid syntax).
		if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol) {
			return null;
		}

		// One parse result per type symbol, even if the type has multiple declarations.
		if (!IsPrimaryDeclaration(typeSymbol, declaration)) {
			return null;
		}

		var pluginType = context.SemanticModel.Compilation.GetTypeByMetadataName(MetadataNames.EngineTypes.Plugin);
		// Candidate scope: concrete plugin subclasses only.
		if (pluginType is null
			|| typeSymbol.IsAbstract
			|| SymbolEqualityComparer.Default.Equals(typeSymbol, pluginType)
			|| !NestedTypeValidator.IsAssignableTo(typeSymbol, pluginType, includeSelf: true)) {
			return null;
		}

		var diagnostics = ImmutableArray.CreateBuilder<PluginTriggerDiagnosticData>();
		var typeLocation = ToLocationData(declaration.Identifier.GetLocation());
		var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		// Generated override lands on the same type, so all declarations must be partial.
		if (!AllDeclarationsArePartial(typeSymbol)) {
			diagnostics.Add(new PluginTriggerDiagnosticData(
				PluginTriggerDiagnosticKind.TypeMustBePartial,
				typeLocation,
				fullTypeName,
				string.Empty));
			return new PluginTriggerParseResult(null, EquatableArray<PluginTriggerDiagnosticData>.From(diagnostics.ToImmutable()));
		}

		// Generator does not support nested targets; warn and skip.
		if (typeSymbol.ContainingType is not null) {
			diagnostics.Add(new PluginTriggerDiagnosticData(
				PluginTriggerDiagnosticKind.NestedTypeNotSupported,
				typeLocation,
				fullTypeName,
				string.Empty));
			return new PluginTriggerParseResult(null, EquatableArray<PluginTriggerDiagnosticData>.From(diagnostics.ToImmutable()));
		}

		var scriptArgsType = context.SemanticModel.Compilation.GetTypeByMetadataName(MetadataNames.EngineTypes.ScriptArgs);
		var methods = GetTriggerMethods(typeSymbol, scriptArgsType);
		// No On_* methods means no dispatch to generate.
		if (methods.Length == 0) {
			return null;
		}

		var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: typeSymbol.ContainingNamespace.ToDisplayString();
		var candidate = new PluginTriggerCandidate(
			typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			namespaceName,
			typeSymbol.Name,
			fullTypeName,
			typeLocation,
			EquatableArray<PluginTriggerMethodData>.From(methods));
		return new PluginTriggerParseResult(candidate, EquatableArray<PluginTriggerDiagnosticData>.From(diagnostics.ToImmutable()));
	}

	private static bool IsPrimaryDeclaration(INamedTypeSymbol typeSymbol, ClassDeclarationSyntax declaration) {
		var first = typeSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax())
			.OfType<ClassDeclarationSyntax>()
			.OrderBy(syntax => syntax.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(syntax => syntax.SpanStart)
			.FirstOrDefault();
		return ReferenceEquals(first, declaration);
	}

	private static bool AllDeclarationsArePartial(INamedTypeSymbol typeSymbol) {
		return typeSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax())
			.OfType<TypeDeclarationSyntax>()
			.All(syntax => syntax.Modifiers.Any(modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));
	}

	private static ImmutableArray<PluginTriggerMethodData> GetTriggerMethods(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol? scriptArgsType) {
		var methods = new List<IMethodSymbol>();

		void AddMethodsFromType(INamedTypeSymbol symbol, bool includeOverridden) {
			var isDeclaringType = SymbolEqualityComparer.Default.Equals(symbol, typeSymbol);
			foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>()) {
				if (method.MethodKind != MethodKind.Ordinary) {
					continue;
				}

				if (method.IsStatic) {
					continue;
				}

				if (!isDeclaringType && method.DeclaredAccessibility == Accessibility.Private) {
					continue;
				}

				if (!method.Name.StartsWith("On_", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!includeOverridden && IsOverriddenByExisting(methods, method)) {
					continue;
				}

				methods.Add(method);
			}
		}

		AddMethodsFromType(typeSymbol, includeOverridden: true);
		for (var current = typeSymbol.BaseType; current is not null; current = current.BaseType) {
			AddMethodsFromType(current, includeOverridden: false);
		}

		return methods
			.Select(method => BuildMethodData(method, scriptArgsType))
			.ToImmutableArray();
	}

	private static PluginTriggerMethodData BuildMethodData(IMethodSymbol method, INamedTypeSymbol? scriptArgsType) {
		var parameters = ImmutableArray.CreateBuilder<DispatchParameterModel>();
		for (var i = 0; i < method.Parameters.Length; i++) {
			if (i == 0
				&& method.Parameters.Length == 1
				&& scriptArgsType is not null
				&& DispatchModelBuilder.IsScriptArgsAssignable(method.Parameters[i].Type, scriptArgsType)) {
				parameters.Add(DispatchModelBuilder.BuildScriptArgsParameter(method.Parameters[i].Type, scriptArgsType));
				continue;
			}

			parameters.Add(DispatchModelBuilder.BuildInvocationParameter(method.Parameters[i].Type));
		}

		var triggerName = method.Name.Substring(3);
		return new PluginTriggerMethodData(
			triggerName,
			method.Name,
			method.ReturnsVoid,
			ToLocationData(method.Locations.FirstOrDefault()),
			EquatableArray<DispatchParameterModel>.From(parameters.ToImmutable()));
	}

	private static bool IsOverriddenByExisting(IEnumerable<IMethodSymbol> existingMethods, IMethodSymbol baseMethod) {
		foreach (var existing in existingMethods) {
			for (var current = existing; current is not null; current = current.OverriddenMethod) {
				if (SymbolEqualityComparer.Default.Equals(current, baseMethod)) {
					return true;
				}
			}
		}
		return false;
	}

	private static SourceLocationData ToLocationData(Location? location) {
		if (location is null || !location.IsInSource || location.SourceTree is null) {
			return new SourceLocationData(string.Empty, 0, 0, 0, 0);
		}

		var lineSpan = location.GetLineSpan();
		return new SourceLocationData(
			location.SourceTree.FilePath,
			lineSpan.StartLinePosition.Line,
			lineSpan.StartLinePosition.Character,
			lineSpan.EndLinePosition.Line,
			lineSpan.EndLinePosition.Character);
	}
}
