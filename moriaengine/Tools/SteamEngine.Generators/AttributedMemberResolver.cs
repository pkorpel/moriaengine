using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal readonly struct AttributedMember {
	internal AttributedMember(string name, ITypeSymbol type, Location? location) {
		Name = name;
		Type = type;
		Location = location;
	}

	internal string Name { get; }
	internal ITypeSymbol Type { get; }
	internal Location? Location { get; }
}

internal static class AttributedMemberResolver {
	internal static readonly DiagnosticDescriptor UnsupportedAttributedMemberDescriptor = new(
		"SEGEN905",
		"Unsupported attributed member",
		"Member '{0}' on '{1}' is marked with [{2}], but {3}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

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

	/// Resolves one attributed method, reporting invalid targets and duplicate attributed methods.
	internal static bool TryFindMethodOrCtorWithAttribute(
		INamedTypeSymbol parentClass,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		bool includeInheritedTypes,
		out IMethodSymbol? method,
		bool includePrivateOrProtected = true)
	{
		method = null;
		if (!TryListMembers(
			parentClass,
			expectedAttribute,
			diagnostics,
			includeInheritedTypes,
			out var members,
			includePrivateOrProtected)) {
			return false;
		}

		IMethodSymbol? foundMethod = null;

		foreach (var member in members)
		{
			if (member is not IMethodSymbol methodSymbol)
			{
				diagnostics.Add(Diagnostic.Create(
					InvalidAttributedMethodTargetDescriptor,
					member.Locations.FirstOrDefault(),
					member.ToDisplayString(),
					parentClass.ToDisplayString(),
					AttributeUsageValidator.GetAttributeDisplayString(expectedAttribute)));
				return false;
			}

			if (foundMethod is not null)
			{
				diagnostics.Add(Diagnostic.Create(
					DuplicateAttributedMethodsDescriptor,
					methodSymbol.Locations.FirstOrDefault() ?? parentClass.Locations.FirstOrDefault(),
					parentClass.ToDisplayString(),
					AttributeUsageValidator.GetAttributeDisplayString(expectedAttribute)));
				return false;
			}

			foundMethod = methodSymbol;
		}

		method = foundMethod;
		return true;
	}

	/// Lists members from the source type and optionally inherited members, respecting visibility and overrides.	
	internal static bool TryListMembers(
		INamedTypeSymbol parentClass,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		bool includeInheritedTypes,
		out IReadOnlyList<ISymbol> members,
		bool includePrivateOrProtected) { // specify true if this is to be called from within the same class
		var symbols = new List<ISymbol>();
		var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		var expectedAttributeDisplay = AttributeUsageValidator.GetAttributeDisplayString(expectedAttribute);

		bool AddMembers(INamedTypeSymbol symbol, bool excludePrivate, bool excludeProtected) {
			foreach (var member in symbol.GetMembers()) {
				if (member is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
					&& !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, parentClass)) {
					continue;
				}
				if (!AttributeUsageValidator.HasAttribute(member, expectedAttribute, diagnostics)) {
					continue;
				}
				if (excludePrivate && member.DeclaredAccessibility == Accessibility.Private) {
					ReportInvalidMember(
						diagnostics,
						member,
						parentClass,
						expectedAttributeDisplay,
						"private members are not accessible to generated code");
					return false;
				}
				if (excludeProtected && member.DeclaredAccessibility == Accessibility.Protected) {
					ReportInvalidMember(
						diagnostics,
						member,
						parentClass,
						expectedAttributeDisplay,
						"protected members are not accessible to generated code");
					return false;
				}
				if (IsOverriddenByExisting(member, symbols)) {
					continue;
				}

				if (seen.Add(member)) {
					symbols.Add(member);
				}
			}
			return true;
		}

		if (!AddMembers(parentClass, !includePrivateOrProtected, !includePrivateOrProtected)) {
			members = Array.Empty<ISymbol>();
			return false;
		}

		if (includeInheritedTypes)
		{
			for (var current = parentClass.BaseType; current is not null; current = current.BaseType)
			{
				// For base types, always exclude private members as they are not accessible from the declaring class
				if (!AddMembers(current, excludePrivate: true, excludeProtected: !includePrivateOrProtected)) {
					members = Array.Empty<ISymbol>();
					return false;
				}
			}
		}

		members = symbols;
		return true;
	}

	/// Checks whether a base member is already represented by an overriding implementation.
	private static bool IsOverriddenByExisting(ISymbol baseMember, List<ISymbol> existingMembers) {
		if (baseMember is IMethodSymbol baseMethod) {
			foreach (var existing in existingMembers.OfType<IMethodSymbol>()) {
				for (var current = existing; current is not null; current = current.OverriddenMethod) {
					if (SymbolEqualityComparer.Default.Equals(current, baseMethod)) {
						return true;
					}
				}
			}
			return false;
		}

		if (baseMember is IPropertySymbol baseProperty) {
			foreach (var existing in existingMembers.OfType<IPropertySymbol>()) {
				for (var current = existing; current is not null; current = current.OverriddenProperty) {
					if (SymbolEqualityComparer.Default.Equals(current, baseProperty)) {
						return true;
					}
				}
			}
		}

		return false;
	}

	/// Collects attributed fields and properties from the supplied or inherited member search.
	internal static bool TryCollectFieldsAndProperties(
		INamedTypeSymbol parentClass,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		INamedTypeSymbol containingType,
		bool includeInheritedTypes,
		out IReadOnlyList<AttributedMember> attributedMembers,
		bool includePrivateOrProtected = true) {

		attributedMembers = Array.Empty<AttributedMember>();
		if (!TryListMembers(
			parentClass,
			expectedAttribute,
			diagnostics,
			includeInheritedTypes,
			out var members,
			includePrivateOrProtected)) {
			return false;
		}
		var expectedAttributeDisplay = AttributeUsageValidator.GetAttributeDisplayString(expectedAttribute);
		var items = new List<AttributedMember>();
		foreach (var member in members) {
			if (member is IFieldSymbol fieldSymbol) {
				if (fieldSymbol.IsStatic || fieldSymbol.DeclaredAccessibility != Accessibility.Public) {
					ReportInvalidMember(
						diagnostics,
						fieldSymbol,
						containingType,
						expectedAttributeDisplay,
						"only public instance fields are supported");
					return false;
				}

				items.Add(new AttributedMember(
					fieldSymbol.Name,
					fieldSymbol.Type,
					fieldSymbol.Locations.FirstOrDefault()));
				continue;
			}

			if (member is IPropertySymbol propertySymbol) {
				var getterPublic = propertySymbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;
				var setterPublic = propertySymbol.SetMethod?.DeclaredAccessibility == Accessibility.Public;
				if (propertySymbol.IsStatic || propertySymbol.Parameters.Length != 0 || !getterPublic || !setterPublic) {
					ReportInvalidMember(
						diagnostics,
						propertySymbol,
						containingType,
						expectedAttributeDisplay,
						"only non-indexer public instance properties with public getter and setter are supported");
					return false;
				}

				items.Add(new AttributedMember(
					propertySymbol.Name,
					propertySymbol.Type,
					propertySymbol.Locations.FirstOrDefault()));
				continue;
			}

			ReportInvalidMember(
				diagnostics,
				member,
				containingType,
				expectedAttributeDisplay,
				"only fields and properties are supported");
			return false;
		}

		attributedMembers = items;
		return true;
	}

	private static void ReportInvalidMember(
		ICollection<Diagnostic> diagnostics,
		ISymbol member,
		INamedTypeSymbol containingType,
		string expectedAttributeDisplay,
		string reason) {
		diagnostics.Add(Diagnostic.Create(
			UnsupportedAttributedMemberDescriptor,
			member.Locations.FirstOrDefault(),
			member.ToDisplayString(),
			containingType.ToDisplayString(),
			expectedAttributeDisplay,
			reason));
	}
}
