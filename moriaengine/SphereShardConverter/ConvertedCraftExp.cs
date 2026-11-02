using System;
using System.Globalization;
using System.IO;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Converter {

	/// <summary>#962 - what craft masters count: nastaveni_craftexp_* and the skill each material asks (def_expcraft in
	/// mistri.scp, nastaveni_miningskill). Constants, as nothing here has a def of its own.</summary>
	internal static class ConvertedCraftExp {
		private const string ExpVar = "nastaveni_craftexp_", SkillVar = "nastaveni_miningskill_";
		private static readonly string[] woods = { "copper", "iron", "verite", "valorite", "obsidian", "adamantinum", "mithril" };

		public static void Write(string outputPath) {
			StringBuilder output = new StringBuilder("[Constants craftexp]\n");
			foreach (string name in SphereWorldVars.RecipeItems(ExpVar)) {
				if (SphereWorldVars.TryGetInt(ExpVar + name, out int value)) {
					output.Append("craftexp_").Append(name.ToLowerInvariant()).Append(' ').Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
				}
			}
			foreach (string wood in woods) {
				Append(output, "i_log_" + wood, "l_" + wood);
				Append(output, "i_ore_" + wood, "o_" + wood);
				Append(output, "i_ingot_" + wood, "o_" + wood);
			}
			string target = Tools.CombineMultiplePaths(outputPath, "scripts", "systems", "craftexp.def");
			Tools.EnsureDirectory(Path.GetDirectoryName(target));
			File.WriteAllText(target, output.ToString());
		}

		private static void Append(StringBuilder output, string item, string resource) {
			string skill = SphereWorldVars.GetString(SkillVar + resource);
			if (skill != null) {
				output.Append("materialskill_").Append(item).Append(' ').Append(skill.Trim(' ', '"').Split(',')[0]).Append('\n');
			}
		}
	}
}
