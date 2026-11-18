using System;
using System.Text;

namespace SteamEngine.Generators;

internal static class MemberValueCopyRenderer {
	internal static bool NeedsDelayed(MemberValueKind kind) {
		return kind switch {
			MemberValueKind.Object => true,
			MemberValueKind.Fallback => true,
			_ => false
		};
	}

	internal static void AppendAssignment(
		StringBuilder builder,
		MemberValueKind kind,
		string fieldName,
		string typeName) {
		if (!NeedsDelayed(kind)) {
			builder.AppendLine($"\t\t\tthis.{fieldName} = copyFrom.{fieldName};");
			return;
		}

		var methodName = "DelayedCopy_" + GetPascal(fieldName);
		builder.AppendLine($"\t\t\tDeepCopyFactory.GetCopyDelayed(copyFrom.{fieldName}, this.{methodName});");
	}

	internal static void AppendDelayedMethod(
		StringBuilder builder,
		MemberValueKind kind,
		string fieldName,
		string typeName) {
		if (!NeedsDelayed(kind)) {
			throw GetUnsupportedKindException(kind);
		}

		var methodName = "DelayedCopy_" + GetPascal(fieldName);
		builder.AppendLine($"\t\tprivate void {methodName}(object copiedValue) {{");
		builder.AppendLine($"\t\t\tthis.{fieldName} = {GetDelayedAssignmentExpression(kind, typeName)};");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	internal static void AppendDelayedMethodWithSelfParameter(
		StringBuilder builder,
		MemberValueKind kind,
		string methodName,
		string ownerTypeName,
		string targetMemberName,
		string typeName) {
		if (!NeedsDelayed(kind)) {
			throw GetUnsupportedKindException(kind);
		}

		builder.AppendLine($"\t\tpublic void {methodName}(object copiedValue, object self) {{");
		builder.AppendLine($"\t\t\t{ownerTypeName} copyConverted = ({ownerTypeName}) self;");
		builder.AppendLine($"\t\t\tcopyConverted.{targetMemberName} = {GetDelayedAssignmentExpression(kind, typeName)};");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	internal static string GetDelayedAssignmentExpression(
		MemberValueKind kind,
		string typeName) {
		if (!NeedsDelayed(kind)) {
			throw GetUnsupportedKindException(kind);
		}

		return kind switch {
			MemberValueKind.Object => "copiedValue",
			MemberValueKind.Fallback => $"({typeName}) copiedValue",
			_ => throw GetUnsupportedKindException(kind)
		};
	}

	private static string GetPascal(string name) {
		if (string.IsNullOrEmpty(name)) {
			return name;
		}
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	private static InvalidOperationException GetUnsupportedKindException(MemberValueKind kind) {
		return new InvalidOperationException("MemberValueKind " + kind + " is not supported for delayed copy expressions.");
	}
}
