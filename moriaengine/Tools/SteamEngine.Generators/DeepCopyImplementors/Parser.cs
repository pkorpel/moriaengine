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

		var diagnosticsBeforeResolve = diagnostics.Count;

		// look for the [DeepCopyImplementation] method / constructor
		var initializerMethod = AttributedMemberResolver.FindMethodOrCtorWithAttribute(
			symbol,
			symbols.Attributes.DeepCopyImplementationAttribute,
			diagnostics,
			includeInheritedTypes: false);
		if (initializerMethod is null && diagnostics.Count == diagnosticsBeforeResolve) {
			diagnostics.Add(Diagnostic.Create(
				MissingDeepCopyImplementationDescriptor,
				symbol.Locations.FirstOrDefault(),
				symbol.ToDisplayString()));
		}

		var copyableMembers = AttributedMemberResolver.CollectFieldsAndProperties(
			symbol,
			symbols.Attributes.CopyableDataAttribute,
			diagnostics,
			symbol,
			includeInheritedTypes: false);
		if (initializerMethod is null || copyableMembers is null) {
			return null;
		}

		return new DeepCopyCandidate(
			symbol,
			initializerMethod,
			copyableMembers);
	}

}
