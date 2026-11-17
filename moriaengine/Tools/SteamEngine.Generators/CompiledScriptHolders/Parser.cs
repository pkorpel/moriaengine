using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledScriptHolders;

internal static class Parser {
	private const string SteamFunctionAttributeSimpleName = "SteamFunction";

	internal static ScriptHolderDiscoveryResult? DiscoverCandidate(GeneratorSyntaxContext context) {
		var diagnostics = new List<Diagnostic>();
		if (context.Node is not MethodDeclarationSyntax) {
			return null;
		}

		if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IMethodSymbol methodSymbol) {
			return null;
		}

		var canonicalAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(Analyzer.SteamFunctionAttributeMetadataName);
		if (canonicalAttribute is null) {
			return null;
		}

		AttributeData? steamFunctionAttribute = null;
		foreach (var attribute in methodSymbol.GetAttributes()) {
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null) {
				continue;
			}

			var simpleName = StripAttributeSuffix(attributeClass.Name);
			if (!string.Equals(simpleName, SteamFunctionAttributeSimpleName, StringComparison.Ordinal)) {
				continue;
			}

			if (SymbolEqualityComparer.Default.Equals(attributeClass, canonicalAttribute)) {
				steamFunctionAttribute = attribute;
				break;
			}

			var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
			if (location is null) {
				location = methodSymbol.Locations.Length > 0 ? methodSymbol.Locations[0] : Location.None;
			}
			diagnostics.Add(Diagnostic.Create(
				Analyzer.AttributeShadowingDescriptor,
				location,
				simpleName,
				attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				canonicalAttribute.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		}

		if (steamFunctionAttribute is null) {
			return diagnostics.Count == 0 ? null : new ScriptHolderDiscoveryResult(null, diagnostics);
		}

		var functionName = methodSymbol.Name;
		if (steamFunctionAttribute.ConstructorArguments.Length == 1
			&& steamFunctionAttribute.ConstructorArguments[0].Value is string configuredName
			&& !string.IsNullOrEmpty(configuredName)) {
			functionName = configuredName;
		}

		var candidate = new ScriptHolderCandidate(methodSymbol, functionName, GetDescription(methodSymbol));
		return new ScriptHolderDiscoveryResult(candidate, diagnostics);
	}

	internal static string StripAttributeSuffix(string name) {
		return name.EndsWith("Attribute", StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
	}

	private static string? GetDescription(IMethodSymbol methodSymbol) {
		var xml = methodSymbol.GetDocumentationCommentXml();
		if (string.IsNullOrWhiteSpace(xml)) {
			return null;
		}

		try {
			var doc = XDocument.Parse(xml);
			var summary = NormalizeDocText(doc.Root?.Element("summary")?.Value);
			var remarks = NormalizeDocText(doc.Root?.Element("remarks")?.Value);
			if (!string.IsNullOrEmpty(summary) && !string.IsNullOrEmpty(remarks)) {
				return summary + "\n" + remarks;
			}
			return summary ?? remarks;
		} catch {
			return null;
		}
	}

	private static string? NormalizeDocText(string? value) {
		if (value is null) {
			return null;
		}
		if (value.Trim().Length == 0) {
			return null;
		}

		return value.Replace("\r", " ").Replace("\n", " ").Trim();
	}
}
