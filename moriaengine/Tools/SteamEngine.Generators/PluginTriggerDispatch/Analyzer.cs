using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal static class Analyzer {
	internal static readonly DiagnosticDescriptor TypeMustBePartialDescriptor = new(
		"SEGEN406",
		"Plugin class must be partial",
		"Plugin class '{0}' must be declared partial for source-generated trigger dispatch",
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

	internal static ImmutableArray<PluginTriggerCandidate> Analyze(
		IReadOnlyCollection<PluginTriggerCandidate> candidates,
		ICollection<Diagnostic> diagnostics) {
		var models = ImmutableArray.CreateBuilder<PluginTriggerCandidate>();

		foreach (var candidate in candidates) {
			if (!TryValidate(candidate, diagnostics)) {
				continue;
			}

			models.Add(candidate);
		}

		return models.ToImmutable();
	}

	private static bool TryValidate(
		PluginTriggerCandidate candidate,
		ICollection<Diagnostic> diagnostics) {
		var duplicateTriggerNames = new HashSet<string>(
			candidate.TriggerMethods
				.GroupBy(method => method.TriggerName, StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key),
			StringComparer.OrdinalIgnoreCase);

		foreach (var duplicateTriggerName in duplicateTriggerNames) {
			diagnostics.Add(Diagnostic.Create(
				DuplicateTriggerNameDescriptor,
				ToLocation(candidate.TypeLocation),
				duplicateTriggerName,
				candidate.FullyQualifiedTypeName));
		}

		return duplicateTriggerNames.Count == 0;
	}

	internal static Diagnostic CreateDiagnostic(PluginTriggerDiagnosticData diagnosticData) {
		var location = ToLocation(diagnosticData.Location);
		if (diagnosticData.Kind == PluginTriggerDiagnosticKind.TypeMustBePartial) {
			return Diagnostic.Create(
				TypeMustBePartialDescriptor,
				location,
				diagnosticData.Argument0);
		}

		if (diagnosticData.Kind == PluginTriggerDiagnosticKind.NestedTypeNotSupported) {
			return Diagnostic.Create(
				NestedTypeValidator.NestedTypeNotSupportedDescriptor,
				location,
				diagnosticData.Argument0);
		}

		throw new InvalidOperationException("Unknown plugin trigger diagnostic kind.");
	}

	private static Location ToLocation(SourceLocationData location) {
		var start = new LinePosition(location.StartLine, location.StartCharacter);
		var end = new LinePosition(location.EndLine, location.EndCharacter);
		return Location.Create(location.FilePath, new TextSpan(0, 0), new LinePositionSpan(start, end));
	}
}
