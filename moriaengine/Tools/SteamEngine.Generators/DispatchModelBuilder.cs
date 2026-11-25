using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

// Discovers trigger methods and describes their arguments without retaining symbols in the models.
internal static class DispatchModelBuilder {
	// Compiled groups require public methods; plugins can also call non-public methods on their own class.
	internal static IReadOnlyList<IMethodSymbol> GetTriggerMethods(INamedTypeSymbol typeSymbol, bool publicOnly) {
		var methods = new List<IMethodSymbol>();
		// Visit derived classes first so an override suppresses its inherited declaration.
		for (var current = typeSymbol; current is not null; current = current.BaseType) {
			var isDeclaringType = SymbolEqualityComparer.Default.Equals(current, typeSymbol);
			foreach (var method in current.GetMembers().OfType<IMethodSymbol>()) {
				// Inherited private methods are not accessible to the generated subclass.
				if (method.MethodKind != MethodKind.Ordinary || method.IsStatic
					|| (publicOnly && method.DeclaredAccessibility != Accessibility.Public)
					|| (!publicOnly && !isDeclaringType && method.DeclaredAccessibility == Accessibility.Private)
					|| !method.Name.StartsWith("on_", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}
				if (!isDeclaringType && IsOverriddenByExisting(methods, method)) {
					continue;
				}
				methods.Add(method);
			}
		}
		return methods;
	}

	private static EquatableArray<DispatchParameterModel> BuildParameters(
		IMethodSymbol method, INamedTypeSymbol? scriptArgsType, int scriptArgsParameterIndex) {
		var parameters = new List<DispatchParameterModel>();
		for (var i = 0; i < method.Parameters.Length; i++) {
			var parameterType = method.Parameters[i].Type;
			// A lone ScriptArgs argument after the optional self parameter receives sa directly.
			if (i == scriptArgsParameterIndex && method.Parameters.Length == scriptArgsParameterIndex + 1
				&& scriptArgsType is not null && IsScriptArgsAssignable(parameterType, scriptArgsType)) {
				parameters.Add(BuildScriptArgsParameter(parameterType, scriptArgsType));
			} else {
				parameters.Add(BuildInvocationParameter(parameterType));
			}
		}
		return EquatableArray<DispatchParameterModel>.From(parameters);
	}

	internal static DispatchMethodModel BuildMethod(IMethodSymbol method, INamedTypeSymbol? scriptArgsType, int scriptArgsParameterIndex) {
		// Discovery already checked the on_ prefix; dispatch uses the remaining name.
		return new DispatchMethodModel(method.Name.Substring(3), method.Name, method.ReturnsVoid,
			BuildParameters(method, scriptArgsType, scriptArgsParameterIndex));
	}

	private static bool IsOverriddenByExisting(IEnumerable<IMethodSymbol> methods, IMethodSymbol baseMethod) {
		foreach (var method in methods) {
			for (var current = method; current is not null; current = current.OverriddenMethod) {
				if (SymbolEqualityComparer.Default.Equals(current, baseMethod)) {
					return true;
				}
			}
		}
		return false;
	}

	internal static DispatchParameterModel BuildScriptArgsParameter(ITypeSymbol parameterType, INamedTypeSymbol scriptArgsType) {
		if (SymbolEqualityComparer.Default.Equals(parameterType, scriptArgsType)) {
			// A ScriptArgs subclass needs a cast from the Run method's ScriptArgs parameter.
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
				// Dispatch converts enums through Int32 before casting to the enum type.
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

	// Also used by script holders, which choose their own enum conversion.
	internal static bool IsNumericOrDateTime(ITypeSymbol typeSymbol) {
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
