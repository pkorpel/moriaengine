using System;

namespace SteamEngine.Generators;

internal static class DispatchRendererHelpers {
	internal static string Indent(int level) {
		return new string('\t', level);
	}

	internal static string ToCSharpStringLiteral(string value) {
		var escaped = value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\r", "\\r")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t");
		return "\"" + escaped + "\"";
	}

	internal static string ApplyConversion(DispatchParameterModel parameter, string inputExpression) {
		return parameter.Conversion switch {
			DispatchParameterConversion.None => inputExpression,
			DispatchParameterConversion.Cast => "(" + parameter.TypeName + ") " + inputExpression,
			DispatchParameterConversion.Convert => "global::System.Convert.To" + parameter.TypeName + "(" + inputExpression + ")",
			DispatchParameterConversion.EnumConvertThenCast => "(" + parameter.TypeName + ")global::System.Convert.ToInt32(" + inputExpression + ")",
			_ => inputExpression
		};
	}
}
