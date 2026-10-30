using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#844 - the title ladders in scripts/Basic/Professions/Definitions: a title follows the rank word ("Master
	/// Sorcerer"), so none can contain one, and each step of a ladder must read differently.</summary>
	public class ProfessionTitleLadderTests {
		private static readonly string[] rankWords = { "Novice", "Expert", "Master", "Grandmaster" };

		private static string DefinitionsDirectory() {
			DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null) {
				string path = Path.Combine(dir.FullName, "scripts", "Basic", "Professions", "Definitions");
				if (Directory.Exists(path)) {
					return path;
				}
				dir = dir.Parent;
			}
			throw new DirectoryNotFoundException("scripts/Basic/Professions/Definitions");
		}

		private static string[] Ladder(string text, string key) {
			Match line = Regex.Match(text, "^" + key + "=(.*?)\\r?$", RegexOptions.Multiline);
			Assert.True(line.Success, "no " + key);
			return line.Groups[1].Value.Split(',').Select(t => t.Trim()).ToArray();
		}

		[Theory]
		[InlineData("mage", 10), InlineData("priest", 10), InlineData("ranger", 10), InlineData("warrior", 10), InlineData("thief", 10)]
		[InlineData("necromant", 10), InlineData("shaman", 10), InlineData("mystic", 10), InlineData("craftsman", 11)]
		public void LaddersReadWellAfterARankWord(string profession, int length) {
			string text = File.ReadAllText(Path.Combine(DefinitionsDirectory(), profession + ".def"));
			foreach (string[] ladder in new[] { Ladder(text, "titles"), Ladder(text, "titlesFemale") }) {
				Assert.Equal(length, ladder.Length);
				Assert.Equal(length, ladder.Distinct(StringComparer.OrdinalIgnoreCase).Count());
				foreach (string quoted in ladder) {
					Assert.True(quoted.StartsWith('"') && quoted.EndsWith('"'), "unquoted, the script parser reads it as a name: " + quoted);
					string title = quoted.Trim('"');
					Assert.True(char.IsUpper(title[0]), title);
					Assert.DoesNotContain(rankWords, word => title.Contains(word, StringComparison.OrdinalIgnoreCase));
				}
			}
		}
	}
}
