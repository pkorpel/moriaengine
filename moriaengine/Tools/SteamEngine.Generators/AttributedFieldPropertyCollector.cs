using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class AttributedFieldPropertyCollector {
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

	internal static CollectionResult<AttributedMember> Collect(
		IEnumerable<ISymbol> members,
		INamedTypeSymbol expectedAttribute,
		ICollection<Diagnostic> diagnostics,
		INamedTypeSymbol containingType,
		DiagnosticDescriptor invalidMemberDescriptor) {
		var items = new List<AttributedMember>();
		var hasInvalidMembers = false;
		foreach (var member in members) {
			if (!AttributeUsageValidator.HasCanonicalAttribute(member, expectedAttribute, diagnostics)) {
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

			ReportInvalidMember(invalidMemberDescriptor, diagnostics, member, containingType, "only fields and properties are supported");
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
