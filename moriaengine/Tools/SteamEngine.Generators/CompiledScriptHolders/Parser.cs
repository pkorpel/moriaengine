using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledScriptHolders;

internal static class Parser {
	internal static ScriptHolderCandidate? DiscoverCandidate(
		Compilation compilation,
		MethodDeclarationSyntax methodDeclaration,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {
		var semanticModel = compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
		if (semanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol) {
			return null;
		}

		var canonicalAttribute = AttributeUsageValidator.FindAttributeOnSymbol(
			methodSymbol,
			symbols.Attributes.SteamFunctionAttribute,
			diagnostics);
		if (canonicalAttribute is null) {
			return null;
		}

		var candidate = new ScriptHolderCandidate(
			methodSymbol,
			GetFunctionName(methodSymbol, canonicalAttribute, diagnostics),
			GetDescription(methodSymbol));
		return candidate;
	}

	private static string GetFunctionName(IMethodSymbol methodSymbol, AttributeData canonicalAttribute, ICollection<Diagnostic> diagnostics) {
		if (canonicalAttribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax) {
			return methodSymbol.Name;
		}

		if (attributeSyntax.ArgumentList is null) {
			return methodSymbol.Name;
		}

		foreach (var argument in attributeSyntax.ArgumentList.Arguments) {
			if (argument.NameEquals is null) {
				if (TryReadStringLiteral(argument.Expression, out var positionalName)) {
					return positionalName;
				}

				ReportInvalidNameLiteral(argument.Expression);
				continue;
			}

			if (argument.NameEquals.Name.Identifier.ValueText != "NewFunctionName") {
				continue;
			}

			if (TryReadStringLiteral(argument.Expression, out var namedName)) {
				return namedName;
			}

			ReportInvalidNameLiteral(argument.Expression);
		}

		return methodSymbol.Name;

		void ReportInvalidNameLiteral(ExpressionSyntax expression) {
			diagnostics.Add(Diagnostic.Create(
				Analyzer.InvalidSteamFunctionNameArgumentDescriptor,
				expression.GetLocation(),
				expression.ToString()));
		}
	}

	private static bool TryReadStringLiteral(ExpressionSyntax expression, out string value) {
		if (expression is LiteralExpressionSyntax literal
			&& literal.IsKind(SyntaxKind.StringLiteralExpression)
			&& !string.IsNullOrWhiteSpace(literal.Token.ValueText)) {
			value = literal.Token.ValueText;
			return true;
		}

		value = string.Empty;
		return false;
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
