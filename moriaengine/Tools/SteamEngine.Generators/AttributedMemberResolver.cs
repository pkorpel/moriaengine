using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class AttributedMemberResolver {
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
	internal static IMethodSymbol? ResolveMethod(
		INamedTypeSymbol parentClass,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		bool includeInheritedTypes,
		bool includeNonPublic = true) {
		var members = includeInheritedTypes
			? GetAttributedSearchMembers(parentClass, includeNonPublic)
			: parentClass.GetMembers();
		var methods = new List<IMethodSymbol>();
		var attributeDisplayName = expectedAttribute.Name.EndsWith("Attribute", StringComparison.Ordinal)
			? expectedAttribute.Name.Substring(0, expectedAttribute.Name.Length - "Attribute".Length)
			: expectedAttribute.Name;

		foreach (var member in members) {
			if (!AttributeUsageValidator.HasAttribute(member, expectedAttribute, diagnostics)) {
				continue;
			}

			if (member is not IMethodSymbol methodSymbol) {
				diagnostics.Add(Diagnostic.Create(
					InvalidAttributedMethodTargetDescriptor,
					member.Locations.FirstOrDefault(),
					member.ToDisplayString(),
					parentClass.ToDisplayString(),
					AttributeUsageValidator.GetAttributeDisplayString(expectedAttribute)));
				continue;
			}

			methods.Add(methodSymbol);
		}

		if (methods.Count > 1) {
			diagnostics.Add(Diagnostic.Create(
				DuplicateAttributedMethodsDescriptor,
				methods[1].Locations.FirstOrDefault() ?? parentClass.Locations.FirstOrDefault(),
				parentClass.ToDisplayString(),
				attributeDisplayName));
			return null;
		}

		if (methods.Count == 1) {
			return methods[0];
		}
		return null;
	}

	/// Collects members from the source type and public inherited members for attributed-member resolution.
	internal static IEnumerable<ISymbol> GetAttributedSearchMembers(
		INamedTypeSymbol parentClass,
		bool includeNonPublic) {
		var symbols = new List<ISymbol>();
		var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

		void AddMembers(INamedTypeSymbol symbol, bool includeNonPublicForType) {
			foreach (var member in symbol.GetMembers()) {
				if (member is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
					&& !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, parentClass)) {
					continue;
				}

				if (!includeNonPublicForType && member.DeclaredAccessibility != Accessibility.Public) {
					continue;
				}

				if (!includeNonPublicForType && IsOverriddenByExisting(member, symbols)) {
					continue;
				}

				if (seen.Add(member)) {
					symbols.Add(member);
				}
			}
		}

		AddMembers(parentClass, includeNonPublic);
		for (var current = parentClass.BaseType; current is not null; current = current.BaseType) {
			AddMembers(current, false);
		}

		return symbols;
	}

	/// Checks whether a base member is already represented by an overriding or property implementation.
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

	internal readonly struct CollectionResult<T> {
		internal CollectionResult(IReadOnlyList<T> items, bool hasInvalidMembers) {
			Items = items;
			HasInvalidMembers = hasInvalidMembers;
		}

		internal IReadOnlyList<T> Items { get; }
		internal bool HasInvalidMembers { get; }
	}

	/// Collects attributed fields and properties from the supplied or inherited member search.
	internal static CollectionResult<AttributedMember> CollectFieldsAndProperties(
		INamedTypeSymbol parentClass,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		INamedTypeSymbol containingType,
		DiagnosticDescriptor invalidMemberDescriptor,
		bool includeInheritedTypes,
		bool includeNonPublic = true) {
		var members = includeInheritedTypes
			? GetAttributedSearchMembers(parentClass, includeNonPublic)
			: parentClass.GetMembers();
		var items = new List<AttributedMember>();
		var hasInvalidMembers = false;
		foreach (var member in members) {
			if (!AttributeUsageValidator.HasAttribute(member, expectedAttribute, diagnostics)) {
				continue;
			}

			if (member is IFieldSymbol fieldSymbol) {
				if (fieldSymbol.IsStatic || fieldSymbol.DeclaredAccessibility != Accessibility.Public) {
					ReportInvalidMember(
						invalidMemberDescriptor,
						diagnostics,
						fieldSymbol,
						containingType,
						"only public instance fields are supported");
					hasInvalidMembers = true;
					continue;
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
						invalidMemberDescriptor,
						diagnostics,
						propertySymbol,
						containingType,
						"only non-indexer public instance properties with public getter and setter are supported");
					hasInvalidMembers = true;
					continue;
				}

				items.Add(new AttributedMember(
					propertySymbol.Name,
					propertySymbol.Type,
					propertySymbol.Locations.FirstOrDefault()));
				continue;
			}

			ReportInvalidMember(
				invalidMemberDescriptor,
				diagnostics,
				member,
				containingType,
				"only fields and properties are supported");
			hasInvalidMembers = true;
		}

		return new CollectionResult<AttributedMember>(items, hasInvalidMembers);
	}

	private static void ReportInvalidMember(
		DiagnosticDescriptor descriptor,
		ICollection<Diagnostic> diagnostics,
		ISymbol member,
		INamedTypeSymbol containingType,
		string reason) {
		diagnostics.Add(Diagnostic.Create(
			descriptor,
			member.Locations.FirstOrDefault(),
			member.ToDisplayString(),
			containingType.ToDisplayString(),
			reason));
	}
}
