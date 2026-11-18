using System;
using System.Text;

namespace SteamEngine.Generators;

internal static class MemberValueLoadRenderer {
	internal static string GetSimpleExpression(MemberValueKind kind, string typeName) {
		return kind switch {
			MemberValueKind.String => "(string) ObjectSaver.OptimizedLoad_String(valueString)",
			MemberValueKind.Boolean => "SteamEngine.Common.ConvertTools.ParseBoolean(valueString)",
			MemberValueKind.Byte => "SteamEngine.Common.ConvertTools.ParseByte(valueString)",
			MemberValueKind.SByte => "SteamEngine.Common.ConvertTools.ParseSByte(valueString)",
			MemberValueKind.Int16 => "SteamEngine.Common.ConvertTools.ParseInt16(valueString)",
			MemberValueKind.UInt16 => "SteamEngine.Common.ConvertTools.ParseUInt16(valueString)",
			MemberValueKind.Int32 => "SteamEngine.Common.ConvertTools.ParseInt32(valueString)",
			MemberValueKind.UInt32 => "SteamEngine.Common.ConvertTools.ParseUInt32(valueString)",
			MemberValueKind.Int64 => "SteamEngine.Common.ConvertTools.ParseInt64(valueString)",
			MemberValueKind.UInt64 => "SteamEngine.Common.ConvertTools.ParseUInt64(valueString)",
			MemberValueKind.Single => "SteamEngine.Common.ConvertTools.ParseSingle(valueString)",
			MemberValueKind.Double => "SteamEngine.Common.ConvertTools.ParseDouble(valueString)",
			MemberValueKind.Decimal => "SteamEngine.Common.ConvertTools.ParseDecimal(valueString)",
			MemberValueKind.Char => $"Convert.ToChar(ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof({typeName})))",
			MemberValueKind.DateTime => $"Convert.ToDateTime(ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof({typeName})))",
			MemberValueKind.TimeSpan => $"({typeName}) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof({typeName}))",
			MemberValueKind.Globals => $"({typeName}) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof({typeName}))",
			_ => throw GetUnsupportedSimpleKindException(kind)
		};
	}

	internal static bool NeedsDelayed(MemberValueKind kind) {
		return kind switch {
			MemberValueKind.Enum => true,
			MemberValueKind.Object => true,
			MemberValueKind.Fallback => true,
			MemberValueKind.Thing => true,
			MemberValueKind.AbstractScript => true,
			MemberValueKind.Region => true,
			MemberValueKind.IAbstractKey => true,
			MemberValueKind.AbstractAccount => true,
			_ => false
		};
	}

	internal static void AppendDelayedMethod(
		StringBuilder builder,
		MemberValueKind kind,
		string fieldName,
		string typeName) {
		if (!NeedsDelayed(kind)) {
			throw GetUnsupportedDelayedKindException(kind);
		}

		var methodName = "DelayedLoad_" + GetPascal(fieldName);
		builder.AppendLine($"\t\tprivate void {methodName}(object resolvedObject, string filename, int line) {{");
		builder.AppendLine($"\t\t\tthis.{fieldName} = {GetDelayedExpression(kind, typeName)};");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	internal static string GetDelayedExpression(MemberValueKind kind, string typeName) {
		return kind switch {
			MemberValueKind.Enum => $"({typeName}) Convert.ToInt32(resolvedObject)",
			MemberValueKind.Object => "resolvedObject",
			MemberValueKind.Fallback
			or MemberValueKind.Thing
			or MemberValueKind.AbstractScript
			or MemberValueKind.Region
			or MemberValueKind.IAbstractKey
			or MemberValueKind.AbstractAccount => $"({typeName}) resolvedObject",
			_ => throw GetUnsupportedDelayedKindException(kind)
		};
	}

	private static string GetPascal(string name) {
		if (string.IsNullOrEmpty(name)) {
			return name;
		}
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	private static InvalidOperationException GetUnsupportedSimpleKindException(MemberValueKind kind) {
		return new InvalidOperationException("MemberValueKind " + kind + " is not supported for direct load expressions.");
	}

	private static InvalidOperationException GetUnsupportedDelayedKindException(MemberValueKind kind) {
		return new InvalidOperationException("MemberValueKind " + kind + " is not supported for delayed load expressions.");
	}
}
