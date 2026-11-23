using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.SaveImplementors;

// Discovers saveable types and their attributed loading, saving, and data members.
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

		bool ResolveMethod(INamedTypeSymbol expectedAttribute, out IMethodSymbol? method, bool includeInheritedTypes = true) {
			return AttributedMemberResolver.TryFindMethodOrCtorWithAttribute(
				symbol,
				expectedAttribute,
				diagnostics,
				includeInheritedTypes: includeInheritedTypes,
				out method,
				includePrivateOrProtected: false); // can't access private from outside
		}

		if (!ResolveMethod(symbols.Attributes.LoadingInitializerAttribute, out var loadingInitializer)
			|| !ResolveMethod(symbols.Attributes.LoadSectionAttribute, out var loadSection, includeInheritedTypes: true)
			|| !ResolveMethod(symbols.Attributes.LoadLineAttribute, out var loadLine)
			|| !ResolveMethod(symbols.Attributes.LoadingFinalizerAttribute, out var loadingFinalizer)
			|| !ResolveMethod(symbols.Attributes.SaveAttribute, out var saveMethod)) {
			return null;
		}
		var foundSaveableMembers = AttributedMemberResolver.TryCollectFieldsAndProperties(
			symbol,
			symbols.Attributes.SaveableDataAttribute,
			diagnostics,
			symbol,
			includeInheritedTypes: true,
			out var saveableMembers,
			includePrivateOrProtected: false);

		if (!foundSaveableMembers) {
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
