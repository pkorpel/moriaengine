using System;
using System.Linq;
using System.Text;

namespace SteamEngine.Generators;

// Shared source renderer for plugin and compiled-trigger-group Run overrides.
internal static class DispatchRenderer {
	// Trigger groups include a self parameter; plugin dispatch receives only the trigger and script arguments.
	internal static string Render(string namespaceName, string typeName, EquatableArray<DispatchMethodModel> methods, bool hasSelf) {
		var builder = new StringBuilder();
		var indentLevel = 0;
		if (namespaceName.Length > 0) {
			builder.AppendLine($"namespace {namespaceName} {{");
			indentLevel++;
		}

		builder.AppendLine($"{Indent(indentLevel)}partial class {typeName} {{");
		builder.AppendLine();
		var selfParameter = hasSelf ? "object self, " : string.Empty;
		builder.AppendLine($"{Indent(indentLevel + 1)}public override object Run({selfParameter}global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa) {{");
		// Trigger names are matched case-insensitively via their lowercase form.
		builder.AppendLine($"{Indent(indentLevel + 2)}switch (tk.LowercaseName) {{");
		foreach (var method in methods) {
			builder.AppendLine($"{Indent(indentLevel + 3)}case {ToCSharpStringLiteral(method.TriggerName.ToLowerInvariant())}:");
			// A lone ScriptArgs parameter after optional self receives sa directly; other arguments use sa.Argv.
			// Trigger groups take self before the arguments from sa; plugins do not.
			var args = string.Join(", ", method.Parameters.Select((parameter, index) => {
				var input = hasSelf && index == 0 ? "self"
					: parameter.UsesScriptArgsInput ? "sa"
					: "sa.Argv[" + (index - (hasSelf ? 1 : 0)) + "]";
				return ApplyConversion(parameter, input);
			}));
			var invocation = "this." + method.MethodName + "(" + args + ")";
			if (method.ReturnsVoid) {
				builder.AppendLine($"{Indent(indentLevel + 4)}{invocation};");
				builder.AppendLine($"{Indent(indentLevel + 4)}return null;");
			} else {
				builder.AppendLine($"{Indent(indentLevel + 4)}return {invocation};");
			}
		}
		// An unknown trigger, like a void handler, returns null from Run.
		builder.AppendLine($"{Indent(indentLevel + 3)}default:");
		builder.AppendLine($"{Indent(indentLevel + 4)}return null;");
		builder.AppendLine($"{Indent(indentLevel + 2)}}}");
		builder.AppendLine($"{Indent(indentLevel + 1)}}}");
		builder.AppendLine($"{Indent(indentLevel)}}}");
		if (namespaceName.Length > 0) {
			builder.AppendLine("}");
		}
		return builder.ToString();
	}

	private static string Indent(int level) {
		return new string('\t', level);
	}

	private static string ToCSharpStringLiteral(string value) {
		var escaped = value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\r", "\\r")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t");
		return "\"" + escaped + "\"";
	}

	private static string ApplyConversion(DispatchParameterModel parameter, string inputExpression) {
		// Dispatch enums use Int32 conversion; script-function enum conversion is separate.
		return parameter.Conversion switch {
			DispatchParameterConversion.None => inputExpression,
			DispatchParameterConversion.Cast => "(" + parameter.TypeName + ") " + inputExpression,
			DispatchParameterConversion.Convert => "global::System.Convert.To" + parameter.TypeName + "(" + inputExpression + ")",
			DispatchParameterConversion.EnumConvertThenCast => "(" + parameter.TypeName + ")global::System.Convert.ToInt32(" + inputExpression + ")",
			_ => inputExpression
		};
	}
}
