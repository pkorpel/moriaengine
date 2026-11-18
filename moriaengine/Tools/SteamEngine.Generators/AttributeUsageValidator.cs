using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class AttributeUsageValidator {
	internal static readonly DiagnosticDescriptor AttributeShadowingDescriptor = new(
		"SEGEN902",
		"Shadowed generator attribute",
		"Attribute '{0}' resolves to '{1}', but this generator expects '{2}'",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static string StripAttributeSuffix(string name) {
		return name.EndsWith("Attribute", StringComparison.Ordinal)
			? name.Substring(0, name.Length - "Attribute".Length)
			: name;
	}

	internal static bool HasCanonicalAttribute(
		ISymbol symbol,
		INamedTypeSymbol? canonicalAttribute,
		ICollection<Diagnostic> diagnostics) {
		return FindCanonicalAttribute(symbol, canonicalAttribute, diagnostics) is not null;
	}

	internal static AttributeData? FindCanonicalAttribute(
		ISymbol symbol,
		INamedTypeSymbol? canonicalAttribute,
		ICollection<Diagnostic> diagnostics) {
		if (canonicalAttribute is null) {
			return null;
		}

		var expectedSimpleName = StripAttributeSuffix(canonicalAttribute.Name);
		var expectedCanonicalTypeName = canonicalAttribute.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		AttributeData? foundCanonical = null;
		var canonicalDisplayName = canonicalAttribute.ToDisplayString();

		foreach (var attribute in symbol.GetAttributes()) {
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null) {
				continue;
			}

			var simpleName = StripAttributeSuffix(attributeClass.Name);
			if (!string.Equals(simpleName, expectedSimpleName, StringComparison.Ordinal)) {
				continue;
			}

			if (SymbolEqualityComparer.Default.Equals(attributeClass, canonicalAttribute)
				|| string.Equals(attributeClass.ToDisplayString(), canonicalDisplayName, StringComparison.Ordinal)) {
				foundCanonical ??= attribute;
				continue;
			}

			var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
				?? symbol.Locations.FirstOrDefault();
			diagnostics.Add(Diagnostic.Create(
				AttributeShadowingDescriptor,
				location,
				expectedSimpleName,
				attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				expectedCanonicalTypeName));
		}

		return foundCanonical;
	}
}
