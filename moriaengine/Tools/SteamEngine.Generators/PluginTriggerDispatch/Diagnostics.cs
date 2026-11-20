using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal static class Diagnostics {
	internal static readonly DiagnosticDescriptor TypeMustBePartialDescriptor = new(
		"SEGEN406",
		"Plugin class must be partial",
		"Plugin class '{0}' must be declared partial for source-generated trigger dispatch",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor ContainingTypeMustBePartialDescriptor = new(
		"SEGEN407",
		"Containing type must be partial",
		"Containing type '{0}' must be declared partial because it contains plugin class '{1}'",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor DuplicateTriggerNameDescriptor = new(
		"SEGEN408",
		"Duplicate plugin trigger method name",
		"Plugin trigger name '{0}' appears multiple times on '{1}'",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static Diagnostic Create(PluginTriggerDiagnosticData diagnosticData) {
		var location = ToLocation(diagnosticData.Location);
		return diagnosticData.Kind switch {
			PluginTriggerDiagnosticKind.TypeMustBePartial => Diagnostic.Create(
				TypeMustBePartialDescriptor,
				location,
				diagnosticData.Argument0),
			PluginTriggerDiagnosticKind.ContainingTypeMustBePartial => Diagnostic.Create(
				ContainingTypeMustBePartialDescriptor,
				location,
				diagnosticData.Argument0,
				diagnosticData.Argument1),
			PluginTriggerDiagnosticKind.DuplicateTriggerName => Diagnostic.Create(
				DuplicateTriggerNameDescriptor,
				location,
				diagnosticData.Argument0,
				diagnosticData.Argument1),
			_ => Diagnostic.Create(
				DuplicateTriggerNameDescriptor,
				location,
				diagnosticData.Argument0,
				diagnosticData.Argument1)
		};
	}

	private static Location ToLocation(SourceLocationData location) {
		var start = new LinePosition(location.StartLine, location.StartCharacter);
		var end = new LinePosition(location.EndLine, location.EndCharacter);
		return Location.Create(location.FilePath, new TextSpan(0, 0), new LinePositionSpan(start, end));
	}
}
