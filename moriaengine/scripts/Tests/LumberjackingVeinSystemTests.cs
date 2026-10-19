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

	/// <summary>Server-side helpers for LumberjackingVeinSystem tests (#422). Uses the
	/// test-only ForceVeinForTest/ForceVeinRegenReadyForTest hooks to get deterministic
	/// coverage of depletion/downgrade/regen behavior despite the real system's randomized
	/// wood roll - same precedent as MiningVeinSystemTests.cs.</summary>
	public static class LumberjackingVeinSystemTests {

		private static WoodTier GetWoodByName(string name) {
			switch (name.ToLowerInvariant()) {
				case "copper": return WoodTiers.Copper;
				case "iron": return WoodTiers.Iron;
				case "verite": return WoodTiers.Verite;
				case "valorite": return WoodTiers.Valorite;
				case "obsidian": return WoodTiers.Obsidian;
				case "adamantinum": return WoodTiers.Adamantinum;
				case "mithril": return WoodTiers.Mithril;
				default: throw new ArgumentException("Unknown wood tier: " + name);
			}
		}

		[SteamFunction]
		public static void TestChopAtTile(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			ItemDef result = LumberjackingVeinSystem.Chop(self, mapPlane, x, y);
			Console.WriteLine("CHOP_RESULT: wood=" + DescribeWoodResult(result));
		}

		/// <summary>Same ItemDef.Defname-vs-real-identity gotcha as MiningVeinSystemTests'
		/// DescribeOreResult - compare by object identity against the known WoodTiers table.</summary>
		private static string DescribeWoodResult(ItemDef result) {
			if (result == null) {
				return "null";
			}
			foreach (WoodTier tier in WoodTiers.All) {
				if (ReferenceEquals(result, tier.ItemDef)) {
					return tier.ItemDefname;
				}
			}
			return "unknown:" + result.Defname;
		}

		[SteamFunction]
		public static void TestForceWoodVein(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			string woodName = Convert.ToString(sa.Argv[2], CultureInfo.InvariantCulture);
			int pool = Convert.ToInt32(sa.Argv[3], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			LumberjackingVeinSystem.ForceVeinForTest(mapPlane, x, y, GetWoodByName(woodName), pool);
			Console.WriteLine("WOOD_VEIN_FORCED: x=" + x + " y=" + y + " wood=" + woodName + " pool=" + pool);
		}

		[SteamFunction]
		public static void TestForceWoodVeinRegenReady(Character self, ScriptArgs sa) {
			int x = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			LumberjackingVeinSystem.ForceVeinRegenReadyForTest(mapPlane, x, y);
			Console.WriteLine("WOOD_VEIN_REGEN_FORCED: x=" + x + " y=" + y);
		}

		/// <summary>Samples `attempts` fresh, never-before-touched tiles (baseX+i each time) to
		/// confirm the weighted roll favors common wood over rare wood - same statistical-
		/// sampling precedent as MiningVeinSystemTests' TestSampleVeinOreDistribution.</summary>
		[SteamFunction]
		public static void TestSampleWoodVeinDistribution(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int baseX = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			byte mapPlane = self.GetMap().M;
			self.SetRealSkillValue((int) SkillName.Lumberjacking, 1200);

			int copperCount = 0;
			int mithrilCount = 0;
			for (int i = 0; i < attempts; i++) {
				ItemDef result = LumberjackingVeinSystem.Chop(self, mapPlane, baseX + i, 0);
				if (ReferenceEquals(result, WoodTiers.Copper.ItemDef)) {
					copperCount++;
				}
				if (ReferenceEquals(result, WoodTiers.Mithril.ItemDef)) {
					mithrilCount++;
				}
			}
			Console.WriteLine("WOOD_DISTRIBUTION_SAMPLE: attempts=" + attempts + " copper=" + copperCount + " mithril=" + mithrilCount);
		}

		/// <summary>#431 - samples `attempts` ambush rolls at self's own location, reporting the
		/// overall trigger count plus a per-monster breakdown (by NPCDef identity). Same
		/// statistical-sampling precedent as Mining's own TestSampleAmbushChance - always rolls
		/// with no wood tier (as On_Fail would), so a hit on the rarest bucket always resolves to
		/// the baseline ReaperSpruceDefname, giving a single well-known defname to count against
		/// wolf.</summary>
		[SteamFunction]
		public static void TestSampleLumberjackAmbushChance(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);

			for (int i = 0; i < attempts; i++) {
				LumberjackingAmbushSystem.TryAmbush(self.P(), null);
			}

			NPCDef wolfDef = (NPCDef) ThingDef.GetByDefname(LumberjackingAmbushSystem.WolfDefname);
			NPCDef reaperSpruceDef = (NPCDef) ThingDef.GetByDefname(LumberjackingAmbushSystem.ReaperSpruceDefname);
			int total = 0;
			int wolfCount = 0;
			int reaperSpruceCount = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 10)) {
				NPC npc = ch as NPC;
				if (npc == null) {
					continue;
				}
				total++;
				if (npc.Def == wolfDef) {
					wolfCount++;
				}
				if (npc.Def == reaperSpruceDef) {
					reaperSpruceCount++;
				}
			}
			Console.WriteLine("LUMBERJACK_AMBUSH_CHANCE_SAMPLE: attempts=" + attempts + " total=" + total +
				" wolf=" + wolfCount + " reaperSpruce=" + reaperSpruceCount);
		}
	}
}
