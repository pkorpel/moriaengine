using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SteamEngine.Converter {

	/// <summary>#860 - Erebor's [VarNames] from save/sphereworld.scp. The shard set most combat
	/// numbers from these nastaveni_* variables at runtime (nastaveni_ceho.scp), overriding what
	/// its item definitions say, so the definitions alone give the wrong values.</summary>
	internal static class SphereWorldVars {
		private static readonly Dictionary<string, string> recipeShortNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static readonly Regex recipeShortNameRE = new Regex(@"^\s*zkratka_(?<short>\S+)\s+""?(?<name>[^\s""/]+)", RegexOptions.CultureInvariant);
		private static readonly Dictionary<string, string> vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public static void Load(string path) {
			vars.Clear();
			if (!File.Exists(path)) {
				ConverterLogger.WriteWarning("No " + path + ", Erebor's nastaveni_* values won't be applied.");
				return;
			}
			bool inVarNames = false;
			foreach (string rawLine in File.ReadLines(path)) {
				string line = rawLine.Trim();
				if (line.StartsWith("[")) {
					inVarNames = line.Equals("[VarNames]", StringComparison.OrdinalIgnoreCase);
				} else if (inVarNames) {
					int eq = line.IndexOf('=');
					if (eq > 0) {
						vars[line.Substring(0, eq)] = line.Substring(eq + 1).Trim().Trim('"');
					}
				}
			}
		}

		private static readonly Dictionary<int, int> layerArmorPercents = new Dictionary<int, int>();

		/// <summary>The layerar[N] table of spheredefs.scp: each layer's share of a whole armor.</summary>
		public static void LoadLayerArmorPercents(string path) {
			layerArmorPercents.Clear();
			if (!File.Exists(path)) {
				ConverterLogger.WriteWarning("No " + path + ", Erebor's armor weights won't be applied.");
				return;
			}
			foreach (string line in File.ReadLines(path)) {
				Match match = Regex.Match(line, @"^layerar\[(\d+)\]\s+(\d+)");
				if (match.Success) {
					layerArmorPercents[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)] = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
				}
			}
		}

		public static bool TryGetLayerArmorPercent(int layer, out int percent) {
			return layerArmorPercents.TryGetValue(layer, out percent);
		}

		/// <summary>#962 - zkratka_iCO i_ingot_copper: nastaveni_cm.scp's short names, which the shard's live recipes are written in.</summary>
		public static void LoadRecipeShortNames(string path) {
			recipeShortNames.Clear();
			if (!File.Exists(path)) {
				ConverterLogger.WriteWarning("No " + path + ", Erebor's live recipes can't be read.");
				return;
			}
			foreach (string line in File.ReadLines(path)) {
				Match m = recipeShortNameRE.Match(line);
				if (m.Success) {
					recipeShortNames[m.Groups["short"].Value] = m.Groups["name"].Value;
				}
			}
		}

		/// <summary>#962 - nastaveni_cm_resources_ or nastaveni_cm_skillmake_ of an item, in full and as a .def line wants it:
		/// zkrpar_expand. Skills go by our keys, SKILL_Blacksmith being blacksmithing.</summary>
		public static string GetRecipe(string name) {
			if (!vars.TryGetValue(name, out string value)) {
				return null;
			}
			List<string> parts = new List<string>();
			foreach (string part in value.Split(',')) {
				string[] words = part.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
				if (words.Length == 2) {
					string resource = recipeShortNames.TryGetValue(words[1], out string full) ? full : words[1];
					parts.Add(words[0].Replace(".", "") + " " + SkillKey(resource.ToLowerInvariant()));
				}
			}
			return (parts.Count == 0) ? null : string.Join(", ", parts);
		}

		/// <summary>#962 - the items with a value under one of these prefixes, each once, sorted so a regen diffs to nothing.</summary>
		public static List<string> RecipeItems(params string[] prefixes) {
			SortedSet<string> items = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string name in vars.Keys) {
				foreach (string prefix in prefixes) {
					if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
						items.Add(name.Substring(prefix.Length));
					}
				}
			}
			return new List<string>(items);
		}

		private static string SkillKey(string resource) {
			if (!resource.StartsWith("skill_", StringComparison.Ordinal)) {
				return resource;
			}
			string skill = resource.Substring("skill_".Length);
			return (skill == "blacksmith") ? "blacksmithing" : skill;
		}

		public static string GetString(string name) {
			return vars.TryGetValue(name, out string value) ? value : null;
		}

		public static bool TryGetInt(string name, out int value) {
			value = 0;
			return vars.TryGetValue(name, out string text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}
	}
}
