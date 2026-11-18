using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledTriggerGroups;

internal static class Analyzer {
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
		global::SteamEngine.Generators.RequiredSymbols requiredSymbols) {
		var orderedCandidates = candidates
			.OrderBy(c => c.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
			.ToList();

		var models = new List<CompiledTriggerGroupModel>();
		foreach (var candidate in orderedCandidates) {
			if (!TryBuildModel(candidate, requiredSymbols.CanonicalTypes.ScriptArgsType, diagnostics, out var model)) {
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
		var orderedMethods = candidate.TriggerMethods
			.OrderBy(m => m.Name.Substring(3), StringComparer.OrdinalIgnoreCase)
			.ThenBy(m => m.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
			.ToList();

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
		var containingTypes = GetContainingTypes(candidate.TypeSymbol);
		model = new CompiledTriggerGroupModel(
			namespaceName,
			containingTypes,
			ToAccessibilityKeyword(candidate.TypeSymbol.DeclaredAccessibility),
			candidate.TypeSymbol.Name,
			methods);
		return true;
	}

	private static IReadOnlyList<ContainingTypeModel> GetContainingTypes(INamedTypeSymbol typeSymbol) {
		var types = new Stack<ContainingTypeModel>();
		for (var containing = typeSymbol.ContainingType; containing is not null; containing = containing.ContainingType) {
			types.Push(new ContainingTypeModel(
				ToAccessibilityKeyword(containing.DeclaredAccessibility),
				containing.IsStatic,
				containing.IsAbstract,
				containing.IsSealed,
				containing.Name));
		}
		return types.ToList();
	}

	private static string ToAccessibilityKeyword(Accessibility accessibility) {
		return accessibility switch {
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			_ => "internal"
		};
	}

	private static CompiledTriggerMethodModel BuildMethodModel(IMethodSymbol method, INamedTypeSymbol scriptArgsType) {
		var parameters = new List<InvocationParameterModel>();
		for (var i = 0; i < method.Parameters.Length; i++) {
			if (i == 0) {
				parameters.Add(BuildInvocationParameter(
					method.Parameters[i].Type,
					usesScriptArgsInput: false));
				continue;
			}

			// Semantic rule only: preserve legacy behavior where a 2-parameter trigger method
			// can use ScriptArgs as the second parameter instead of argv[0].
			if (i == 1
				&& method.Parameters.Length == 2
				&& IsScriptArgsAssignable(method.Parameters[i].Type, scriptArgsType)) {
				parameters.Add(BuildScriptArgsParameter(method.Parameters[i].Type, scriptArgsType));
				continue;
			}

			parameters.Add(BuildInvocationParameter(
				method.Parameters[i].Type,
				usesScriptArgsInput: false));
		}

		return new CompiledTriggerMethodModel(
			method.Name.Substring(3),
			method.Name,
			method.ReturnsVoid,
			parameters);
	}

	private static InvocationParameterModel BuildScriptArgsParameter(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		if (SymbolEqualityComparer.Default.Equals(parameterType, scriptArgsType)) {
			return new InvocationParameterModel(
				usesScriptArgsInput: true,
				InvocationParameterConversion.None,
				typeName: null);
		}

		return new InvocationParameterModel(
			usesScriptArgsInput: true,
			InvocationParameterConversion.Cast,
			parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	private static bool IsScriptArgsAssignable(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		var current = parameterType;
		while (current is not null) {
			if (SymbolEqualityComparer.Default.Equals(current, scriptArgsType)) {
				return true;
			}
			current = (current as INamedTypeSymbol)?.BaseType;
		}
		return false;
	}

	private static InvocationParameterModel BuildInvocationParameter(
		ITypeSymbol parameterType,
		bool usesScriptArgsInput) {
		if (IsNumericOrDateTime(parameterType)) {
			if (parameterType.TypeKind == TypeKind.Enum) {
				var enumType = (INamedTypeSymbol) parameterType;
				return new InvocationParameterModel(
					usesScriptArgsInput,
					InvocationParameterConversion.EnumConvertThenCast,
					enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			}

			return new InvocationParameterModel(
				usesScriptArgsInput,
				InvocationParameterConversion.Convert,
				parameterType.Name);
		}

		if (parameterType.SpecialType != SpecialType.System_Object) {
			return new InvocationParameterModel(
				usesScriptArgsInput,
				InvocationParameterConversion.Cast,
				parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		}

		return new InvocationParameterModel(
			usesScriptArgsInput,
			InvocationParameterConversion.None,
			typeName: null);
	}

	private static bool IsNumericOrDateTime(ITypeSymbol typeSymbol) {
		if (typeSymbol.TypeKind == TypeKind.Enum) {
			return true;
		}

		return typeSymbol.SpecialType switch {
			SpecialType.System_Boolean => true,
			SpecialType.System_Byte => true,
			SpecialType.System_SByte => true,
			SpecialType.System_Int16 => true,
			SpecialType.System_UInt16 => true,
			SpecialType.System_Int32 => true,
			SpecialType.System_UInt32 => true,
			SpecialType.System_Int64 => true,
			SpecialType.System_UInt64 => true,
			SpecialType.System_Single => true,
			SpecialType.System_Double => true,
			SpecialType.System_Decimal => true,
			_ => string.Equals(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "global::System.DateTime", StringComparison.Ordinal)
		};
	}
}
