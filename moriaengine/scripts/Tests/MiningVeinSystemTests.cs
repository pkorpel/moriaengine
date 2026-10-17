/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>Server-side helpers for MiningVeinSystem tests (#333). Uses the test-only
	/// ForceVeinForTest/ForceVeinRegenReadyForTest hooks to get deterministic coverage of
	/// depletion/downgrade/regen behavior despite the real system's randomized ore roll -
	/// same precedent as every other probabilistic-system test this session.</summary>
	public static class MiningVeinSystemTests {

		private static OreTier GetOreByName(string name) {
			switch (name.ToLowerInvariant()) {
				case "copper": return OreTiers.Copper;
				case "iron": return OreTiers.Iron;
				case "silver": return OreTiers.Silver;
				case "gold": return OreTiers.Gold;
				case "verite": return OreTiers.Verite;
				case "valorite": return OreTiers.Valorite;
				case "obsidian": return OreTiers.Obsidian;
				case "shadow": return OreTiers.Shadow;
				case "adamantinum": return OreTiers.Adamantinum;
				case "mithril": return OreTiers.Mithril;
				default: throw new ArgumentException("Unknown ore tier: " + name);
			}
		}

		[SteamFunction]
		public static void TestMineAtTile(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			ItemDef result = MiningVeinSystem.Mine(self, mapPlane, x, y);
			Console.WriteLine("MINE_RESULT: ore=" + DescribeOreResult(result));
		}

		/// <summary>ItemDef.Defname is the raw header defname (e.g. "0x19b7"), not the real
		/// `DEFNAME=i_ore_iron` alt-name registered inside the block - same already-documented
		/// Defname-vs-PrettyDefname gotcha from elsewhere in this codebase. Comparing by object
		/// identity against the known OreTiers table sidesteps it entirely.</summary>
		private static string DescribeOreResult(ItemDef result) {
			if (result == null) {
				return "null";
			}
			foreach (OreTier tier in OreTiers.All) {
				if (ReferenceEquals(result, tier.ItemDef)) {
					return tier.ItemDefname;
				}
			}
			return "unknown:" + result.Defname;
		}

		[SteamFunction]
		public static void TestForceVein(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			string oreName = Convert.ToString(sa.Argv[2], CultureInfo.InvariantCulture);
			int pool = Convert.ToInt32(sa.Argv[3], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			MiningVeinSystem.ForceVeinForTest(mapPlane, x, y, GetOreByName(oreName), pool);
			Console.WriteLine("VEIN_FORCED: x=" + x + " y=" + y + " ore=" + oreName + " pool=" + pool);
		}

		/// <summary>#334 - forces a cave-in onto a tile (bypassing the real 1/120 roll) for
		/// deterministic clear-roll testing.</summary>
		[SteamFunction]
		public static void TestForceCaveIn(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			MiningVeinSystem.ForceCaveInForTest(mapPlane, x, y);
			Console.WriteLine("CAVE_IN_FORCED: x=" + x + " y=" + y);
		}

		[SteamFunction]
		public static void TestCheckCaveInBlocked(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			bool blocked = MiningVeinSystem.IsCaveInBlocked(mapPlane, x, y);
			Console.WriteLine("CAVE_IN_BLOCKED_CHECK: blocked=" + blocked);
		}

		[SteamFunction]
		public static void TestTryClearCaveIn(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			bool cleared = MiningVeinSystem.TryClearCaveIn(self, mapPlane, x, y);
			Console.WriteLine("CAVE_IN_CLEAR_ATTEMPT: cleared=" + cleared);
		}

		/// <summary>Samples `attempts` fresh tiles, each mined once with a high enough skill to
		/// always succeed against copper's own 0 threshold, counting how many trigger the real
		/// 1/120 cave-in roll - same statistical-sampling precedent as
		/// TestSampleVeinOreDistribution above.</summary>
		[SteamFunction]
		public static void TestSampleCaveInChance(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int baseX = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			self.SetRealSkillValue((int) SkillName.Mining, 1200);

			int caveIns = 0;
			for (int i = 0; i < attempts; i++) {
				MiningVeinSystem.Mine(self, mapPlane, baseX + i, 1);
				if (MiningVeinSystem.IsCaveInBlocked(mapPlane, baseX + i, 1)) {
					caveIns++;
				}
			}
			Console.WriteLine("CAVE_IN_CHANCE_SAMPLE: attempts=" + attempts + " caveIns=" + caveIns);
		}

		[SteamFunction]
		public static void TestForceVeinRegenReady(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			MiningVeinSystem.ForceVeinRegenReadyForTest(mapPlane, x, y);
			Console.WriteLine("VEIN_REGEN_FORCED: x=" + x + " y=" + y);
		}

		/// <summary>Samples `attempts` fresh, never-before-touched tiles (baseX+i each time) to
		/// confirm the weighted roll actually favors common ores over rare ones - same
		/// statistical-sampling precedent as SpawnCommandTests' TestSampleRollSpawnDefDistribution.
		/// Skill set high enough to never trigger the downgrade, so this purely measures the
		/// untouched-vein roll itself.</summary>
		[SteamFunction]
		public static void TestSampleVeinOreDistribution(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int baseX = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			self.SetRealSkillValue((int) SkillName.Mining, 1200);

			int copperCount = 0;
			int mithrilCount = 0;
			for (int i = 0; i < attempts; i++) {
				ItemDef result = MiningVeinSystem.Mine(self, mapPlane, baseX + i, 0);
				if (ReferenceEquals(result, OreTiers.Copper.ItemDef)) {
					copperCount++;
				}
				if (ReferenceEquals(result, OreTiers.Mithril.ItemDef)) {
					mithrilCount++;
				}
			}
			Console.WriteLine("VEIN_DISTRIBUTION_SAMPLE: attempts=" + attempts + " copper=" + copperCount + " mithril=" + mithrilCount);
		}
	}
}
