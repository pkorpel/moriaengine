using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledScriptHolders;

internal static class Analyzer {
	internal const string SteamFunctionAttributeMetadataName = "SteamEngine.CompiledScripts.SteamFunctionAttribute";

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

	internal static readonly DiagnosticDescriptor MissingSteamFunctionAttributeDescriptor = new(
		"SEGEN103",
		"Missing SteamFunction attribute declaration",
		"Required generator attribute declaration '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor AttributeShadowingDescriptor = new(
		"SEGEN104",
		"Shadowed generator attribute",
		"Attribute '{0}' resolves to '{1}', but this generator expects '{2}'",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static readonly DiagnosticDescriptor MissingCanonicalEngineTypeDescriptor = new(
		"SEGEN106",
		"Missing canonical engine type",
		"Required canonical engine type '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static IReadOnlyList<ScriptHolderModel> BuildModels(
		IReadOnlyCollection<ScriptHolderCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		Compilation compilation) {
		var valid = new List<ScriptHolderCandidate>();
		foreach (var candidate in candidates) {
			if (IsPublicStatic(candidate.MethodSymbol)) {
				valid.Add(candidate);
				continue;
			}

			diagnostics.Add(Diagnostic.Create(
				SteamFunctionMustBePublicStaticDescriptor,
				candidate.MethodSymbol.Locations.FirstOrDefault(),
				candidate.MethodSymbol.ToDisplayString()));
		}

		var unique = new Dictionary<string, ScriptHolderCandidate>(StringComparer.OrdinalIgnoreCase);
		var ordered = valid.OrderBy(c => c.FunctionName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(c => c.MethodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
			.ToList();
		foreach (var candidate in ordered) {
			if (unique.TryGetValue(candidate.FunctionName, out var existing)) {
				diagnostics.Add(Diagnostic.Create(
					DuplicateSteamFunctionNameDescriptor,
					candidate.MethodSymbol.Locations.FirstOrDefault(),
					candidate.FunctionName,
					existing.MethodSymbol.ToDisplayString(),
					candidate.MethodSymbol.ToDisplayString()));
				continue;
			}
			unique[candidate.FunctionName] = candidate;
		}

		var models = new List<ScriptHolderModel>();
		foreach (var candidate in unique.Values) {
			models.Add(BuildModel(candidate, compilation));
		}

		return models
			.OrderBy(m => m.FunctionName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(m => m.GeneratedTypeName, StringComparer.Ordinal)
			.ToList();
	}

	internal static bool ValidateRequiredDeclarations(Compilation compilation, ICollection<Diagnostic> diagnostics) {
		if (compilation.GetTypeByMetadataName(SteamFunctionAttributeMetadataName) is not null) {
			return true;
		}

		diagnostics.Add(Diagnostic.Create(
			MissingSteamFunctionAttributeDescriptor,
			Location.None,
			SteamFunctionAttributeMetadataName));
		return false;
	}

	internal static bool ValidateRequiredCanonicalTypes(Compilation compilation, ICollection<Diagnostic> diagnostics) {
		var requiredMetadataNames = new[] {
			"SteamEngine.ScriptHolder",
			"SteamEngine.ScriptArgs",
			"SteamEngine.CompiledScripts.CompiledScriptHolder"
		};

		var allPresent = true;
		foreach (var metadataName in requiredMetadataNames) {
			if (compilation.GetTypeByMetadataName(metadataName) is not null) {
				continue;
			}

			diagnostics.Add(Diagnostic.Create(
				MissingCanonicalEngineTypeDescriptor,
				Location.None,
				metadataName));
			allPresent = false;
		}
		return allPresent;
	}

	private static bool IsPublicStatic(IMethodSymbol methodSymbol) {
		return methodSymbol.IsStatic && methodSymbol.DeclaredAccessibility == Accessibility.Public;
	}

	private static ScriptHolderModel BuildModel(ScriptHolderCandidate candidate, Compilation compilation) {
		var method = candidate.MethodSymbol;
		var invocationParameters = new List<InvocationParameterModel>();
		var parameters = method.Parameters;
		var scriptArgsType = compilation.GetTypeByMetadataName("SteamEngine.ScriptArgs")
			?? throw new InvalidOperationException("Canonical engine type SteamEngine.ScriptArgs is unavailable.");

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
			CreateGeneratedTypeName(candidate.FunctionName),
			candidate.FunctionName,
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
		var builder = new StringBuilder("GeneratedScriptHolder_");
		if (string.IsNullOrEmpty(functionName)) {
			builder.Append("Unnamed");
		} else {
			foreach (var ch in functionName) {
				builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
			}
		}
		return builder.ToString();
	}
}
