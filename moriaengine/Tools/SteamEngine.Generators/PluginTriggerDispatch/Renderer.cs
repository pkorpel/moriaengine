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

		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel)}partial class {model.TypeName} {{");
		builder.AppendLine();
		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 1)}public override object Run(global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa) {{");
		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 2)}switch (tk.LowercaseName) {{");

		foreach (var method in model.TriggerMethods) {
			builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 3)}case {DispatchRendererHelpers.ToCSharpStringLiteral(method.TriggerName.ToLowerInvariant())}:");
			var args = string.Join(", ", method.Parameters.Select((parameter, index) => RenderArgumentExpression(parameter, index)));
			var invocation = "this." + method.MethodName + "(" + args + ")";
			if (method.ReturnsVoid) {
				builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 4)}{invocation};");
				builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 4)}return null;");
			} else {
				builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 4)}return {invocation};");
			}
		}

		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 3)}default:");
		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 4)}return null;");
		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 2)}}}");
		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel + 1)}}}");
		builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel)}}}");

		if (model.NamespaceName.Length > 0) {
			indentLevel--;
			builder.AppendLine($"{DispatchRendererHelpers.Indent(indentLevel)}}}");
		}

		source = builder.ToString();
		return true;
	}

	private static string RenderArgumentExpression(DispatchParameterModel parameter, int parameterIndex) {
		string inputExpression;
		if (parameter.UsesScriptArgsInput) {
			inputExpression = "sa";
		} else {
			inputExpression = "sa.Argv[" + parameterIndex + "]";
		}

		return DispatchRendererHelpers.ApplyConversion(parameter, inputExpression);
	}
}
