using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1405 - NPCDef's add-up lists as typed def fields. TestDumpNpcDefLists writes what every NPCDef's lists
	/// read as (spells, summons, loot, starting equipment, butcher resources, food types, skill and resist ranges,
	/// movement), one block per def by defname, so a dump made before the change can be compared with one after.</summary>
	public static class NpcDefListsTests {
		[SteamFunction]
		public static void TestDumpNpcDefLists(Character self, ScriptArgs sa) {
			string path = sa.Args.Trim().Trim('"');
			HashSet<NPCDef> seen = new HashSet<NPCDef>(); //AllScripts holds a def under each of its names
			List<NPCDef> defs = new List<NPCDef>();
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if ((script is NPCDef def) && seen.Add(def)) {
					defs.Add(def);
				}
			}
			defs.Sort((a, b) => string.CompareOrdinal(a.PrettyDefname, b.PrettyDefname));
			StringBuilder sb = new StringBuilder();
			foreach (NPCDef def in defs) {
				sb.Append('[').Append(def.PrettyDefname).Append("]\n");
				Line(sb, "spells", def.Spells?.Select(s => s.PrettyDefname));
				Line(sb, "summons", def.SummonedCreatures?.Select(s => s.PrettyDefname));
				Line(sb, "battleSummons", def.BattleSummons?.Select(s => s.PrettyDefname));
				Line(sb, "loot", def.Loot);
				Line(sb, "equip", def.StartingEquipment?.Select(e => e.Defname + "|" + Range(e.AmountRange) + "|" + e.Color + "|" + e.Name + "|" +
					((e.RandomDefnames == null) ? "" : string.Join(" ", e.RandomDefnames) + "/" + string.Join(" ", e.RandomWeights))));
				Line(sb, "resources", def.ButcherResources?.Select(r => r.Amount + " " + r.Defname));
				Line(sb, "foodTypes", def.FoodTypes?.Select(f => f.SatiationValue + " " + f.CategoryOrDefname));
				Line(sb, "skills", def.SkillValueRanges?.OrderBy(p => p.Key.ToString(), StringComparer.Ordinal).Select(p => p.Key + " " + Range(p.Value)));
				Line(sb, "resists", def.ResistRanges?.OrderBy(p => p.Key.ToLowerInvariant(), StringComparer.Ordinal).Select(p => p.Key.ToLowerInvariant() + " " + Range(p.Value)));
				if (def.DefaultMovementSettings is DynamicMovementSettings m) {
					sb.Append("  can=").Append(m.canCrossLand).Append(' ').Append(m.canSwim).Append(' ').Append(m.canCrossLava).Append(' ')
						.Append(m.canFly).Append(' ').Append(m.ignoreDoors).Append(' ').Append(m.climbPower).Append('\n');
				}
			}
			File.WriteAllText(path, sb.ToString());
			Console.WriteLine("NPCDEFLISTS_DUMPED: defs=" + defs.Count + " path=" + path);
		}

		private static void Line(StringBuilder sb, string name, IEnumerable<string> items) {
			if (items == null) {
				return;
			}
			sb.Append("  ").Append(name).Append('=').Append(string.Join(" ; ", items)).Append('\n');
		}

		private static string Range(int[] range) {
			return (range == null) ? "" : string.Join(" ", range.Select(i => i.ToString(CultureInfo.InvariantCulture)));
		}
	}
}
