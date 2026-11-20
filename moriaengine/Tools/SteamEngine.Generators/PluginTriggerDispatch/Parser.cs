using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal static class Parser {
	internal static PluginTriggerParseResult? ParseCandidate(GeneratorSyntaxContext context) {
		if (context.Node is not ClassDeclarationSyntax declaration) {
			return null;
		}

		if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol) {
			return null;
		}

		if (!IsPrimaryDeclaration(typeSymbol, declaration)) {
			return null;
		}

		var pluginType = context.SemanticModel.Compilation.GetTypeByMetadataName(MetadataNames.EngineTypes.Plugin);
		if (pluginType is null
			|| typeSymbol.IsAbstract
			|| SymbolEqualityComparer.Default.Equals(typeSymbol, pluginType)
			|| !IsAssignableTo(typeSymbol, pluginType)) {
			return null;
		}

		var diagnostics = ImmutableArray.CreateBuilder<PluginTriggerDiagnosticData>();
		var typeLocation = ToLocationData(declaration.Identifier.GetLocation());
		var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (!AllDeclarationsArePartial(typeSymbol)) {
			diagnostics.Add(new PluginTriggerDiagnosticData(
				PluginTriggerDiagnosticKind.TypeMustBePartial,
				typeLocation,
				fullTypeName,
				string.Empty));
			return new PluginTriggerParseResult(null, diagnostics.ToImmutable());
		}

		for (var containing = typeSymbol.ContainingType; containing is not null; containing = containing.ContainingType) {
			if (AllDeclarationsArePartial(containing)) {
				continue;
			}

			diagnostics.Add(new PluginTriggerDiagnosticData(
				PluginTriggerDiagnosticKind.ContainingTypeMustBePartial,
				typeLocation,
				containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				fullTypeName));
			return new PluginTriggerParseResult(null, diagnostics.ToImmutable());
		}

		var scriptArgsType = context.SemanticModel.Compilation.GetTypeByMetadataName(MetadataNames.EngineTypes.ScriptArgs);
		var methods = GetTriggerMethods(typeSymbol, scriptArgsType);
		if (methods.Length == 0) {
			return null;
		}

		var containingTypes = GetContainingTypes(typeSymbol);
		var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: typeSymbol.ContainingNamespace.ToDisplayString();
		var candidate = new PluginTriggerCandidate(
			typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			namespaceName,
			containingTypes,
			ToAccessibilityKeyword(typeSymbol.DeclaredAccessibility),
			typeSymbol.Name,
			fullTypeName,
			typeLocation,
			methods);
		return new PluginTriggerParseResult(candidate, diagnostics.ToImmutable());
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

	private static bool IsAssignableTo(INamedTypeSymbol source, INamedTypeSymbol targetBaseType) {
		for (var current = source; current is not null; current = current.BaseType) {
			if (SymbolEqualityComparer.Default.Equals(current, targetBaseType)) {
				return true;
			}
		}
		return false;
	}

	private static ImmutableArray<ContainingTypeData> GetContainingTypes(INamedTypeSymbol typeSymbol) {
		var types = new Stack<ContainingTypeData>();
		for (var containing = typeSymbol.ContainingType; containing is not null; containing = containing.ContainingType) {
			types.Push(new ContainingTypeData(
				ToAccessibilityKeyword(containing.DeclaredAccessibility),
				containing.IsStatic,
				containing.IsAbstract,
				containing.IsSealed,
				containing.Name));
		}
		return types.ToImmutableArray();
	}

	private static ImmutableArray<PluginTriggerMethodData> GetTriggerMethods(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol? scriptArgsType) {
		var methods = new List<IMethodSymbol>();

		void AddMethodsFromType(INamedTypeSymbol symbol, bool includeOverridden) {
			foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>()) {
				if (method.MethodKind != MethodKind.Ordinary) {
					continue;
				}

				if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public) {
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
		var parameters = ImmutableArray.CreateBuilder<PluginInvocationParameterData>();
		for (var i = 0; i < method.Parameters.Length; i++) {
			if (i == 0
				&& method.Parameters.Length == 1
				&& scriptArgsType is not null
				&& IsScriptArgsAssignable(method.Parameters[i].Type, scriptArgsType)) {
				parameters.Add(BuildScriptArgsParameter(method.Parameters[i].Type, scriptArgsType));
				continue;
			}

			parameters.Add(BuildInvocationParameter(method.Parameters[i].Type));
		}

		var triggerName = method.Name.Substring(3);
		return new PluginTriggerMethodData(
			triggerName,
			method.Name,
			method.ReturnsVoid,
			ToLocationData(method.Locations.FirstOrDefault()),
			parameters.ToImmutable());
	}

	private static PluginInvocationParameterData BuildScriptArgsParameter(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		if (SymbolEqualityComparer.Default.Equals(parameterType, scriptArgsType)) {
			return new PluginInvocationParameterData(
				true,
				PluginInvocationParameterConversion.None,
				string.Empty);
		}

		return new PluginInvocationParameterData(
			true,
			PluginInvocationParameterConversion.Cast,
			parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	private static bool IsScriptArgsAssignable(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		var current = parameterType;
		while (current is not null) {
			if (SymbolEqualityComparer.Default.Equals(current, scriptArgsType)) {
				return true;
			}
			current = (current as INamedTypeSymbol)?.BaseType;
		}
		return false;
	}

	private static PluginInvocationParameterData BuildInvocationParameter(ITypeSymbol parameterType) {
		if (IsNumericOrDateTime(parameterType)) {
			if (parameterType.TypeKind == TypeKind.Enum) {
				return new PluginInvocationParameterData(
					false,
					PluginInvocationParameterConversion.EnumConvertThenCast,
					parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			}

			return new PluginInvocationParameterData(
				false,
				PluginInvocationParameterConversion.Convert,
				parameterType.Name);
		}

		if (parameterType.SpecialType != SpecialType.System_Object) {
			return new PluginInvocationParameterData(
				false,
				PluginInvocationParameterConversion.Cast,
				parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		}

		return new PluginInvocationParameterData(
			false,
			PluginInvocationParameterConversion.None,
			string.Empty);
	}

	private static bool IsNumericOrDateTime(ITypeSymbol typeSymbol) {
		if (typeSymbol.TypeKind == TypeKind.Enum) {
			return true;
		}

		return typeSymbol.SpecialType switch {
			SpecialType.System_Boolean => true,
			SpecialType.System_Byte => true,
			SpecialType.System_SByte => true,
			SpecialType.System_Int16 => true,
			SpecialType.System_UInt16 => true,
			SpecialType.System_Int32 => true,
			SpecialType.System_UInt32 => true,
			SpecialType.System_Int64 => true,
			SpecialType.System_UInt64 => true,
			SpecialType.System_Single => true,
			SpecialType.System_Double => true,
			SpecialType.System_Decimal => true,
			_ => string.Equals(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "global::System.DateTime", StringComparison.Ordinal)
		};
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

	private static string ToAccessibilityKeyword(Accessibility accessibility) {
		return accessibility switch {
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			_ => "internal"
		};
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
