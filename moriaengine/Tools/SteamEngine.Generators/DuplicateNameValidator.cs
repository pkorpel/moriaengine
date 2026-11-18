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
		var firstByName = new Dictionary<string, ISymbol>(StringComparer.OrdinalIgnoreCase);
		var conflictingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var item in items) {
			if (!firstByName.TryGetValue(item.Name, out var first)) {
				firstByName[item.Name] = item.Symbol;
				continue;
			}

			conflictingNames.Add(item.Name);
			diagnostics.Add(Diagnostic.Create(
				duplicateNameDescriptor,
				item.Symbol.Locations.FirstOrDefault(),
				item.Name,
				first.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				item.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		}

		return conflictingNames;
	}
}
