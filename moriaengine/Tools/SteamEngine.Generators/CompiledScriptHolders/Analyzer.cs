using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledScriptHolders;

// Builds invocation models and rejects functions with conflicting names.
internal static class Analyzer {
	internal static readonly DiagnosticDescriptor SteamFunctionMustBePublicStaticDescriptor = new(
		"SEGEN101",
		"SteamFunction must be public static",
		"Method '{0}' is marked with [SteamFunction] but must be public static",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor DuplicateSteamFunctionNameDescriptor = new(
		"SEGEN102",
		"Duplicate SteamFunction name",
		"Steam function name '{0}' is declared by both '{1}' and '{2}'",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor InvalidSteamFunctionNameArgumentDescriptor = new(
		"SEGEN103",
		"Invalid SteamFunction name argument",
		"SteamFunction name argument must be a string literal, but was '{0}'",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor InvalidSteamFunctionNameDescriptor = new(
		"SEGEN104",
		"Invalid SteamFunction name",
		"Steam function name '{0}' must start with an ASCII letter or underscore and contain only ASCII letters, digits, or underscores",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static IReadOnlyList<ScriptHolderModel> BuildModels(
		IReadOnlyCollection<ScriptHolderCandidate> candidates,
		ICollection<GeneratorDiagnosticData> diagnostics) {
		var conflictingFunctionNames = DuplicateNameValidator.Validate(candidates, candidate => candidate.Model.FunctionName, (name, first, candidate) =>
			diagnostics.Add(GeneratorDiagnosticData.Create(DuplicateSteamFunctionNameDescriptor, candidate.Location,
				name, first.DisplayName, candidate.DisplayName)));

		var models = new List<ScriptHolderModel>();
		foreach (var candidate in candidates) {
			if (conflictingFunctionNames.Contains(candidate.Model.FunctionName)) {
				continue;
			}

			models.Add(candidate.Model);
		}

		return models;
	}

	internal static ScriptHolderModel BuildModel(IMethodSymbol method, string functionName, string? description, INamedTypeSymbol scriptArgsType) {
		var invocationParameters = new List<InvocationParameterModel>();
		var parameters = method.Parameters;

		for (var i = 0; i < parameters.Length; i++) {
			if (i == 0) {
				invocationParameters.Add(new InvocationParameterModel(
					CastParameter(parameters[i].Type, "self")));
				continue;
			}

			if (i == 1
				&& parameters.Length == 2
				&& DispatchModelBuilder.IsScriptArgsAssignable(parameters[i].Type, scriptArgsType)) {
				var scriptArgs = SymbolEqualityComparer.Default.Equals(parameters[i].Type, scriptArgsType)
					? "sa"
					: "(" + parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ") sa";
				invocationParameters.Add(new InvocationParameterModel(scriptArgs));
				continue;
			}

			invocationParameters.Add(new InvocationParameterModel(
				CastParameter(parameters[i].Type, "argv[" + (i - 1).ToString(CultureInfo.InvariantCulture) + "]")));
		}

		return new ScriptHolderModel(
			CreateGeneratedTypeName(functionName),
			functionName,
			description,
			method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			method.Name,
			method.ReturnsVoid,
			parameters.Length > 0,
			EquatableArray<InvocationParameterModel>.From(invocationParameters));
	}

	private static string CastParameter(ITypeSymbol parameterType, string inputExpression) {
		if (DispatchModelBuilder.IsNumericOrDateTime(parameterType)) {
			if (parameterType.TypeKind == TypeKind.Enum) {
				var enumType = (INamedTypeSymbol) parameterType;
				// Script functions convert through the enum's underlying type, not always Int32.
				var underlying = enumType.EnumUnderlyingType ?? enumType;
				return "(" + parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")"
					+ ConvertExpression(underlying, inputExpression);
			}

			return ConvertExpression(parameterType, inputExpression);
		}

		if (parameterType.SpecialType != SpecialType.System_Object) {
			return "(" + parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ") " + inputExpression;
		}

		return inputExpression;
	}

	private static string ConvertExpression(ITypeSymbol typeSymbol, string inputExpression) {
		return "global::System.Convert.To" + GetConvertSuffix(typeSymbol) + "(" + inputExpression + ")";
	}

	private static string GetConvertSuffix(ITypeSymbol typeSymbol) {
		return typeSymbol.SpecialType switch {
			SpecialType.System_Boolean => "Boolean",
			SpecialType.System_Byte => "Byte",
			SpecialType.System_SByte => "SByte",
			SpecialType.System_Int16 => "Int16",
			SpecialType.System_UInt16 => "UInt16",
			SpecialType.System_Int32 => "Int32",
			SpecialType.System_UInt32 => "UInt32",
			SpecialType.System_Int64 => "Int64",
			SpecialType.System_UInt64 => "UInt64",
			SpecialType.System_Single => "Single",
			SpecialType.System_Double => "Double",
			SpecialType.System_Decimal => "Decimal",
			_ => "DateTime"
		};
	}

	private static string CreateGeneratedTypeName(string functionName) {
		return "GeneratedScriptHolder_" + IdentifierSanitizer.Sanitize(functionName);
	}
}
