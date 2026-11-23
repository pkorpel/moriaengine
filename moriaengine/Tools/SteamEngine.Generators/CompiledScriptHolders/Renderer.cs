using System.Linq;
using System.Text;

namespace SteamEngine.Generators.CompiledScriptHolders;

// Renders a compiled holder that invokes the modeled script function.
internal static class Renderer {
	internal static string GetHintName(ScriptHolderModel model) => "CompiledScriptHolders/" + model.GeneratedTypeName + ".g.cs";

	internal static string Render(ScriptHolderModel model) {
		var builder = new StringBuilder();
		builder.AppendLine("namespace SteamEngine.CompiledScripts {");
		builder.AppendLine($"\tpublic sealed class {model.GeneratedTypeName} : CompiledScriptHolder {{");
		builder.AppendLine($"\t\tpublic {model.GeneratedTypeName}()");
		builder.AppendLine($"\t\t\t: base({ToCSharpStringLiteral(model.FunctionName)}, {ToCSharpStringLiteral(model.Description)}) {{");
		builder.AppendLine("\t\t\tthis.RegisterAsFunction();");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
		builder.AppendLine("\t\tpublic override object Run(object self, global::SteamEngine.ScriptArgs sa) {");
		if (model.NeedsArgvArray) {
			builder.AppendLine("\t\t\tvar argv = sa.Argv;");
		}

		var args = string.Join(", ", model.Parameters.Select(p => p.Expression));
		var invocation = model.DeclaringTypeName + "." + model.MethodName + "(" + args + ")";
		if (model.ReturnsVoid) {
			builder.AppendLine("\t\t\t" + invocation + ";");
			builder.AppendLine("\t\t\treturn null;");
		} else {
			builder.AppendLine("\t\t\treturn " + invocation + ";");
		}

		builder.AppendLine("\t\t}");
		builder.AppendLine("\t}");
		builder.AppendLine("}");
		return builder.ToString();
	}

	private static string ToCSharpStringLiteral(string? value) {
		if (value is null) {
			return "null";
		}

		var escaped = value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\r", "\\r")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t");
		return "\"" + escaped + "\"";
	}
}
