using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class NestedTypeValidator {
	internal static readonly DiagnosticDescriptor NestedTypeNotSupportedDescriptor = new(
		"SEGEN906",
		"Nested target type not supported",
		"Type '{0}' is nested and is ignored by this generator",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static bool TryReportNestedType(
		INamedTypeSymbol symbol,
		ICollection<Diagnostic> diagnostics) {
		if (symbol.ContainingType is null) {
			return false;
		}

		diagnostics.Add(Diagnostic.Create(
			NestedTypeNotSupportedDescriptor,
			symbol.Locations.FirstOrDefault(),
			symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		return true;
	}

	internal static bool IsAssignableTo(INamedTypeSymbol source, INamedTypeSymbol targetBaseType, bool includeSelf) {
		var current = includeSelf ? source : source.BaseType;
		while (current is not null) {
			if (SymbolEqualityComparer.Default.Equals(current, targetBaseType)) {
				return true;
			}

			current = current.BaseType;
		}

		return false;
	}
}
