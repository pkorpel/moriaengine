using System.Text;

namespace SteamEngine.Generators.DeepCopyImplementors;

// Renders deep-copy implementors from validated models.
internal static class Renderer {
	internal static string GetHintName(DeepCopyModel model) => "DeepCopyImplementors/" + model.GeneratedTypeName + ".g.cs";

	internal static string Render(DeepCopyModel model) {
		var builder = new StringBuilder();
		builder.AppendLine("namespace SteamEngine.CompiledScripts {");
		builder.AppendLine($"\tpublic sealed class {model.GeneratedTypeName} : global::SteamEngine.IDeepCopyImplementor {{");

		AppendHandledTypeProperty(builder, model);
		AppendDelayedCopyMethods(builder, model);
		AppendDeepCopyMethod(builder, model);
		builder.AppendLine("\t}");
		builder.AppendLine("}");

		return builder.ToString();
	}

	private static void AppendHandledTypeProperty(StringBuilder builder, DeepCopyModel model) {
		builder.AppendLine("\t\tpublic global::System.Type HandledType {");
		builder.AppendLine("\t\t\tget {");
		builder.AppendLine($"\t\t\t\treturn typeof({model.QualifiedTypeName});");
		builder.AppendLine("\t\t\t}");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	private static void AppendDelayedCopyMethods(StringBuilder builder, DeepCopyModel model) {
		foreach (var member in model.Members) {
			if (!MemberValueCopyRenderer.NeedsDelayed(member.ValueKind)) {
				continue;
			}

			var methodName = GetDelayedMethodName(member.MemberName);
			MemberValueCopyRenderer.AppendDelayedMethodWithSelfParameter(
				builder,
				member.ValueKind,
				methodName,
				model.QualifiedTypeName,
				member.MemberName,
				member.MemberTypeName);
		}
	}

	private static void AppendDeepCopyMethod(StringBuilder builder, DeepCopyModel model) {
		builder.AppendLine("\t\tpublic object DeepCopy(object copyFrom) {");
		builder.AppendLine($"\t\t\t{model.QualifiedTypeName} copyFromConverted = ({model.QualifiedTypeName}) copyFrom;");
		builder.AppendLine($"\t\t\t{model.QualifiedTypeName} copy = {GetInitializerExpression(model)};");

		foreach (var member in model.Members) {
			if (!MemberValueCopyRenderer.NeedsDelayed(member.ValueKind)) {
				builder.AppendLine($"\t\t\tcopy.{member.MemberName} = copyFromConverted.{member.MemberName};");
				continue;
			}

			var methodName = GetDelayedMethodName(member.MemberName);
			builder.AppendLine($"\t\t\tglobal::SteamEngine.DeepCopyFactory.GetCopyDelayed(copyFromConverted.{member.MemberName}, this.{methodName}, copy);");
		}

		builder.AppendLine("\t\t\treturn copy;");
		builder.AppendLine("\t\t}");
	}

	private static string GetInitializerExpression(DeepCopyModel model) {
		var argument = model.Initializer.AcceptsSourceParameter ? "copyFromConverted" : string.Empty;
		return model.Initializer.Kind switch {
			DeepCopyInitializerKind.Constructor => argument.Length == 0
				? "new " + model.QualifiedTypeName + "()"
				: "new " + model.QualifiedTypeName + "(" + argument + ")",
			DeepCopyInitializerKind.StaticMethod => argument.Length == 0
				? "(" + model.QualifiedTypeName + ") " + model.QualifiedTypeName + "." + model.Initializer.InitializerName + "()"
				: "(" + model.QualifiedTypeName + ") " + model.QualifiedTypeName + "." + model.Initializer.InitializerName + "(" + argument + ")",
			_ => throw new System.InvalidOperationException("Unsupported deep-copy initializer kind.")
		};
	}

	private static string GetDelayedMethodName(string memberName) {
		return "DelayedCopy_" + ToPascal(memberName);
	}

	private static string ToPascal(string name) {
		if (string.IsNullOrEmpty(name)) {
			return name;
		}
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}
}
