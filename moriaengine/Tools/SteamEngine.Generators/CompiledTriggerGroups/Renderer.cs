using System.Linq;
using System.Text;

namespace SteamEngine.Generators.CompiledTriggerGroups;

internal static class Renderer {
	internal static string GetHintName(CompiledTriggerGroupModel model) => "CompiledTriggerGroups/" + model.TypeName + ".g.cs";

	internal static bool TryRender(CompiledTriggerGroupModel model, out string source) {
		var builder = new StringBuilder();
		if (model.NamespaceName.Length > 0) {
			builder.AppendLine($"namespace {model.NamespaceName} {{");
		}
		builder.AppendLine($"\tpartial class {model.TypeName} {{");
		builder.AppendLine();
		builder.AppendLine("\t\tpublic override object Run(object self, global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa) {");

		if (model.TriggerMethods.Count > 0) {
			foreach (var method in model.TriggerMethods) {
				builder.AppendLine($"\t\t\tif (tk is not null && global::System.StringComparer.OrdinalIgnoreCase.Equals(tk.Name, {ToCSharpStringLiteral(method.TriggerName)})) {{");
				var args = string.Join(", ", method.Parameters.Select((parameter, index) => RenderArgumentExpression(parameter, index)));
				var invocation = "this." + method.MethodName + "(" + args + ")";
				if (method.ReturnsVoid) {
					builder.AppendLine("\t\t\t\t" + invocation + ";");
					builder.AppendLine("\t\t\t\treturn null;");
				} else {
					builder.AppendLine("\t\t\t\treturn " + invocation + ";");
				}
				builder.AppendLine("\t\t\t}");
			}
		}

		builder.AppendLine("\t\t\treturn null;");
		builder.AppendLine("\t\t}");
		builder.AppendLine("\t}");
		if (model.NamespaceName.Length > 0) {
			builder.AppendLine("}");
		}

		source = builder.ToString();
		return true;
	}

	private static string RenderArgumentExpression(InvocationParameterModel parameter, int parameterIndex) {
		// Positional wiring is renderer-only: first argument is always self, remaining
		// arguments come from either ScriptArgs (semantic flag) or argv[index - 1].
		string inputExpression;
		if (parameterIndex == 0) {
			inputExpression = "self";
		} else if (parameter.UsesScriptArgsInput) {
			inputExpression = "sa";
		} else {
			inputExpression = "sa.Argv[" + (parameterIndex - 1) + "]";
		}

		switch (parameter.Conversion) {
			case InvocationParameterConversion.None:
				return inputExpression;
			case InvocationParameterConversion.Cast:
				return "(" + parameter.TypeName + ") " + inputExpression;
			case InvocationParameterConversion.Convert:
				return "global::System.Convert.To" + parameter.TypeName + "(" + inputExpression + ")";
			case InvocationParameterConversion.EnumConvertThenCast:
				return "(" + parameter.TypeName + ")global::System.Convert.ToInt32(" + inputExpression + ")";
			default:
				return inputExpression;
		}
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
}
