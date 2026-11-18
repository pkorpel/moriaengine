using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.DeepCopyImplementors;

internal static class Parser {
	private const string DeepCopyableClassAttributeSimpleName = "DeepCopyableClass";

	internal static DeepCopyDiscoveryResult? DiscoverCandidate(GeneratorSyntaxContext context) {
		var diagnostics = new List<Diagnostic>();
		if (context.Node is not TypeDeclarationSyntax declaration) {
			return null;
		}
		if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) {
			return null;
		}
		if (symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct)) {
			return null;
		}

		var canonicalAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(Analyzer.DeepCopyableClassAttributeMetadataName);
		var hasCanonicalAttribute = false;
		foreach (var attribute in symbol.GetAttributes()) {
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null) {
				continue;
			}

			var simpleName = StripAttributeSuffix(attributeClass.Name);
			if (!string.Equals(simpleName, DeepCopyableClassAttributeSimpleName, StringComparison.Ordinal)) {
				continue;
			}

			if (canonicalAttribute is not null && SymbolEqualityComparer.Default.Equals(attributeClass, canonicalAttribute)) {
				hasCanonicalAttribute = true;
				continue;
			}

			var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
				?? symbol.Locations.FirstOrDefault()
				?? Location.None;
			diagnostics.Add(Diagnostic.Create(
				Analyzer.AttributeShadowingDescriptor,
				location,
				simpleName,
				attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				Analyzer.DeepCopyableClassAttributeMetadataName));
		}

		if (!hasCanonicalAttribute) {
			return diagnostics.Count == 0 ? null : new DeepCopyDiscoveryResult(null, diagnostics);
		}

		return new DeepCopyDiscoveryResult(new DeepCopyCandidate(symbol), diagnostics);
	}

	internal static string StripAttributeSuffix(string name) {
		return name.EndsWith("Attribute", StringComparison.Ordinal)
			? name.Substring(0, name.Length - "Attribute".Length)
			: name;
	}
}
