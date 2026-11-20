using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.DeepCopyImplementors;

internal static class Parser {
	internal static readonly DiagnosticDescriptor MissingDeepCopyImplementationDescriptor = new(
		"SEGEN205",
		"Missing deep-copy implementation member",
		"Type '{0}' is marked with [DeepCopyableClass] but has no valid [DeepCopyImplementation] member",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static DeepCopyCandidate? DiscoverCandidate(
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
			symbols.Attributes.DeepCopyableClassAttribute,
			diagnostics);

		if (!hasCanonicalAttribute) {
			return null;
		}

		// look for the [DeepCopyImplementation] method / constructor
		var foundInitializerMethod = AttributedMemberResolver.TryFindMethodOrCtorWithAttribute(
			symbol,
			symbols.Attributes.DeepCopyImplementationAttribute,
			diagnostics,
			includeInheritedTypes: false,
			out var initializerMethod);
		if (!foundInitializerMethod) {
			return null;
		}
		if (initializerMethod is null) {
			diagnostics.Add(Diagnostic.Create(
				MissingDeepCopyImplementationDescriptor,
				symbol.Locations.FirstOrDefault(),
				symbol.ToDisplayString()));
		}

		var foundCopyableMembers = AttributedMemberResolver.TryCollectFieldsAndProperties(
			symbol,
			symbols.Attributes.CopyableDataAttribute,
			diagnostics,
			symbol,
			includeInheritedTypes: false,
			out var copyableMembers,
			includePrivateOrProtected: false);
		if (!foundCopyableMembers) {
			return null;
		}

		return new DeepCopyCandidate(
			symbol,
			initializerMethod,
			copyableMembers);
	}

}
