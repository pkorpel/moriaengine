using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.ThingAndPluginClasses;

internal static class Parser {
	private static readonly string DefFieldAttributeName = AttributeUsageValidator.StripAttributeSuffix("DefFieldAttribute");

	internal static TemplateCandidate? DiscoverCandidate(
		Compilation compilation,
		ClassDeclarationSyntax declaration,
		global::SteamEngine.Generators.RequiredSymbols requiredSymbols,
		ICollection<Diagnostic> diagnostics) {
		var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
		if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) {
			return null;
		}

		var classification = ClassifyTemplate(symbol, declaration, requiredSymbols, diagnostics);
		if (classification is null) {
			return null;
		}

		return new TemplateCandidate(
			symbol,
			classification.Value.Kind,
			classification.Value.Family,
			classification.Value.CopyAllowed,
			compilation);
	}

	private static (TemplateKind Kind, TemplateFamily Family, bool? CopyAllowed)? ClassifyTemplate(
		INamedTypeSymbol symbol,
		ClassDeclarationSyntax declaration,
		global::SteamEngine.Generators.RequiredSymbols requiredSymbols,
		ICollection<Diagnostic> diagnostics) {
		var thingTemplate = AttributeUsageValidator.FindCanonicalAttribute(symbol, requiredSymbols.Attributes.ThingTemplateAttribute, diagnostics);
		if (thingTemplate is not null) {
			return (TemplateKind.Instance, TemplateFamily.Thing, GetTemplateCopySetting(thingTemplate, diagnostics));
		}

		var thingDefTemplate = AttributeUsageValidator.FindCanonicalAttribute(symbol, requiredSymbols.Attributes.ThingDefTemplateAttribute, diagnostics);
		if (thingDefTemplate is not null) {
			return (TemplateKind.Def, TemplateFamily.Thing, null);
		}

		var pluginTemplate = AttributeUsageValidator.FindCanonicalAttribute(symbol, requiredSymbols.Attributes.PluginTemplateAttribute, diagnostics);
		if (pluginTemplate is not null) {
			return (TemplateKind.Instance, TemplateFamily.Plugin, GetTemplateCopySetting(pluginTemplate, diagnostics));
		}

		var pluginDefTemplate = AttributeUsageValidator.FindCanonicalAttribute(symbol, requiredSymbols.Attributes.PluginDefTemplateAttribute, diagnostics);
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

	private static string GetAttributeSimpleName(NameSyntax nameSyntax) {
		var name = nameSyntax switch {
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
			AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.ValueText,
			_ => nameSyntax.ToString()
		};
		return AttributeUsageValidator.StripAttributeSuffix(name);
	}

	internal static IEnumerable<DefFieldSpec> GetDefFields(
		INamedTypeSymbol? defSymbol,
		Compilation compilation,
		INamedTypeSymbol expectedDefFieldAttribute,
		ICollection<Diagnostic> diagnostics) {
		if (defSymbol is null) {
			return Array.Empty<DefFieldSpec>();
		}

		var fields = new List<DefFieldSpec>();
		foreach (var syntaxRef in defSymbol.DeclaringSyntaxReferences) {
			if (syntaxRef.GetSyntax() is not ClassDeclarationSyntax classDeclaration) {
				continue;
			}
			var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
			fields.AddRange(GetDefFields(classDeclaration, semanticModel, expectedDefFieldAttribute, diagnostics));
		}
		return fields;
	}

	internal static string? GetInitializer(IFieldSymbol field) {
		foreach (var syntaxRef in field.DeclaringSyntaxReferences) {
			if (syntaxRef.GetSyntax() is VariableDeclaratorSyntax declarator && declarator.Initializer?.Value is { } value) {
				return value.ToString();
			}
		}
		return null;
	}

	internal static string? GetInitializer(IPropertySymbol property) {
		foreach (var syntaxRef in property.DeclaringSyntaxReferences) {
			if (syntaxRef.GetSyntax() is PropertyDeclarationSyntax declaration && declaration.Initializer?.Value is { } value) {
				return value.ToString();
			}
		}
		return null;
	}

	private static IEnumerable<DefFieldSpec> GetDefFields(
		ClassDeclarationSyntax classDeclaration,
		SemanticModel semanticModel,
		INamedTypeSymbol expectedDefFieldAttribute,
		ICollection<Diagnostic> diagnostics) {
		var fields = new List<DefFieldSpec>();
		foreach (var field in classDeclaration.Members.OfType<FieldDeclarationSyntax>()) {
			var attribute = FindCanonicalDefFieldAttribute(field, semanticModel, expectedDefFieldAttribute, diagnostics);
			if (attribute is null) {
				continue;
			}
			var spec = ParseDefField(attribute, diagnostics);
			foreach (var variable in field.Declaration.Variables) {
				fields.Add(new DefFieldSpec(variable.Identifier.ValueText, spec.TypeName, spec.DefaultExpression));
			}
		}
		return fields;
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

	private static AttributeSyntax? FindCanonicalDefFieldAttribute(
		FieldDeclarationSyntax fieldDeclaration,
		SemanticModel semanticModel,
		INamedTypeSymbol expectedDefFieldAttribute,
		ICollection<Diagnostic> diagnostics) {
		var firstVariable = fieldDeclaration.Declaration.Variables.FirstOrDefault();
		if (firstVariable is null
			|| semanticModel.GetDeclaredSymbol(firstVariable) is not IFieldSymbol fieldSymbol) {
			return null;
		}

		var canonicalAttribute = AttributeUsageValidator.FindCanonicalAttribute(fieldSymbol, expectedDefFieldAttribute, diagnostics);
		if (canonicalAttribute?.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax canonicalAttributeSyntax) {
			return canonicalAttributeSyntax;
		}
		return null;
	}
}
