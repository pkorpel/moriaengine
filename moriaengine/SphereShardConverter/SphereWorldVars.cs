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

		public static bool TryGetInt(string name, out int value) {
			value = 0;
			return vars.TryGetValue(name, out string text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}
	}
}
