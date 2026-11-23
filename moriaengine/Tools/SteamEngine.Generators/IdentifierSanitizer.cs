using System;
using System.Globalization;
using System.Text;

namespace SteamEngine.Generators;

internal static class IdentifierSanitizer {
	internal static string Sanitize(string sourceName) {
		// Reserve the prefix so escaped names cannot match unchanged names.
		const string prefix = "__encoded_";
		if (!string.IsNullOrEmpty(sourceName)
			&& !char.IsDigit(sourceName[0])
			&& !sourceName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
			var valid = true;
			foreach (var ch in sourceName) {
				if (!char.IsLetterOrDigit(ch) && ch != '_') {
					valid = false;
					break;
				}
			}
			if (valid) {
				return sourceName;
			}
		}

		var builder = new StringBuilder(prefix);
		foreach (var ch in sourceName) {
			if (char.IsLetterOrDigit(ch)) {
				builder.Append(ch);
			} else {
				AppendEscape(builder, ch);
			}
		}
		return builder.ToString();
	}

	internal static string EncodeHintName(string value) {
		// Hint names must remain distinct even when compared case-insensitively.
		var builder = new StringBuilder("n");
		foreach (var ch in value) {
			if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) {
				builder.Append(ch);
			} else {
				AppendEscape(builder, ch);
			}
		}
		return builder.ToString();
	}

	private static void AppendEscape(StringBuilder builder, char ch) {
		builder.Append('_').Append(((int) ch).ToString("x4", CultureInfo.InvariantCulture));
	}
}
