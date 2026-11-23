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


	internal static string GetAttributeDisplayString(INamedTypeSymbol attribute) => StripAttributeSuffix(attribute.Name);

	internal static string StripAttributeSuffix(string name)
	{
		return name.EndsWith("Attribute", StringComparison.Ordinal)
			? name.Substring(0, name.Length - "Attribute".Length)
			: name;
	}

	internal static bool HasAttribute(
		ISymbol symbol,
		INamedTypeSymbol? canonicalAttribute,
		ICollection<Diagnostic> diagnostics) {
		return FindAttributeOnSymbol(symbol, canonicalAttribute, diagnostics) is not null;
	}

	/// <summary>
	/// Finds the attribute on the given symbol that matches the canonical attribute, if it exists.
	/// Reports diagnostics for any shadowed attributes.
	/// </summary>
	internal static AttributeData? FindAttributeOnSymbol(
		ISymbol symbol,
		INamedTypeSymbol? canonicalAttribute,
		ICollection<Diagnostic> diagnostics) {
		if (canonicalAttribute is null) {
			return null;
		}
		var expectedSimpleName = GetAttributeDisplayString(canonicalAttribute);
		var expectedCanonicalTypeName = canonicalAttribute.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		return FindAttributeOnSymbol(symbol, canonicalAttribute, attribute => {
			var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
				?? symbol.Locations.FirstOrDefault();
			diagnostics.Add(Diagnostic.Create(
				AttributeShadowingDescriptor,
				location,
				expectedSimpleName,
				attribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				expectedCanonicalTypeName));
		});
	}

	internal static AttributeData? FindAttributeOnSymbol(
		ISymbol symbol,
		INamedTypeSymbol? canonicalAttribute,
		Action<AttributeData> reportShadowed) {
		if (canonicalAttribute is null) {
			return null;
		}
		var expectedSimpleName = GetAttributeDisplayString(canonicalAttribute);
		AttributeData? foundCanonical = null;
		var canonicalDisplayName = canonicalAttribute.ToDisplayString();

		foreach (var attribute in symbol.GetAttributes()) {
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null) {
				continue;
			}

			var simpleName = GetAttributeDisplayString(attributeClass);
			if (!string.Equals(simpleName, expectedSimpleName, StringComparison.Ordinal)) {
				continue;
			}

			if (SymbolEqualityComparer.Default.Equals(attributeClass, canonicalAttribute)
				|| string.Equals(attributeClass.ToDisplayString(), canonicalDisplayName, StringComparison.Ordinal)) {
				foundCanonical ??= attribute;
				continue;
			}

			reportShadowed(attribute);
		}

		return foundCanonical;
	}
}
