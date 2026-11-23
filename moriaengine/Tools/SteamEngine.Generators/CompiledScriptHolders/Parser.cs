using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledScriptHolders;

// Parses attributed methods into function candidates and declaration diagnostics.
internal static class Parser {
	internal static ScriptHolderParseResult? Parse(GeneratorSyntaxContext context, RequiredSymbols? symbols) {
		if (symbols is null) {
			return null;
		}
		if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IMethodSymbol methodSymbol) {
			return null;
		}
		var compilation = context.SemanticModel.Compilation;
		var attributeType = symbols.Attributes.SteamFunctionAttribute;
		var scriptArgsType = symbols.CanonicalTypes.ScriptArgsType;

		var diagnostics = new List<GeneratorDiagnosticData>();
		var expectedSimpleName = AttributeUsageValidator.GetAttributeDisplayString(attributeType);
		var canonicalAttribute = AttributeUsageValidator.FindAttributeOnSymbol(methodSymbol, attributeType, attribute => {
			diagnostics.Add(GeneratorDiagnosticData.Create(AttributeUsageValidator.AttributeShadowingDescriptor,
				SourceLocationData.From(compilation, attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? methodSymbol.Locations.FirstOrDefault()),
				expectedSimpleName, attribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				attributeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		});

		ScriptHolderCandidate? candidate = null;
		if (canonicalAttribute is not null) {
			var name = GetFunctionName(compilation, methodSymbol, canonicalAttribute, diagnostics);
			var location = SourceLocationData.From(compilation, methodSymbol.Locations.FirstOrDefault());
			if (!methodSymbol.IsStatic || methodSymbol.DeclaredAccessibility != Accessibility.Public) {
				diagnostics.Add(GeneratorDiagnosticData.Create(Analyzer.SteamFunctionMustBePublicStaticDescriptor, location, methodSymbol.ToDisplayString()));
			} else if (name is not null) {
				candidate = new ScriptHolderCandidate(
					methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), location,
					Analyzer.BuildModel(methodSymbol, name, GetDescription(methodSymbol), scriptArgsType));
			}
		}
		return candidate is null && diagnostics.Count == 0 ? null
			: new ScriptHolderParseResult(SourceLocationData.GetTreeIndex(compilation, context.Node.SyntaxTree), context.Node.SpanStart,
				candidate, EquatableArray<GeneratorDiagnosticData>.From(diagnostics));
	}

	private static string? GetFunctionName(Compilation compilation, IMethodSymbol methodSymbol, AttributeData canonicalAttribute, ICollection<GeneratorDiagnosticData> diagnostics) {
		var name = methodSymbol.Name;
		var location = methodSymbol.Locations.FirstOrDefault();
		if (canonicalAttribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax { ArgumentList: not null } attributeSyntax) {
			foreach (var argument in attributeSyntax.ArgumentList.Arguments) {
				if (argument.NameEquals is not null && argument.NameEquals.Name.Identifier.ValueText != "NewFunctionName") {
					continue;
				}
				if (argument.Expression is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression)) {
					diagnostics.Add(GeneratorDiagnosticData.Create(Analyzer.InvalidSteamFunctionNameArgumentDescriptor,
						SourceLocationData.From(compilation, argument.Expression.GetLocation()), argument.Expression.ToString()));
					return null;
				}
				name = literal.Token.ValueText;
				location = argument.Expression.GetLocation();
				break;
			}
		}
		if (!IsValidFunctionName(name)) {
			diagnostics.Add(GeneratorDiagnosticData.Create(Analyzer.InvalidSteamFunctionNameDescriptor,
				SourceLocationData.From(compilation, location), name));
			return null;
		}
		return name;
	}

	private static bool IsValidFunctionName(string name) {
		// Match LScript's ASCII symbol characters, with no leading digit.
		for (var i = 0; i < name.Length; i++) {
			var ch = name[i];
			if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_'
				|| (i > 0 && ch >= '0' && ch <= '9')) {
				continue;
			}
			return false;
		}
		return name.Length > 0;
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
