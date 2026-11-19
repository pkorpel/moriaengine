using System.Collections.Generic;
using System.Linq;
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

		IMethodSymbol? ResolveMethod(INamedTypeSymbol expectedAttribute) {
			return AttributedMemberResolver.FindMethodOrCtorWithAttribute(
				symbol,
				expectedAttribute,
				diagnostics,
				includeInheritedTypes: true,
				includePrivateOrProtected: false); // can't access private from outside
		}

		var loadingInitializer = ResolveMethod(symbols.Attributes.LoadingInitializerAttribute);
		var loadSection = ResolveMethod(symbols.Attributes.LoadSectionAttribute);
		var loadLine = ResolveMethod(symbols.Attributes.LoadLineAttribute);
		var loadingFinalizer = ResolveMethod(symbols.Attributes.LoadingFinalizerAttribute);
		var saveMethod = ResolveMethod(symbols.Attributes.SaveAttribute);
		var saveableMembers = AttributedMemberResolver.CollectFieldsAndProperties(
			symbol,
			symbols.Attributes.SaveableDataAttribute,
			diagnostics,
			symbol,
			includeInheritedTypes: true,
			includePrivateOrProtected: true); // collect private members to report unsupported usage

		if (saveableMembers is null) {
			return null;
		}

		return new SaveImplementorCandidate(
			symbol,
			loadingInitializer,
			loadSection,
			loadLine,
			loadingFinalizer,
			saveMethod,
			saveableMembers);
	}

}
