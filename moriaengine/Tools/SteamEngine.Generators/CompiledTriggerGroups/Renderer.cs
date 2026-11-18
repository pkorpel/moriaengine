using System.Linq;
using System.Text;

namespace SteamEngine.Generators.CompiledTriggerGroups;

internal static class Renderer {
	internal static string GetHintName(CompiledTriggerGroupModel model) => "CompiledTriggerGroups/" + model.TypeName + ".g.cs";

	internal static bool TryRender(CompiledTriggerGroupModel model, out string source) {
		var builder = new StringBuilder();
		var indentLevel = 0;
		if (model.NamespaceName.Length > 0) {
			builder.AppendLine($"namespace {model.NamespaceName} {{");
			indentLevel++;
		}

		foreach (var containingType in model.ContainingTypes) {
			builder.AppendLine($"{Indent(indentLevel)}{containingType.Accessibility} {RenderModifiers(containingType.IsStatic, containingType.IsAbstract, containingType.IsSealed)}partial class {containingType.TypeName} {{");
			indentLevel++;
		}

		builder.AppendLine($"{Indent(indentLevel)}{model.TypeAccessibility} partial class {model.TypeName} {{");
		builder.AppendLine();
		builder.AppendLine($"{Indent(indentLevel + 1)}public override object Run(object self, global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa) {{");

		if (model.TriggerMethods.Count > 0) {
			foreach (var method in model.TriggerMethods) {
				builder.AppendLine($"{Indent(indentLevel + 2)}if (global::System.Object.ReferenceEquals(tk, global::SteamEngine.TriggerKey.Acquire({ToCSharpStringLiteral(method.TriggerName)}))) {{");
				var args = string.Join(", ", method.Parameters.Select((parameter, index) => RenderArgumentExpression(parameter, index)));
				var invocation = "this." + method.MethodName + "(" + args + ")";
				if (method.ReturnsVoid) {
					builder.AppendLine($"{Indent(indentLevel + 3)}{invocation};");
					builder.AppendLine($"{Indent(indentLevel + 3)}return null;");
				} else {
					builder.AppendLine($"{Indent(indentLevel + 3)}return {invocation};");
				}
				builder.AppendLine($"{Indent(indentLevel + 2)}}}");
			}
		}

		builder.AppendLine($"{Indent(indentLevel + 2)}return null;");
		builder.AppendLine($"{Indent(indentLevel + 1)}}}");
		builder.AppendLine($"{Indent(indentLevel)}}}");
		for (var i = model.ContainingTypes.Count - 1; i >= 0; i--) {
			indentLevel--;
			builder.AppendLine($"{Indent(indentLevel)}}}");
		}
		if (model.NamespaceName.Length > 0) {
			indentLevel--;
			builder.AppendLine($"{Indent(indentLevel)}}}");
		}

		source = builder.ToString();
		return true;
	}

	private static string RenderModifiers(bool isStatic, bool isAbstract, bool isSealed) {
		if (isStatic) {
			return "static ";
		}
		if (isAbstract && !isSealed) {
			return "abstract ";
		}
		if (isSealed && !isAbstract) {
			return "sealed ";
		}
		return string.Empty;
	}

	private static string Indent(int level) {
		return new string('\t', level);
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
