using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.DeepCopyImplementors;

internal static class Parser {
	internal static DeepCopyCandidate? DiscoverCandidate(
		Compilation compilation,
		TypeDeclarationSyntax declaration,
		global::SteamEngine.Generators.RequiredSymbols requiredSymbols,
		ICollection<Diagnostic> diagnostics) {
		var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
		if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) {
			return null;
		}
		if (symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct)) {
			return null;
		}

		var hasCanonicalAttribute = AttributeUsageValidator.HasCanonicalAttribute(
			symbol,
			requiredSymbols.Attributes.DeepCopyableClassAttribute,
			diagnostics);

		if (!hasCanonicalAttribute) {
			return null;
		}

		return new DeepCopyCandidate(symbol);
	}

}
