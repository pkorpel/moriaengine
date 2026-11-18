using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledScriptHolders;

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

	internal static IReadOnlyList<ScriptHolderModel> BuildModels(
		IReadOnlyCollection<ScriptHolderCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		global::SteamEngine.Generators.RequiredSymbols requiredSymbols) {
		var valid = new List<(ScriptHolderCandidate Candidate, string FunctionName)>();
		foreach (var candidate in candidates) {
			if (IsPublicStatic(candidate.MethodSymbol)) {
				valid.Add((candidate, candidate.FunctionName));
				continue;
			}

			diagnostics.Add(Diagnostic.Create(
				SteamFunctionMustBePublicStaticDescriptor,
				candidate.MethodSymbol.Locations.FirstOrDefault(),
				candidate.MethodSymbol.ToDisplayString()));
		}

		var ordered = valid.OrderBy(c => c.FunctionName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(c => c.Candidate.MethodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
			.ToList();
		var conflictingFunctionNames = DuplicateNameValidator.Validate(
			ordered.Select(candidate => (
				Name: candidate.FunctionName,
				Symbol: (ISymbol) candidate.Candidate.MethodSymbol)),
			DuplicateSteamFunctionNameDescriptor,
			diagnostics);

		var emittedFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var models = new List<ScriptHolderModel>();
		foreach (var candidate in ordered) {
			if (!emittedFunctions.Add(candidate.FunctionName)) {
				continue;
			}

			models.Add(BuildModel(candidate.Candidate, candidate.FunctionName, requiredSymbols.CanonicalTypes.ScriptArgsType));
		}

		return models
			.OrderBy(m => m.FunctionName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(m => m.GeneratedTypeName, StringComparer.Ordinal)
			.ToList();
	}

	private static bool IsPublicStatic(IMethodSymbol methodSymbol) {
		return methodSymbol.IsStatic && methodSymbol.DeclaredAccessibility == Accessibility.Public;
	}

	private static ScriptHolderModel BuildModel(ScriptHolderCandidate candidate, string functionName, INamedTypeSymbol scriptArgsType) {
		var method = candidate.MethodSymbol;
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
				&& IsScriptArgsAssignable(parameters[i].Type, scriptArgsType)) {
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
			candidate.Description,
			method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			method.Name,
			method.ReturnsVoid,
			parameters.Length > 0,
			invocationParameters);
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

	private static string CastParameter(ITypeSymbol parameterType, string inputExpression) {
		if (IsNumericOrDateTime(parameterType)) {
			if (parameterType.TypeKind == TypeKind.Enum) {
				var enumType = (INamedTypeSymbol) parameterType;
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
