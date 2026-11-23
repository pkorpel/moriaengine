using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class DuplicateNameValidator {
	internal static HashSet<string> Validate(
		IEnumerable<(string Name, ISymbol Symbol)> items,
		DiagnosticDescriptor duplicateNameDescriptor,
		ICollection<Diagnostic> diagnostics) {
		return Validate(items, item => item.Name, (name, first, duplicate) =>
			diagnostics.Add(Diagnostic.Create(
				duplicateNameDescriptor,
				duplicate.Symbol.Locations.FirstOrDefault(),
				name,
				first.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				duplicate.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
	}

	internal static HashSet<string> Validate<T>(
		IEnumerable<T> items,
		Func<T, string> getName,
		Action<string, T, T> reportDuplicate) {
		// Record conflicting names so callers can suppress every occurrence, including the first.
		var firstByName = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
		var conflictingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var item in items) {
			var name = getName(item);
			if (!firstByName.TryGetValue(name, out var first)) {
				firstByName[name] = item;
				continue;
			}
			conflictingNames.Add(name);
			reportDuplicate(name, first, item);
		}

		return conflictingNames;
	}
}
