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

	/// <summary>Server-side helpers for Fishing's catch + ambush tests (#271; catch reworked onto
	/// region resources in #813). Mirrors the
	/// LumberjackingVeinSystemTests / LumberjackingSettingsTests server-side helper shape - drives
	/// the real FishingCatchSystem.Catch / FishingAmbushSystem.TryAmbush / FishingSettings
	/// write-through paths and reports a greppable line back over the game console for the
	/// Tests/Protocol xunit driver to assert on. Statistical sampling (not deterministic single
	/// rolls) for the probabilistic paths, same precedent as the sibling systems.</summary>
	public static class FishingCatchSystemTests {

		/// <summary>#813 - samples the archive's r_default_water region type (what most of the
		/// world's water rolls from): how many picks are nothing / fish / junk fish.</summary>
		[SteamFunction]
		public static void TestSampleWaterRegionRoll(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			RegionTypeDef water = RegionTypeDef.GetByDefname("r_default_water");
			int nothing = 0, fish = 0, junk = 0;
			for (int i = 0; i < attempts; i++) {
				RegionResourceDef r = water.PickRandom();
				if ((r == null) || r.IsNothing) {
					nothing++;
				} else if (FishingCatchSystem.IsJunk(r)) {
					junk++;
				} else {
					fish++;
				}
			}
			Console.WriteLine("WATER_REGION_SAMPLE: attempts=" + attempts + " nothing=" + nothing + " fish=" + fish + " junk=" + junk);
		}

		/// <summary>#813 - one water spot, forced to hold `resource` with a pool of 2 (bypassing the
		/// region roll): three catches by self at Fishing `skill` (x10). Reports each catch's kind
		/// and item.</summary>
		[SteamFunction]
		public static void TestFishingSpotCatches(Character self, ScriptArgs sa) {
			//the raw text: a plain account's Argv is the whole line as one literal string
			string[] args = sa.Args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string resource = args[0].Trim('"');
			int skill = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
			self.SetRealSkillValue((int) SkillName.Fishing, skill);
			byte m = self.M;
			int x = self.X + 50, y = self.Y + 50; //some spot nobody else uses
			FishingCatchSystem.ForceSpotForTest(m, x, y, RegionResourceDef.GetByDefname(resource), 2);
			string report = "";
			for (int i = 0; i < 3; i++) {
				FishingCatchSystem.CatchResult r = FishingCatchSystem.Catch(self, m, x, y);
				report += " " + r.Kind + ":" + ((r.ItemDef == null) ? "none" : r.ItemDef.PrettyDefname);
			}
			Console.WriteLine("FISHING_SPOT_CATCHES: " + resource + " skill=" + skill + report);
		}

		/// <summary>#271 - samples `attempts` ambush rolls at self's own location, reporting the
		/// overall trigger count plus a per-monster breakdown. Same statistical-sampling precedent
		/// as Lumberjacking's TestSampleLumberjackAmbushChance.</summary>
		[SteamFunction]
		public static void TestSampleFishingAmbushChance(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);

			for (int i = 0; i < attempts; i++) {
				FishingAmbushSystem.TryAmbush(self.P());
			}

			NPCDef frogDef = (NPCDef) ThingDef.GetByDefname(FishingAmbushSystem.FrogDefname);
			NPCDef krakenDef = (NPCDef) ThingDef.GetByDefname(FishingAmbushSystem.KrakenDefname);
			int total = 0;
			int frogCount = 0;
			int krakenCount = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 10)) {
				NPC npc = ch as NPC;
				if (npc == null) {
					continue;
				}
				total++;
				if (npc.Def == frogDef) {
					frogCount++;
				}
				if (npc.Def == krakenDef) {
					krakenCount++;
				}
			}
			Console.WriteLine("FISHING_AMBUSH_CHANCE_SAMPLE: attempts=" + attempts + " total=" + total +
				" frog=" + frogCount + " kraken=" + krakenCount);
		}

		// ---- FishingSettings write-through (#271) ----

		[SteamFunction]
		public static void TestSetAmbushChanceViaFishingSettings(Character self, ScriptArgs sa) {
			int denominator = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			FishingSettings.instance.ambushChanceDenominator = denominator;
			Console.WriteLine("FISHING_SETTINGS_AMBUSH_SET: denominator=" + FishingAmbushSystem.AmbushChanceDenominator);
		}

		[SteamFunction]
		public static void TestSetAmbushKrakenDefnameViaFishingSettings(Character self, ScriptArgs sa) {
			string defname = Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture);
			FishingSettings.instance.ambushKrakenDefname = defname;
			Console.WriteLine("FISHING_SETTINGS_AMBUSH_KRAKEN_SET: krakenDefname=" + FishingAmbushSystem.KrakenDefname);
		}

		[SteamFunction]
		public static void TestSetGatherDelayBaseViaFishingSettings(Character self, ScriptArgs sa) {
			double seconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			FishingSettings.instance.sharedGatherDelayBaseSeconds = seconds;
			Console.WriteLine("FISHING_SETTINGS_DELAY_SET: baseSeconds=" + GatheringDelaySystem.BaseDelaySeconds);
		}
	}
}
