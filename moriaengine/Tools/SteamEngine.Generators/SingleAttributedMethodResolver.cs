using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class SingleAttributedMethodResolver {
	internal static readonly DiagnosticDescriptor InvalidAttributedMethodTargetDescriptor = new(
		"SEGEN903",
		"Invalid attributed method target",
		"Member '{0}' on '{1}' is marked with [{2}] but only methods or constructors are supported",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor DuplicateAttributedMethodsDescriptor = new(
		"SEGEN904",
		"Multiple attributed methods",
		"Type '{0}' has multiple members marked with [{1}]",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static IMethodSymbol? Resolve(
		IEnumerable<ISymbol> members,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		INamedTypeSymbol typeSymbol,
		string attributeDisplayName) {
		var methods = new List<IMethodSymbol>();

		foreach (var member in members) {
			if (!AttributeUsageValidator.HasCanonicalAttribute(member, expectedAttribute, diagnostics)) {
				continue;
			}

			if (member is not IMethodSymbol methodSymbol) {
				diagnostics.Add(Diagnostic.Create(
					InvalidAttributedMethodTargetDescriptor,
					member.Locations.FirstOrDefault(),
					member.ToDisplayString(),
					typeSymbol.ToDisplayString(),
					attributeDisplayName));
				continue;
			}

			methods.Add(methodSymbol);
		}

		if (methods.Count > 1) {
			diagnostics.Add(Diagnostic.Create(
				DuplicateAttributedMethodsDescriptor,
				methods[1].Locations.FirstOrDefault() ?? typeSymbol.Locations.FirstOrDefault(),
				typeSymbol.ToDisplayString(),
				attributeDisplayName));
			return null;
		}

		if (methods.Count == 1) {
			return methods[0];
		}

		return null;
	}
}
