using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.ViewableClasses;

/// <summary>Diagnostics specific to the ViewableClass generator (SEGEN950-953).</summary>
internal static class Diagnostics {
	/// <summary>A member of a viewable type can't become a row or button (indexer, ref struct, bad button signature...).</summary>
	internal static readonly DiagnosticDescriptor InvalidViewableMember = new(
		"SEGEN950",
		"Invalid viewable member",
		"Viewable member '{0}' cannot be generated: {1}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	/// <summary>A [ViewDescriptor] class or one of its Get/Set/Button methods is malformed.</summary>
	internal static readonly DiagnosticDescriptor InvalidViewDescriptor = new(
		"SEGEN951",
		"Invalid view descriptor",
		"View descriptor '{0}' cannot be generated: {1}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	/// <summary>A type has both [ViewableClass] and [ViewDescriptor].</summary>
	internal static readonly DiagnosticDescriptor ConflictingViewableType = new(
		"SEGEN952",
		"Conflicting viewable type",
		"Type '{0}' is declared as both a viewable class and a view descriptor",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	/// <summary>A user type shadows a canonical engine type name, so the text-to-value conversion would be ambiguous.</summary>
	internal static readonly DiagnosticDescriptor TypeNameConflict = new(
		"SEGEN953",
		"Conflicting canonical type name",
		"Type '{0}' has the same name as canonical engine type '{1}', so DataView conversion cannot be generated safely",
		"Usage",
		DiagnosticSeverity.Warning,
		true);
}
