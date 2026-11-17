using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.ThingAndPluginClasses;

internal static class Parser {
	private static readonly string ThingTemplateAttributeName = StripAttributeSuffix("ThingTemplateAttribute");
	private static readonly string ThingDefTemplateAttributeName = StripAttributeSuffix("ThingDefTemplateAttribute");
	private static readonly string PluginTemplateAttributeName = StripAttributeSuffix("PluginTemplateAttribute");
	private static readonly string PluginDefTemplateAttributeName = StripAttributeSuffix("PluginDefTemplateAttribute");
	private static readonly string DefFieldAttributeName = StripAttributeSuffix("DefFieldAttribute");

	internal static TemplateDiscoveryResult? DiscoverCandidate(GeneratorSyntaxContext context) {
		if (context.Node is not ClassDeclarationSyntax declaration) {
			return null;
		}
		if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) {
			return null;
		}

		var diagnostics = new List<Diagnostic>();
		var classification = ClassifyTemplate(context.SemanticModel, declaration, diagnostics);
		if (classification is null) {
			return diagnostics.Count == 0
				? null
				: new TemplateDiscoveryResult(null, diagnostics);
		}
		return new TemplateDiscoveryResult(
			new TemplateCandidate(
				symbol,
				classification.Value.Kind,
				classification.Value.Family,
				classification.Value.CopyAllowed,
				context.SemanticModel.Compilation),
			diagnostics);
	}

	private static (TemplateKind Kind, TemplateFamily Family, bool? CopyAllowed)? ClassifyTemplate(
		SemanticModel semanticModel,
		ClassDeclarationSyntax declaration,
		ICollection<Diagnostic> diagnostics) {
		foreach (var list in declaration.AttributeLists) {
			foreach (var attribute in list.Attributes) {
				var simpleName = GetAttributeSimpleName(attribute.Name);
				if (!TryMapTemplateAttribute(simpleName, out var expectedMetadataName, out var kind, out var family)) {
					continue;
				}

				var actualType = (semanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol)?.ContainingType;
				if (actualType is null) {
					var copyAllowedWithoutSymbol = kind == TemplateKind.Instance ? GetTemplateCopySetting(attribute, diagnostics) : (bool?) null;
					return (kind, family, copyAllowedWithoutSymbol);
				}

				var actualMetadataName = actualType.ToDisplayString();
				if (!string.Equals(actualMetadataName, expectedMetadataName, StringComparison.Ordinal)) {
					diagnostics.Add(CreateAttributeShadowingDiagnostic(attribute.GetLocation(), simpleName, actualType, expectedMetadataName ?? string.Empty));
					continue;
				}

				var copyAllowed = kind == TemplateKind.Instance ? GetTemplateCopySetting(attribute, diagnostics) : (bool?) null;
				return (kind, family, copyAllowed);
			}
		}
		return null;
	}

	private static Diagnostic CreateAttributeShadowingDiagnostic(Location location, string attributeName, INamedTypeSymbol actualType, string expectedTypeName) {
		return Diagnostic.Create(
			Analyzer.AttributeShadowingDescriptor,
			location,
			attributeName,
			actualType.ToDisplayString(),
			expectedTypeName);
	}

	private static bool TryMapTemplateAttribute(
		string simpleName,
		out string? expectedMetadataName,
		out TemplateKind kind,
		out TemplateFamily family) {
		expectedMetadataName = null;
		kind = default;
		family = default;
		if (simpleName == ThingTemplateAttributeName) {
			expectedMetadataName = Analyzer.ThingTemplateAttributeMetadataName;
			kind = TemplateKind.Instance;
			family = TemplateFamily.Thing;
			return true;
		}
		if (simpleName == ThingDefTemplateAttributeName) {
			expectedMetadataName = Analyzer.ThingDefTemplateAttributeMetadataName;
			kind = TemplateKind.Def;
			family = TemplateFamily.Thing;
			return true;
		}
		if (simpleName == PluginTemplateAttributeName) {
			expectedMetadataName = Analyzer.PluginTemplateAttributeMetadataName;
			kind = TemplateKind.Instance;
			family = TemplateFamily.Plugin;
			return true;
		}
		if (simpleName == PluginDefTemplateAttributeName) {
			expectedMetadataName = Analyzer.PluginDefTemplateAttributeMetadataName;
			kind = TemplateKind.Def;
			family = TemplateFamily.Plugin;
			return true;
		}
		return false;
	}

	private static bool GetTemplateCopySetting(AttributeSyntax attribute, ICollection<Diagnostic> diagnostics) {
		if (attribute.ArgumentList is null) {
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
		return StripAttributeSuffix(name);
	}

	internal static string StripAttributeSuffix(string name) {
		return name.EndsWith("Attribute", StringComparison.Ordinal)
			? name.Substring(0, name.Length - "Attribute".Length)
			: name;
	}

	internal static IEnumerable<DefFieldSpec> GetDefFields(INamedTypeSymbol? defSymbol, Compilation compilation, ICollection<Diagnostic> diagnostics) {
		if (defSymbol is null) {
			return Array.Empty<DefFieldSpec>();
		}
		var expectedDefFieldAttribute = compilation.GetTypeByMetadataName(Analyzer.DefFieldAttributeMetadataName);
		if (expectedDefFieldAttribute is null) {
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
			var attribute = FindCanonicalDefFieldAttribute(field.AttributeLists, semanticModel, expectedDefFieldAttribute, diagnostics);
			if (attribute is null) {
				continue;
			}
			var spec = ParseDefField(attribute);
			foreach (var variable in field.Declaration.Variables) {
				fields.Add(new DefFieldSpec(variable.Identifier.ValueText, spec.TypeName, spec.DefaultExpression));
			}
		}
		return fields;
	}

	private static (string TypeName, string DefaultExpression) ParseDefField(AttributeSyntax attribute) {
		var typeName = "object";
		var defaultExpression = "default(object)";
		if (attribute.ArgumentList is null) {
			return (typeName, defaultExpression);
		}
		foreach (var arg in attribute.ArgumentList.Arguments) {
			ApplyDefFieldArgument(arg, ref typeName, ref defaultExpression);
		}
		return (typeName, defaultExpression);
	}

	private static void ApplyDefFieldArgument(AttributeArgumentSyntax arg, ref string typeName, ref string defaultExpression) {
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
		}
	}

	private static AttributeSyntax? FindCanonicalDefFieldAttribute(
		SyntaxList<AttributeListSyntax> lists,
		SemanticModel semanticModel,
		INamedTypeSymbol expectedDefFieldAttribute,
		ICollection<Diagnostic> diagnostics) {
		var expectedMetadataName = expectedDefFieldAttribute?.ToDisplayString() ?? string.Empty;
		foreach (var list in lists) {
			foreach (var attr in list.Attributes) {
				var simpleName = GetAttributeSimpleName(attr.Name);
				if (!string.Equals(simpleName, DefFieldAttributeName, StringComparison.Ordinal)) {
					continue;
				}

				var actualType = (semanticModel.GetSymbolInfo(attr).Symbol as IMethodSymbol)?.ContainingType;
				if (actualType is null) {
					return attr;
				}

				if (expectedDefFieldAttribute is not null
					&& (SymbolEqualityComparer.Default.Equals(actualType, expectedDefFieldAttribute)
						|| string.Equals(actualType.ToDisplayString(), expectedMetadataName, StringComparison.Ordinal))) {
					return attr;
				}

				if (expectedDefFieldAttribute is not null) {
					diagnostics.Add(CreateAttributeShadowingDiagnostic(attr.GetLocation(), DefFieldAttributeName, actualType, expectedMetadataName));
				}
			}
		}
		return null;
	}
}
