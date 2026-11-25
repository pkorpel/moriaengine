using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledTriggerGroups;

// Validates global group names and trigger names within each group.
internal static class Analyzer {
	internal static readonly DiagnosticDescriptor DuplicateTypeNameDescriptor = new(
		"SEGEN405",
		"Compiled trigger group type name is not unique",
		"Compiled trigger group type name '{0}' conflicts between '{1}' and '{2}'; names must be distinct for code generation",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor DuplicateTriggerNameDescriptor = new(
		"SEGEN402",
		"Duplicate trigger method name",
		"Trigger method name '{0}' appears multiple times on '{1}'",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static readonly DiagnosticDescriptor TypeMustBePartialDescriptor = new(
		"SEGEN403",
		"Compiled trigger group must be partial",
		"Compiled trigger group '{0}' must be declared partial for source-generated Run dispatch",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static IReadOnlyList<CompiledTriggerGroupModel> BuildModels(
		IReadOnlyCollection<CompiledTriggerGroupCandidate> candidates,
		ICollection<GeneratorDiagnosticData> diagnostics) {
		var conflictingNames = DuplicateNameValidator.Validate(candidates, candidate => candidate.Model.TypeName, (name, first, candidate) =>
			diagnostics.Add(GeneratorDiagnosticData.Create(DuplicateTypeNameDescriptor, candidate.Location,
				name, first.FullyQualifiedTypeName, candidate.FullyQualifiedTypeName)));

		var models = new List<CompiledTriggerGroupModel>();
		foreach (var candidate in candidates) {
			var model = candidate.Model;
			if (conflictingNames.Contains(model.TypeName)) {
				continue;
			}
			var duplicates = new HashSet<string>(
				model.TriggerMethods.GroupBy(method => method.TriggerName, StringComparer.OrdinalIgnoreCase)
					.Where(group => group.Count() > 1).Select(group => group.Key),
				StringComparer.OrdinalIgnoreCase);
			foreach (var duplicate in duplicates) {
				diagnostics.Add(GeneratorDiagnosticData.Create(DuplicateTriggerNameDescriptor, candidate.Location,
					duplicate, candidate.FullyQualifiedTypeName));
			}
			if (duplicates.Count == 0) {
				models.Add(model);
			}
		}
		return models;
	}

}
