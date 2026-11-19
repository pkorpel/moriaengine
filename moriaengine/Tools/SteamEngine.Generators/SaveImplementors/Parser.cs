using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.SaveImplementors;

internal static class Parser {
	internal static SaveImplementorCandidate? DiscoverCandidate(
		Compilation compilation,
		TypeDeclarationSyntax declaration,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {
		var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
		if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) {
			return null;
		}
		if (symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct)) {
			return null;
		}

		var hasCanonicalAttribute = AttributeUsageValidator.HasAttribute(
			symbol,
			symbols.Attributes.SaveableClassAttribute,
			diagnostics);

		if (!hasCanonicalAttribute) {
			return null;
		}

		return new SaveImplementorCandidate(symbol);
	}

}
