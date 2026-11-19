using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.ThingAndPluginClasses;

internal static class Parser {

	internal static TemplateCandidate? DiscoverCandidate(
		Compilation compilation,
		ClassDeclarationSyntax declaration,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {

		var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
		if (semanticModel.GetDeclaredSymbol(declaration) is not { } symbol) {
			return null;
		}

		var classification = ClassifyTemplate(symbol, symbols, diagnostics);
		if (classification is null) {
			return null;
		}
		IReadOnlyList<DefFieldSpec> defFields = classification.Value.Kind == TemplateKind.Def
			? GetDefFields(symbol, symbols.Attributes.DefFieldAttribute, diagnostics).ToList()
			: Array.Empty<DefFieldSpec>();
		// Capture textual initializers once in parser stage so analyzer stays symbol-policy only.
		var instanceFieldDefaultExpressions = GetInstanceFieldDefaultExpressions(symbol);

		return new TemplateCandidate(
			symbol,
			classification.Value.Kind,
			classification.Value.Family,
			classification.Value.CopyAllowed,
			defFields,
			instanceFieldDefaultExpressions);
	}

	private static (TemplateKind Kind, TemplateFamily Family, bool? CopyAllowed)? ClassifyTemplate(
		INamedTypeSymbol symbol,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {

		var thingTemplate = AttributeUsageValidator.FindAttributeOnSymbol(symbol, symbols.Attributes.ThingTemplateAttribute, diagnostics);
		if (thingTemplate is not null) {
			return (TemplateKind.Instance, TemplateFamily.Thing, GetTemplateCopySetting(thingTemplate, diagnostics));
		}

		var thingDefTemplate = AttributeUsageValidator.FindAttributeOnSymbol(symbol, symbols.Attributes.ThingDefTemplateAttribute, diagnostics);
		if (thingDefTemplate is not null) {
			return (TemplateKind.Def, TemplateFamily.Thing, null);
		}

		var pluginTemplate = AttributeUsageValidator.FindAttributeOnSymbol(symbol, symbols.Attributes.PluginTemplateAttribute, diagnostics);
		if (pluginTemplate is not null) {
			return (TemplateKind.Instance, TemplateFamily.Plugin, GetTemplateCopySetting(pluginTemplate, diagnostics));
		}

		var pluginDefTemplate = AttributeUsageValidator.FindAttributeOnSymbol(symbol, symbols.Attributes.PluginDefTemplateAttribute, diagnostics);
		if (pluginDefTemplate is not null) {
			return (TemplateKind.Def, TemplateFamily.Plugin, null);
		}

		return null;
	}

	private static bool GetTemplateCopySetting(AttributeData attributeData, ICollection<Diagnostic> diagnostics) {
		if (attributeData.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attribute
			|| attribute.ArgumentList is null) {
			return true;
		}

		foreach (var arg in attribute.ArgumentList.Arguments) {
			if (arg.NameEquals?.Name.Identifier.ValueText != "Copy") {
				continue;
			}
			if (arg.Expression.IsKind(SyntaxKind.TrueLiteralExpression)) {
				return true;
			}
			if (arg.Expression.IsKind(SyntaxKind.FalseLiteralExpression)) {
				return false;
			}
			diagnostics.Add(Diagnostic.Create(
				Analyzer.InvalidTemplateCopySettingDescriptor,
				arg.Expression.GetLocation(),
				arg.Expression.ToString()));
			return true;
		}
		return true;
	}

	private static IEnumerable<DefFieldSpec> GetDefFields(
		INamedTypeSymbol? defSymbol,
		INamedTypeSymbol expectedDefFieldAttribute,
		ICollection<Diagnostic> diagnostics) {

		if (defSymbol is null) {
			return Array.Empty<DefFieldSpec>();
		}

		var fields = new List<DefFieldSpec>();
		foreach (var fieldSymbol in defSymbol.GetMembers().OfType<IFieldSymbol>()) {
			if (!TryGetCanonicalDefFieldAttribute(fieldSymbol, expectedDefFieldAttribute, diagnostics, out var attributeSyntax)) {
				continue;
			}

			var spec = ParseDefField(attributeSyntax, diagnostics);
			fields.Add(new DefFieldSpec(fieldSymbol.Name, spec.TypeName, spec.DefaultExpression));
		}
		return fields;
	}

	private static IReadOnlyDictionary<ISymbol, string> GetInstanceFieldDefaultExpressions(INamedTypeSymbol symbol) {
		var initializers = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
		foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>()) {
			var initializer = GetInitializer(field);
			if (initializer is not null) {
				initializers[field] = initializer;
			}
		}
		foreach (var property in symbol.GetMembers().OfType<IPropertySymbol>()) {
			var initializer = GetInitializer(property);
			if (initializer is not null) {
				initializers[property] = initializer;
			}
		}
		return initializers;
	}

	private static string? GetInitializer(IFieldSymbol field) {
		foreach (var syntaxRef in field.DeclaringSyntaxReferences) {
			if (syntaxRef.GetSyntax() is VariableDeclaratorSyntax declarator && declarator.Initializer?.Value is { } value) {
				return value.ToString();
			}
		}
		return null;
	}

	private static string? GetInitializer(IPropertySymbol property) {
		foreach (var syntaxRef in property.DeclaringSyntaxReferences) {
			if (syntaxRef.GetSyntax() is PropertyDeclarationSyntax declaration && declaration.Initializer?.Value is { } value) {
				return value.ToString();
			}
		}
		return null;
	}

	private static (string TypeName, string DefaultExpression) ParseDefField(AttributeSyntax attribute, ICollection<Diagnostic> diagnostics) {
		var typeName = "object";
		var defaultExpression = "default(object)";
		if (attribute.ArgumentList is null) {
			return (typeName, defaultExpression);
		}
		foreach (var arg in attribute.ArgumentList.Arguments) {
			ApplyDefFieldArgument(arg, diagnostics, ref typeName, ref defaultExpression);
		}
		return (typeName, defaultExpression);
	}

	private static void ApplyDefFieldArgument(
		AttributeArgumentSyntax arg,
		ICollection<Diagnostic> diagnostics,
		ref string typeName,
		ref string defaultExpression) {
		if (arg.NameEquals is null) {
			if (arg.Expression is TypeOfExpressionSyntax typeOfExpression) {
				typeName = typeOfExpression.Type.ToString();
				defaultExpression = "default(" + typeName + ")";
			}
			return;
		}

		var isDefaultExpressionArg = arg.NameEquals.Name.Identifier.ValueText == "DefaultExpression";
		if (!isDefaultExpressionArg) {
			return;
		}
		if (arg.Expression is LiteralExpressionSyntax literal
			&& literal.IsKind(SyntaxKind.StringLiteralExpression)) {
			defaultExpression = literal.Token.ValueText;
			return;
		}

		diagnostics.Add(Diagnostic.Create(
			Analyzer.InvalidDefFieldDefaultExpressionDescriptor,
			arg.Expression.GetLocation(),
			arg.Expression.ToString()));
	}

	private static bool TryGetCanonicalDefFieldAttribute(
		IFieldSymbol fieldSymbol,
		INamedTypeSymbol expectedDefFieldAttribute,
		ICollection<Diagnostic> diagnostics,
		out AttributeSyntax attributeSyntax) {
		attributeSyntax = null!;

		var canonicalAttribute = AttributeUsageValidator.FindAttributeOnSymbol(fieldSymbol, expectedDefFieldAttribute, diagnostics);
		if (canonicalAttribute?.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax canonicalAttributeSyntax) {
			attributeSyntax = canonicalAttributeSyntax;
			return true;
		}
		return false;
	}
}
