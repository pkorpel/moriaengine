using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal static class Analyzer {
	internal static (ImmutableArray<PluginTriggerCandidate> Models, ImmutableArray<PluginTriggerDiagnosticData> Diagnostics) Analyze(
		ImmutableArray<PluginTriggerCandidate> candidates) {
		var diagnostics = ImmutableArray.CreateBuilder<PluginTriggerDiagnosticData>();
		var models = ImmutableArray.CreateBuilder<PluginTriggerCandidate>();

		foreach (var candidate in candidates) {
			if (!TryValidate(candidate, diagnostics)) {
				continue;
			}

			models.Add(candidate);
		}

		return (models.ToImmutable(), diagnostics.ToImmutable());
	}

	private static bool TryValidate(
		PluginTriggerCandidate candidate,
		ImmutableArray<PluginTriggerDiagnosticData>.Builder diagnostics) {
		var duplicateTriggerNames = new HashSet<string>(
			candidate.TriggerMethods
				.GroupBy(method => method.TriggerName, StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key),
			StringComparer.OrdinalIgnoreCase);

		foreach (var duplicateTriggerName in duplicateTriggerNames) {
			diagnostics.Add(new PluginTriggerDiagnosticData(
				PluginTriggerDiagnosticKind.DuplicateTriggerName,
				candidate.TypeLocation,
				duplicateTriggerName,
				candidate.FullyQualifiedTypeName));
		}

		return duplicateTriggerNames.Count == 0;
	}
}
