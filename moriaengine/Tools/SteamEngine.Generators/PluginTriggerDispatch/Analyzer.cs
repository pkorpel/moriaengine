using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.PluginTriggerDispatch;

// Rejects duplicate trigger names within a plugin before rendering its dispatch.
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

	internal static PluginTriggerOutput Analyze(PluginTriggerParseResult result) {
		var candidate = result.Candidate;
		if (candidate is null) {
			return new PluginTriggerOutput(null, result.Diagnostics);
		}
		var model = candidate.Model;
		var duplicateTriggerNames = model.TriggerMethods
				.GroupBy(method => method.TriggerName, StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key)
				.ToArray();

		if (duplicateTriggerNames.Length == 0) {
			return new PluginTriggerOutput(model, result.Diagnostics);
		}
		var diagnostics = new List<GeneratorDiagnosticData>(result.Diagnostics);
		foreach (var duplicateTriggerName in duplicateTriggerNames) {
			diagnostics.Add(GeneratorDiagnosticData.Create(
				DuplicateTriggerNameDescriptor,
				candidate.TypeLocation,
				duplicateTriggerName,
				model.FullyQualifiedTypeName));
		}
		return new PluginTriggerOutput(null,
			EquatableArray<GeneratorDiagnosticData>.From(diagnostics));
	}
}
