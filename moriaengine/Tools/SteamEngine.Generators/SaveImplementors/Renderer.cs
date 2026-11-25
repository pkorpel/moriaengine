using System;
using System.Text;

namespace SteamEngine.Generators.SaveImplementors;

// Renders persistence implementors from validated models.
internal static class Renderer {
	internal static string GetHintName(SaveImplementorModel model) => "SaveImplementors/" + model.GeneratedTypeName + ".g.cs";

	internal static string Render(SaveImplementorModel model) {
		var builder = new StringBuilder();
		builder.AppendLine("using System;");
		builder.AppendLine("using SteamEngine.Persistence;");
		builder.AppendLine();
		builder.AppendLine("namespace SteamEngine.CompiledScripts {");
		builder.AppendLine($"\tpublic sealed class {model.GeneratedTypeName} : global::SteamEngine.Persistence.DecoratedClassesSaveImplementor {{");
		AppendConstructor(builder, model);
		AppendLoadLineImpl(builder, model);
		AppendDelayedLoadMethods(builder, model);
		AppendLoadSection(builder, model);
		AppendSave(builder, model);
		builder.AppendLine("\t}");
		builder.AppendLine("}");
		return builder.ToString();
	}

	private static void AppendConstructor(StringBuilder builder, SaveImplementorModel model) {
		builder.AppendLine();
		builder.AppendLine($"\t\tpublic {model.GeneratedTypeName}()");
		builder.AppendLine($"\t\t\t: base(typeof({model.QualifiedTypeName}), \"{EscapeString(model.HeaderName)}\") {{");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	private static void AppendLoadLineImpl(StringBuilder builder, SaveImplementorModel model) {
		if (model.LoadLineMethodName is null) {
			return;
		}

		builder.AppendLine("\t\tprotected override void LoadLineImpl(object loadedObject, string filename, int line, string name, string value) {");
		builder.AppendLine($"\t\t\t{model.QualifiedTypeName} castLoadedObject = ({model.QualifiedTypeName}) loadedObject;");
		builder.AppendLine($"\t\t\tcastLoadedObject.{model.LoadLineMethodName}(filename, line, name, value);");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	private static void AppendDelayedLoadMethods(StringBuilder builder, SaveImplementorModel model) {
		foreach (var member in model.SaveableMembers) {
			if (!MemberValueLoadRenderer.NeedsDelayed(member.ValueKind)) {
				continue;
			}

			var methodName = "DelayedLoad_" + ToPascal(member.Name);
			builder.AppendLine($"\t\tprivate void {methodName}(object resolvedObject, string filename, int line, object param) {{");
			builder.AppendLine($"\t\t\t{model.QualifiedTypeName} loadedObject = ({model.QualifiedTypeName}) param;");
			var delayedExpression = MemberValueLoadRenderer.GetDelayedExpression(member.ValueKind, member.MemberTypeName);
			builder.AppendLine($"\t\t\tloadedObject.{member.Name} = {delayedExpression};");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
		}
	}

	private static void AppendLoadSection(StringBuilder builder, SaveImplementorModel model) {
		builder.AppendLine("\t\tpublic override object LoadSection(global::SteamEngine.PropsSection input) {");
		builder.AppendLine("\t\t\tint currentLineNumber = input.HeaderLine;");
		builder.AppendLine("\t\t\ttry {");
		builder.AppendLine($"\t\t\t\t{model.QualifiedTypeName} loadedObject = {GetLoadFactoryExpression(model)};");

		if (!model.LoadFactory.TakesPropsSection) {
			for (var i = 0; i < model.SaveableMembers.Count; i++) {
				AppendLoadMember(builder, model.SaveableMembers[i], i);
			}
		}

		if (model.LoadLineMethodName is not null) {
			builder.AppendLine("\t\t\t\tthis.LoadSectionLines(input, loadedObject);");
		}
		if (model.LoadingFinalizerMethodName is not null) {
			builder.AppendLine($"\t\t\t\tloadedObject.{model.LoadingFinalizerMethodName}();");
		}

		builder.AppendLine("\t\t\t\treturn loadedObject;");
		builder.AppendLine("\t\t\t} catch (global::SteamEngine.FatalException) {");
		builder.AppendLine("\t\t\t\tthrow;");
		builder.AppendLine("\t\t\t} catch (global::SteamEngine.SEException sex) {");
		builder.AppendLine("\t\t\t\tsex.TryAddFileLineInfo(input.Filename, currentLineNumber);");
		builder.AppendLine("\t\t\t\tthrow;");
		builder.AppendLine("\t\t\t} catch (global::System.Exception e) {");
		builder.AppendLine("\t\t\t\tthrow new global::SteamEngine.SEException(input.Filename, currentLineNumber, e);");
		builder.AppendLine("\t\t\t}");
		builder.AppendLine("\t\t}");
		builder.AppendLine();
	}

	private static void AppendLoadMember(StringBuilder builder, SaveableMemberModel member, int memberIndex) {
		var propsLineName = "propsLine_" + memberIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
		builder.AppendLine($"\t\t\t\tglobal::SteamEngine.PropsLine {propsLineName} = input.TryPopPropsLine(\"{EscapeString(member.Name)}\");");
		builder.AppendLine($"\t\t\t\tif ({propsLineName} == null) {{");
		builder.AppendLine($"\t\t\t\t\tglobal::SteamEngine.CoreLogger.WriteWarning(input.Filename, input.HeaderLine, \"Missing value '{EscapeString(member.Name)}' in loaded section\");");
		builder.AppendLine("\t\t\t\t} else {");
		builder.AppendLine($"\t\t\t\t\tcurrentLineNumber = {propsLineName}.Line;");
		if (MemberValueLoadRenderer.NeedsDelayed(member.ValueKind)) {
			var methodName = "DelayedLoad_" + ToPascal(member.Name);
			builder.AppendLine($"\t\t\t\t\tglobal::SteamEngine.Persistence.ObjectSaver.Load({propsLineName}.Value, this.{methodName}, input.Filename, {propsLineName}.Line, loadedObject);");
		} else {
			var expression = MemberValueLoadRenderer.GetSimpleExpression(member.ValueKind, member.MemberTypeName)
				.Replace("valueString", propsLineName + ".Value");
			builder.AppendLine($"\t\t\t\t\tloadedObject.{member.Name} = {expression};");
		}
		builder.AppendLine("\t\t\t\t}");
	}

	private static string GetLoadFactoryExpression(SaveImplementorModel model) {
		var arg = model.LoadFactory.TakesPropsSection ? "input" : string.Empty;
		return model.LoadFactory.Kind switch {
			SaveFactoryKind.Constructor => arg.Length == 0
				? $"new {model.QualifiedTypeName}()"
				: $"new {model.QualifiedTypeName}({arg})",
			SaveFactoryKind.StaticMethod => GetStaticFactoryExpression(model, arg),
			_ => throw new InvalidOperationException("Unsupported save factory kind.")
		};
	}

	private static string GetStaticFactoryExpression(SaveImplementorModel model, string arg) {
		var invocation = arg.Length == 0
			? $"{model.LoadFactory.DeclaringTypeName}.{model.LoadFactory.MethodName}()"
			: $"{model.LoadFactory.DeclaringTypeName}.{model.LoadFactory.MethodName}({arg})";
		return model.LoadFactory.RequiresCast
			? $"({model.QualifiedTypeName}) {invocation}"
			: invocation;
	}

	private static void AppendSave(StringBuilder builder, SaveImplementorModel model) {
		builder.AppendLine("\t\tpublic override void Save(object objToSave, global::SteamEngine.Persistence.SaveStream writer) {");
		builder.AppendLine($"\t\t\t{model.QualifiedTypeName} castObjToSave = ({model.QualifiedTypeName}) objToSave;");

		if (!model.LoadFactory.TakesPropsSection) {
			foreach (var member in model.SaveableMembers) {
				builder.AppendLine($"\t\t\twriter.WriteValue(\"{EscapeString(member.Name)}\", castObjToSave.{member.Name});");
			}
		}

		if (model.SaveMethodName is not null) {
			builder.AppendLine($"\t\t\tcastObjToSave.{model.SaveMethodName}(writer);");
		}

		builder.AppendLine("\t\t}");
	}

	private static string EscapeString(string value) {
		return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
	}

	private static string ToPascal(string name) {
		if (string.IsNullOrEmpty(name)) {
			return name;
		}
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}
}
