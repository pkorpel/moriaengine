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
			//#813 emptySpot: the spot's region roll was "nothing" (not just a used-up pool)
			Console.WriteLine("MINE_RESULT: ore=" + DescribeOreResult(result) + " emptySpot=" + MiningVeinSystem.IsEmptySpot(mapPlane, x, y));
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

		/// <summary>Samples `attempts` fresh tiles, each given a copper vein (#813 - a fresh tile's
		/// own region roll is mostly "nothing", which never reaches the cave-in roll) and mined
		/// once with a high enough skill to always succeed, counting how many trigger the real
		/// 1/120 cave-in roll.</summary>
		[SteamFunction]
		public static void TestSampleCaveInChance(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int baseX = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			self.SetRealSkillValue((int) SkillName.Mining, 1200);

			int caveIns = 0;
			for (int i = 0; i < attempts; i++) {
				MiningVeinSystem.ForceVeinForTest(mapPlane, baseX + i, 1, OreTiers.Copper, 5);
				MiningVeinSystem.Mine(self, mapPlane, baseX + i, 1);
				if (MiningVeinSystem.IsCaveInBlocked(mapPlane, baseX + i, 1)) {
					caveIns++;
				}
			}
			Console.WriteLine("CAVE_IN_CHANCE_SAMPLE: attempts=" + attempts + " caveIns=" + caveIns);
		}

		/// <summary>#335 - samples `attempts` ambush rolls at self's own location, reporting
		/// the overall trigger count plus a per-monster breakdown (by NPCDef identity, so the
		/// two deliberate content-substitution overlaps - rock elemental sharing earth
		/// elemental's defname, random-golem sharing copper spirit's - collapse into the same
		/// counted bucket, which is fine for confirming relative weighting direction). Same
		/// statistical-sampling precedent as TestSampleVeinOreDistribution/
		/// TestSampleCaveInChance above.</summary>
		[SteamFunction]
		public static void TestSampleAmbushChance(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);

			for (int i = 0; i < attempts; i++) {
				MonsterAmbushSystem.TryAmbush(self.P());
			}

			NPCDef ratDef = (NPCDef) ThingDef.GetByDefname(MonsterAmbushSystem.RatDefname);
			NPCDef golemDef = (NPCDef) ThingDef.GetByDefname(MonsterAmbushSystem.GolemDefname);
			int total = 0;
			int ratCount = 0;
			int golemCount = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 10)) {
				NPC npc = ch as NPC;
				if (npc == null) {
					continue;
				}
				total++;
				if (npc.Def == ratDef) {
					ratCount++;
				}
				if (npc.Def == golemDef) {
					golemCount++;
				}
			}
			Console.WriteLine("AMBUSH_CHANCE_SAMPLE: attempts=" + attempts + " total=" + total +
				" rat=" + ratCount + " golem=" + golemCount);
		}

		[SteamFunction]
		public static void TestForceVeinRegenReady(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			MiningVeinSystem.ForceVeinRegenReadyForTest(mapPlane, x, y);
			Console.WriteLine("VEIN_REGEN_FORCED: x=" + x + " y=" + y);
		}
	}
}
