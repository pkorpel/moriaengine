using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Converter {

	/// <summary>#962 - the shard's live cast time, mana, duration and skill requirement of each spell, which override
	/// spheretables.scp as nastaveni_cm_* do a recipe. The spells here are written by hand, so these go to
	/// [SpellValuesDef]s, read at boot. Effects stay in the hand-written sections: what the number means differs by spell.</summary>
	internal static class ConvertedSpellValues {
		private const string KeptDuration = "s_gate_travel"; //3-4 s by the numbers, #489 made it a minute
		private const string TimeVar = "nastaveni_spelltime_", ManaVar = "nastaveni_spellmana_", DurationVar = "nastaveni_duration_", ReqVar = "nastaveni_SKILLREQ_";

		public static void Write(string outputPath) {
			StringBuilder output = new StringBuilder();
			foreach (string spell in SphereWorldVars.RecipeItems(TimeVar, ManaVar, DurationVar, ReqVar)) {
				if (HandWrittenScripts.IsDefined(spell)) {
					output.Append("[SpellValuesDef spellvalues_").Append(spell).Append("]\n");
					output.Append("Spell = ").Append(spell).Append('\n');
					string time = SphereWorldVars.GetString(TimeVar + spell);
					if (time != null) {
						output.Append("CastTime = ").Append(Tenths(time)).Append('\n');
					}
					string mana = SphereWorldVars.GetString(ManaVar + spell);
					if (mana != null) {
						output.Append("Mana = ").Append(mana.Trim()).Append('\n');
					}
					string duration = SphereWorldVars.GetString(DurationVar + spell);
					if ((duration != null) && !IsZero(duration) && !StringComparer.OrdinalIgnoreCase.Equals(spell, KeptDuration)) {
						output.Append("Duration = ").Append(Tenths(duration)).Append('\n');
					}
					string requirement = SphereWorldVars.GetString(ReqVar + spell);
					if (requirement != null) {
						output.Append("Skill = ").Append(requirement.Trim().Split(' ')[0]).Append('\n');
					}
					output.Append('\n');
				}
			}
			string target = Tools.CombineMultiplePaths(outputPath, "scripts", "systems", "spellvalues.def");
			Tools.EnsureDirectory(Path.GetDirectoryName(target));
			File.WriteAllText(target, output.ToString());
		}

		private static bool IsZero(string values) {
			return values.Split(',').All(part => double.Parse(part.Trim(' ', '"'), CultureInfo.InvariantCulture) == 0);
		}

		//Sphere keeps tenths, the .def files here seconds. A lone value stands for the whole range
		private static string Tenths(string values) {
			List<string> parts = values.Split(',').Select(part => (double.Parse(part.Trim(' ', '"'), CultureInfo.InvariantCulture) / 10).ToString("0.###", CultureInfo.InvariantCulture)).ToList();
			return string.Join(", ", parts);
		}
	}
}
