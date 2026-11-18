using System.Text;

namespace SteamEngine.Generators;

internal static class IdentifierSanitizer {
	internal static string Sanitize(string sourceName) {
		if (string.IsNullOrEmpty(sourceName)) {
			return "Unnamed";
		}

		var builder = new StringBuilder(sourceName.Length);
		foreach (var ch in sourceName) {
			builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
		}

		if (builder.Length == 0) {
			return "Unnamed";
		}

		if (char.IsDigit(builder[0])) {
			builder.Insert(0, '_');
		}

		return builder.ToString();
	}
}
