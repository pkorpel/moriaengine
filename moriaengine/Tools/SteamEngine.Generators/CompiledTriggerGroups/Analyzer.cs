using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledTriggerGroups;

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

	internal static IReadOnlyList<CompiledTriggerGroupModel> BuildModels(
		IReadOnlyCollection<CompiledTriggerGroupCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		RequiredSymbols symbols) {
		var conflictingTypeNames = DuplicateNameValidator.Validate(
			candidates.Select(candidate => (
				candidate.TypeSymbol.Name,
				(ISymbol) candidate.TypeSymbol)),
			DuplicateTypeNameDescriptor,
			diagnostics);

		var models = new List<CompiledTriggerGroupModel>();
		foreach (var candidate in candidates) {
			if (conflictingTypeNames.Contains(candidate.TypeSymbol.Name)) {
				continue;
			}

			if (!TryBuildModel(candidate, symbols.CanonicalTypes.ScriptArgsType, diagnostics, out var model)) {
				continue;
			}

			models.Add(model);
		}

		return models;
	}

	private static bool TryBuildModel(
		CompiledTriggerGroupCandidate candidate,
		INamedTypeSymbol scriptArgsType,
		ICollection<Diagnostic> diagnostics,
		out CompiledTriggerGroupModel model) {
		model = null!;
		var orderedMethods = candidate.TriggerMethods.ToList();

		var duplicateTriggerNames = new HashSet<string>(
			orderedMethods
				.GroupBy(method => method.Name.Substring(3), StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key),
			StringComparer.OrdinalIgnoreCase);

		foreach (var duplicateTriggerName in duplicateTriggerNames) {
			diagnostics.Add(Diagnostic.Create(
				DuplicateTriggerNameDescriptor,
				candidate.TypeSymbol.Locations.FirstOrDefault(),
				duplicateTriggerName,
				candidate.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		}

		if (duplicateTriggerNames.Count > 0) {
			return false;
		}

		var methods = orderedMethods.Select(method => BuildMethodModel(method, scriptArgsType)).ToList();
		var namespaceName = candidate.TypeSymbol.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: candidate.TypeSymbol.ContainingNamespace.ToDisplayString();
		model = new CompiledTriggerGroupModel(
			namespaceName,
			candidate.TypeSymbol.Name,
			methods);
		return true;
	}

	private static CompiledTriggerMethodModel BuildMethodModel(IMethodSymbol method, INamedTypeSymbol scriptArgsType) {
		var parameters = new List<DispatchParameterModel>();
		for (var i = 0; i < method.Parameters.Length; i++) {
			if (i == 0) {
				parameters.Add(DispatchModelBuilder.BuildInvocationParameter(method.Parameters[i].Type));
				continue;
			}

			// Semantic rule only: preserve legacy behavior where a 2-parameter trigger method
			// can use ScriptArgs as the second parameter instead of argv[0].
			if (i == 1
				&& method.Parameters.Length == 2
				&& DispatchModelBuilder.IsScriptArgsAssignable(method.Parameters[i].Type, scriptArgsType)) {
				parameters.Add(DispatchModelBuilder.BuildScriptArgsParameter(method.Parameters[i].Type, scriptArgsType));
				continue;
			}

			parameters.Add(DispatchModelBuilder.BuildInvocationParameter(method.Parameters[i].Type));
		}

		return new CompiledTriggerMethodModel(
			method.Name.Substring(3),
			method.Name,
			method.ReturnsVoid,
			parameters);
	}
}
