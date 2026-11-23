using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.PluginTriggerDispatch;

// Parses concrete plugin subclasses into dispatch candidates and diagnostics.
internal static class Parser {
	internal static PluginTriggerParseResult? Parse(GeneratorSyntaxContext context, RequiredSymbols? symbols) {
		if (symbols is null) {
			return null;
		}
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

		var compilation = context.SemanticModel.Compilation;
		var pluginType = symbols.CanonicalTypes.PluginType;
		var scriptArgsType = symbols.CanonicalTypes.ScriptArgsType;
		// Candidate scope: concrete plugin subclasses only.
		if (typeSymbol.IsAbstract
			|| SymbolEqualityComparer.Default.Equals(typeSymbol, pluginType)
			|| !NestedTypeValidator.IsAssignableTo(typeSymbol, pluginType, includeSelf: true)) {
			return null;
		}

		var typeLocation = SourceLocationData.From(compilation, declaration.Identifier.GetLocation());
		var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		// Generated override lands on the same type, so all declarations must be partial.
		if (!AllDeclarationsArePartial(typeSymbol)) {
			var diagnostic = GeneratorDiagnosticData.Create(
				Analyzer.TypeMustBePartialDescriptor,
				typeLocation,
				fullTypeName);
			return new PluginTriggerParseResult(null, EquatableArray<GeneratorDiagnosticData>.From(new[] { diagnostic }));
		}

		// Generator does not support nested targets; warn and skip.
		if (typeSymbol.ContainingType is not null) {
			var diagnostic = GeneratorDiagnosticData.Create(
				NestedTypeValidator.NestedTypeNotSupportedDescriptor,
				typeLocation,
				fullTypeName);
			return new PluginTriggerParseResult(null, EquatableArray<GeneratorDiagnosticData>.From(new[] { diagnostic }));
		}

		var methods = EquatableArray<DispatchMethodModel>.From(
			DispatchModelBuilder.GetTriggerMethods(typeSymbol, publicOnly: false)
				.Select(method => DispatchModelBuilder.BuildMethod(method, scriptArgsType, scriptArgsParameterIndex: 0)));
		// No On_* methods means no dispatch to generate.
		if (methods.Length == 0) {
			return null;
		}

		var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: typeSymbol.ContainingNamespace.ToDisplayString();
		var model = new PluginTriggerModel(
			namespaceName,
			typeSymbol.Name,
			fullTypeName,
			methods);
		var candidate = new PluginTriggerCandidate(model, typeLocation);
		return new PluginTriggerParseResult(candidate, EquatableArray<GeneratorDiagnosticData>.Empty);
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

}
