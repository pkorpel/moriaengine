using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#858 - Erebor's markup in the quest texts. What isn't expanded shows raw in a label, and in HTML the client drops
	/// it as an unknown tag: words just go missing. A text may sit in a branch nobody clicks, so every one is checked here,
	/// for each gender and realm.</summary>
	public class QuestDialogTextTests {
		private static string QuestsDirectory() {
			DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null) {
				string path = Path.Combine(dir.FullName, "scripts", "Basic", "Quests");
				if (Directory.Exists(path)) {
					return path;
				}
				dir = dir.Parent;
			}
			throw new DirectoryNotFoundException("scripts/Basic/Quests");
		}

		private static List<string> QuestTexts() {
			List<string> texts = new List<string>();
			foreach (string file in Directory.GetFiles(QuestsDirectory(), "*.cs", SearchOption.AllDirectories)) {
				foreach (Match m in Regex.Matches(File.ReadAllText(file), "\"((?:[^\"\\\\\\n]|\\\\.)*)\"")) {
					string text = m.Groups[1].Value.Replace("\\\"", "\"");
					if (text.Contains("<src", StringComparison.OrdinalIgnoreCase) || text.Contains("<name>", StringComparison.Ordinal) || text.Contains("<def_", StringComparison.Ordinal)) {
						texts.Add(text);
					}
				}
			}
			return texts;
		}

		[Fact]
		public void EveryQuestTextExpandsCompletely() {
			List<string> texts = QuestTexts();
			Assert.True(texts.Count > 50, "only found " + texts.Count + " texts");
			foreach (string text in texts) {
				foreach (bool male in new[] { true, false }) {
					foreach (bool gondor in new[] { true, false }) {
						string expanded = QuestDialogContent.ExpandMarkup(text, male, gondor, "Hero", "Npc");
						string withoutHtml = Regex.Replace(expanded, "</?(basefont|br|center)[^>]*>", "", RegexOptions.IgnoreCase);
						Assert.False(withoutHtml.Contains('<'), "left unexpanded: " + withoutHtml);
					}
				}
			}
		}

		[Theory]
		[InlineData("<src.sex(\"ten, kdo pomohl\",\"ta, ktera pomohla\")>", true, true, "ten, kdo pomohl")]
		[InlineData("<src.sex(\"ten, kdo pomohl\",\"ta, ktera pomohla\")>", false, true, "ta, ktera pomohla")]
		[InlineData("<src.splitRealm(\"Promin, ale ted nemohu...\",\"Hah! Na to ti cvrka pes ty blazniva!\")>", true, false, "Hah! Na to ti cvrka pes ty blazniva!")]
		[InlineData("<?src.sex(neslysel,neslysela)?>", false, true, "neslysela")]
		[InlineData("<src.splitRealm(\"<src.sex(\"a, b\",\"c\")>\",\"<src.sex(\"d\",\"e\")>\")>", true, true, "a, b")]
		[InlineData("<src.splitRealm(\"<src.sex(\"a, b\",\"c\")>\",\"<src.sex(\"d\",\"e\")>\")>", false, false, "e")]
		[InlineData("Me jmeno je <src.name>, tvoje <name>", true, true, "Me jmeno je Hero, tvoje Npc")]
		public void MarkupPicksTheRightVariant(string text, bool male, bool gondor, string expected) {
			Assert.Equal(expected, QuestDialogContent.ExpandMarkup(text, male, gondor, "Hero", "Npc"));
		}
	}
}
