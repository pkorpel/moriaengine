using System;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal static class MemberValueKindClassifier {
	internal static MemberValueKind GetMemberValueKind(
		ITypeSymbol type,
		CanonicalTypeSymbols knownTypes,
		Action<ITypeSymbol, INamedTypeSymbol> reportTypeNameConflict) {
		if (type.TypeKind == TypeKind.Enum) {
			return MemberValueKind.Enum;
		}
		if (type.SpecialType == SpecialType.System_Object) {
			return MemberValueKind.Object;
		}

		switch (type.SpecialType) {
			case SpecialType.System_String: return MemberValueKind.String;
			case SpecialType.System_Boolean: return MemberValueKind.Boolean;
			case SpecialType.System_Byte: return MemberValueKind.Byte;
			case SpecialType.System_SByte: return MemberValueKind.SByte;
			case SpecialType.System_Int16: return MemberValueKind.Int16;
			case SpecialType.System_UInt16: return MemberValueKind.UInt16;
			case SpecialType.System_Int32: return MemberValueKind.Int32;
			case SpecialType.System_UInt32: return MemberValueKind.UInt32;
			case SpecialType.System_Int64: return MemberValueKind.Int64;
			case SpecialType.System_UInt64: return MemberValueKind.UInt64;
			case SpecialType.System_Single: return MemberValueKind.Single;
			case SpecialType.System_Double: return MemberValueKind.Double;
			case SpecialType.System_Decimal: return MemberValueKind.Decimal;
			case SpecialType.System_Char: return MemberValueKind.Char;
		}

		if (string.Equals(type.Name, "DateTime", StringComparison.Ordinal)) {
			return MemberValueKind.DateTime;
		}
		if (string.Equals(type.Name, "TimeSpan", StringComparison.Ordinal)) {
			return MemberValueKind.TimeSpan;
		}

		if (IsTypeOrBase(type, knownTypes.ThingType, "Thing", reportTypeNameConflict)) {
			return MemberValueKind.Thing;
		}
		if (IsTypeOrBase(type, knownTypes.GlobalsType, "Globals", reportTypeNameConflict)) {
			return MemberValueKind.Globals;
		}
		if (IsTypeOrBase(type, knownTypes.AbstractScriptType, "AbstractScript", reportTypeNameConflict)) {
			return MemberValueKind.AbstractScript;
		}
		if (IsTypeOrBase(type, knownTypes.RegionType, "Region", reportTypeNameConflict)) {
			return MemberValueKind.Region;
		}
		if (ImplementsInterface(type, knownTypes.IAbstractKeyType, "IAbstractKey", reportTypeNameConflict)) {
			return MemberValueKind.IAbstractKey;
		}
		if (IsTypeOrBase(type, knownTypes.AbstractAccountType, "AbstractAccount", reportTypeNameConflict)) {
			return MemberValueKind.AbstractAccount;
		}
		return MemberValueKind.Fallback;
	}

	private static bool IsTypeOrBase(
		ITypeSymbol type,
		INamedTypeSymbol canonicalType,
		string expectedName,
		Action<ITypeSymbol, INamedTypeSymbol> reportTypeNameConflict) {
		for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType) {
			if (SymbolEqualityComparer.Default.Equals(current, canonicalType)) {
				return true;
			}
			if (string.Equals(current.Name, expectedName, StringComparison.Ordinal)) {
				reportTypeNameConflict(current, canonicalType);
			}
		}
		return false;
	}

	private static bool ImplementsInterface(
		ITypeSymbol type,
		INamedTypeSymbol canonicalType,
		string expectedName,
		Action<ITypeSymbol, INamedTypeSymbol> reportTypeNameConflict) {
		foreach (var iface in type.AllInterfaces) {
			if (SymbolEqualityComparer.Default.Equals(iface, canonicalType)) {
				return true;
			}
			if (string.Equals(iface.Name, expectedName, StringComparison.Ordinal)) {
				reportTypeNameConflict(iface, canonicalType);
			}
		}
		return false;
	}
}
