using System;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class DispatchModelBuilder {
	internal static DispatchParameterModel BuildScriptArgsParameter(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		if (SymbolEqualityComparer.Default.Equals(parameterType, scriptArgsType)) {
			return new DispatchParameterModel(
				UsesScriptArgsInput: true,
				DispatchParameterConversion.None,
				string.Empty);
		}

		return new DispatchParameterModel(
			UsesScriptArgsInput: true,
			DispatchParameterConversion.Cast,
			parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	internal static bool IsScriptArgsAssignable(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		var current = parameterType;
		while (current is not null) {
			if (SymbolEqualityComparer.Default.Equals(current, scriptArgsType)) {
				return true;
			}
			current = (current as INamedTypeSymbol)?.BaseType;
		}
		return false;
	}

	internal static DispatchParameterModel BuildInvocationParameter(ITypeSymbol parameterType) {
		if (IsNumericOrDateTime(parameterType)) {
			if (parameterType.TypeKind == TypeKind.Enum) {
				return new DispatchParameterModel(
					UsesScriptArgsInput: false,
					DispatchParameterConversion.EnumConvertThenCast,
					parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			}

			return new DispatchParameterModel(
				UsesScriptArgsInput: false,
				DispatchParameterConversion.Convert,
				parameterType.Name);
		}

		if (parameterType.SpecialType != SpecialType.System_Object) {
			return new DispatchParameterModel(
				UsesScriptArgsInput: false,
				DispatchParameterConversion.Cast,
				parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		}

		return new DispatchParameterModel(
			UsesScriptArgsInput: false,
			DispatchParameterConversion.None,
			string.Empty);
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
