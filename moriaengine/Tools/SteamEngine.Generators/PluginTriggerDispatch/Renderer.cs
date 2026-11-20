using System;
using System.Linq;
using System.Text;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal static class Renderer {
	internal static string GetHintName(PluginTriggerCandidate model) {
		var typePath = IdentifierSanitizer.Sanitize(model.TypeIdentity);
		return "PluginTriggerDispatch/" + typePath + ".g.cs";
	}

	internal static bool TryRender(PluginTriggerCandidate model, out string source) {
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
		builder.AppendLine($"{Indent(indentLevel + 1)}public override object Run(global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa) {{");
		builder.AppendLine($"{Indent(indentLevel + 2)}switch (tk.LowercaseName) {{");

		foreach (var method in model.TriggerMethods) {
			builder.AppendLine($"{Indent(indentLevel + 3)}case {ToCSharpStringLiteral(method.TriggerName.ToLowerInvariant())}:");
			var args = string.Join(", ", method.Parameters.Select((parameter, index) => RenderArgumentExpression(parameter, index)));
			var invocation = "this." + method.MethodName + "(" + args + ")";
			if (method.ReturnsVoid) {
				builder.AppendLine($"{Indent(indentLevel + 4)}{invocation};");
				builder.AppendLine($"{Indent(indentLevel + 4)}return null;");
			} else {
				builder.AppendLine($"{Indent(indentLevel + 4)}return {invocation};");
			}
		}

		builder.AppendLine($"{Indent(indentLevel + 3)}default:");
		builder.AppendLine($"{Indent(indentLevel + 4)}return null;");
		builder.AppendLine($"{Indent(indentLevel + 2)}}}");
		builder.AppendLine($"{Indent(indentLevel + 1)}}}");
		builder.AppendLine($"{Indent(indentLevel)}}}");

		for (var i = model.ContainingTypes.Length - 1; i >= 0; i--) {
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

	private static string RenderArgumentExpression(PluginInvocationParameterData parameter, int parameterIndex) {
		string inputExpression;
		if (parameter.UsesScriptArgsInput) {
			inputExpression = "sa";
		} else {
			inputExpression = "sa.Argv[" + parameterIndex + "]";
		}

		switch (parameter.Conversion) {
			case PluginInvocationParameterConversion.None:
				return inputExpression;
			case PluginInvocationParameterConversion.Cast:
				return "(" + parameter.TypeName + ") " + inputExpression;
			case PluginInvocationParameterConversion.Convert:
				return "global::System.Convert.To" + parameter.TypeName + "(" + inputExpression + ")";
			case PluginInvocationParameterConversion.EnumConvertThenCast:
				return "(" + parameter.TypeName + ")global::System.Convert.ToInt32(" + inputExpression + ")";
			default:
				return inputExpression;
		}
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
}
