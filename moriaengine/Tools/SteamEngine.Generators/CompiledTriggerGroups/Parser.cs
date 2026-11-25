using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledTriggerGroups;

// Parses eligible trigger-group classes into dispatch candidates and diagnostics.
internal static class Parser {
	internal static CompiledTriggerGroupParseResult? Parse(GeneratorSyntaxContext context, RequiredSymbols? symbols) {
		if (symbols is null) {
			return null;
		}
		var declaration = (ClassDeclarationSyntax) context.Node;
		if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol) {
			return null;
		}

		var first = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
		if (first is null || first.SyntaxTree != declaration.SyntaxTree || first.Span != declaration.Span) {
			return null;
		}

		var compilation = context.SemanticModel.Compilation;
		var baseType = symbols.CanonicalTypes.CompiledTriggerGroupType;
		var scriptArgsType = symbols.CanonicalTypes.ScriptArgsType;
		if (typeSymbol.IsAbstract || typeSymbol.IsSealed
			|| !NestedTypeValidator.IsAssignableTo(typeSymbol, baseType, includeSelf: false)) {
			return null;
		}

		var location = SourceLocationData.From(compilation, typeSymbol.Locations.First());
		var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		if (!typeSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax())
			.OfType<TypeDeclarationSyntax>()
			.All(syntax => syntax.Modifiers.Any(modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))) {
			return new CompiledTriggerGroupParseResult(null, GeneratorDiagnosticData.Create(Analyzer.TypeMustBePartialDescriptor, location, fullName));
		}

		if (typeSymbol.ContainingType is not null) {
			return new CompiledTriggerGroupParseResult(null, GeneratorDiagnosticData.Create(NestedTypeValidator.NestedTypeNotSupportedDescriptor, location, fullName));
		}

		var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: typeSymbol.ContainingNamespace.ToDisplayString();
		var methods = EquatableArray<DispatchMethodModel>.From(
			DispatchModelBuilder.GetTriggerMethods(typeSymbol, publicOnly: true)
				.Select(method => DispatchModelBuilder.BuildMethod(method, scriptArgsType, scriptArgsParameterIndex: 1)));
		var model = new CompiledTriggerGroupModel(namespaceName, typeSymbol.Name, methods);
		var candidate = new CompiledTriggerGroupCandidate(model, fullName, location);
		return new CompiledTriggerGroupParseResult(candidate, null);
	}

}
