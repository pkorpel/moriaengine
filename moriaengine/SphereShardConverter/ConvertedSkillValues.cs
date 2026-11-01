using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Converter {

	/// <summary>#962 - the shard's live DELAY, EFFECT and ADVRATE of each skill, which override spheretables.scp as nastaveni_cm_*
	/// do a recipe. The skills here are written by hand, so these go to [SkillValuesDef]s, read at boot. 0fffffff, no gain by use, is 0.</summary>
	internal static class ConvertedSkillValues {
		private const string DelayVar = "nastaveni_skilldelay_", EffectVar = "nastaveni_skilleffect_", AdvRateVar = "nastaveni_skilladvrate_";
		private const string NoGain = "268435455";
		private const string DiscordanceHere = "Skill_Discordance";

		public static void Write(string outputPath) {
			StringBuilder output = new StringBuilder();
			foreach (string skill in SphereWorldVars.RecipeItems(DelayVar, EffectVar, AdvRateVar)) {
				string ours = StringComparer.OrdinalIgnoreCase.Equals(skill, "skill_enticement") ? DiscordanceHere : skill; //its slot, Erebor's Discordance
				if (HandWrittenScripts.IsDefined(ours)) {
					output.Append("[SkillValuesDef skillvalues_").Append(skill).Append("]\n");
					output.Append("Skill = ").Append(ours).Append('\n');
					string delay = SphereWorldVars.GetString(DelayVar + skill);
					if (delay != null) {
						output.Append("Delay = ").Append(Scaled(delay, 10, true)).Append('\n');
					}
					string effect = SphereWorldVars.GetString(EffectVar + skill);
					if (effect != null) {
						output.Append("Effect = ").Append(Scaled(effect, 1, true)).Append('\n');
					}
					string advRate = SphereWorldVars.GetString(AdvRateVar + skill);
					if (advRate != null) {
						output.Append("AdvRate = ").Append((advRate == NoGain) ? "0" : Scaled(advRate, 10, false)).Append('\n');
					}
					output.Append('\n');
				}
			}
			string target = Tools.CombineMultiplePaths(outputPath, "scripts", "systems", "skillvalues.def");
			Tools.EnsureDirectory(Path.GetDirectoryName(target));
			File.WriteAllText(target, output.ToString());
		}

		//a lone value stands for the whole range. Sphere keeps tenths, the .def files here the number
		private static string Scaled(string values, int divisor, bool range) {
			List<string> parts = values.Split(',').Select(part => (double.Parse(part.Trim(), CultureInfo.InvariantCulture) / divisor).ToString("0.0##", CultureInfo.InvariantCulture)).ToList();
			if (range && (parts.Count == 1)) {
				parts.Add(parts[0]);
			}
			return string.Join(", ", parts);
		}
	}
}
