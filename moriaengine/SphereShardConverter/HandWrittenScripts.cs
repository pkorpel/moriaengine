using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.Converter {
	/// <summary>#937 - what the engine's own scripts already define. Those win: a Sphere value can mean something
	/// else here (attr_ and statf_ flags are different bits), and defining a name twice is an error at boot.</summary>
	internal static class HandWrittenScripts {
		private static readonly Regex headerRE = new Regex(@"^\s*\[\s*(?<type>\w+)(\s+(?<name>[^\]\s]+))?", RegexOptions.CultureInvariant);
		private static readonly Regex defnameRE = new Regex(@"^\s*defname\s*=?\s*(?<name>[^\s/]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		private static readonly HashSet<string> constants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public static readonly HashSet<string> Items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		public static readonly HashSet<string> Chars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public static bool IsConstant(string name) {
			return constants.Contains(name);
		}

		/// <summary>Anything of that name: an item, a character, a spell...</summary>
		public static bool IsDefined(string name) {
			return names.Contains(name) || Items.Contains(name) || Chars.Contains(name);
		}

		public static void Load(string scriptsPath, string convertedPath) {
			string skip = Path.GetFullPath(convertedPath);
			foreach (string file in Directory.EnumerateFiles(scriptsPath, "*", SearchOption.AllDirectories)) {
				bool isScript = file.EndsWith(".def", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".scp", StringComparison.OrdinalIgnoreCase);
				if (isScript && !Path.GetFullPath(file).StartsWith(skip, StringComparison.Ordinal)) {
					LoadFile(file);
				}
			}
		}

		private static void LoadFile(string file) {
			bool inConstants = false;
			HashSet<string> defs = null;
			foreach (string line in File.ReadLines(file, Encoding.Latin1)) {
				string trimmed = line.Trim();
				if (trimmed.StartsWith("//", StringComparison.Ordinal)) {
					continue;
				}
				Match header = headerRE.Match(line);
				if (header.Success) {
					string type = header.Groups["type"].Value.ToLowerInvariant();
					inConstants = (type == "constants") || (type == "defname") || (type == "defnames");
					defs = SetOfDefType(type);
					if (defs != null) {
						defs.Add(NormalizeNumber(header.Groups["name"].Value));
					}
				} else if (inConstants) {
					if (trimmed.Length > 0) {
						constants.Add(trimmed.Split(new[] { ' ', '\t', '=' }, 2)[0]);
					}
				} else if (defs != null) {
					Match defname = defnameRE.Match(line);
					if (defname.Success) {
						defs.Add(defname.Groups["name"].Value);
					}
				}
			}
		}

		private static HashSet<string> SetOfDefType(string type) {
			if (!type.EndsWith("def", StringComparison.Ordinal)) {
				return null;
			}
			if (type.Contains("scroll")) {
				return Items;
			}
			//the numbers of skills and spells aren't models
			if (type.Contains("skill") || type.Contains("spell") || type.Contains("ability")) {
				return names;
			}
			if (type.Contains("npc") || type.Contains("player") || type.Contains("char")) {
				return Chars;
			}
			return Items;
		}

		private static string NormalizeNumber(string name) {
			int number;
			if (ConvertTools.TryParseInt32(name, out number)) {
				return "0x" + number.ToString("x");
			}
			return name;
		}
	}
}
