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

	/// <summary>Server-side helpers for Fishing's catch + ambush tests (#271). Mirrors the
	/// LumberjackingVeinSystemTests / LumberjackingSettingsTests server-side helper shape - drives
	/// the real FishingCatchSystem.Catch / FishingAmbushSystem.TryAmbush / FishingSettings
	/// write-through paths and reports a greppable line back over the game console for the
	/// Tests/Protocol xunit driver to assert on. Statistical sampling (not deterministic single
	/// rolls) for the probabilistic paths, same precedent as the sibling systems.</summary>
	public static class FishingCatchSystemTests {

		/// <summary>Samples `attempts` catches, reporting how many were fish vs junk crates vs
		/// junk-crates-with-map. Temporarily forces generous junk/map chances so a reasonable
		/// sample size exercises all three branches, then restores the real defaults so the test
		/// run leaves no global side effect (the chances are process-wide mutable statics).</summary>
		[SteamFunction]
		public static void TestSampleFishingCatch(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);

			int savedJunkNum = FishingCatchSystem.JunkChanceNumerator;
			int savedJunkDen = FishingCatchSystem.JunkChanceDenominator;
			int savedMapNum = FishingCatchSystem.TreasureMapChanceNumerator;
			int savedMapDen = FishingCatchSystem.TreasureMapChanceDenominator;
			try {
				FishingCatchSystem.JunkChanceNumerator = 1;
				FishingCatchSystem.JunkChanceDenominator = 2; //~half junk, so both fish and junk are well-sampled
				FishingCatchSystem.TreasureMapChanceNumerator = 1;
				FishingCatchSystem.TreasureMapChanceDenominator = 2; //~half of junk also carries a map

				int fish = 0;
				int junk = 0;
				int junkWithMap = 0;
				for (int i = 0; i < attempts; i++) {
					FishingCatchSystem.CatchResult r = FishingCatchSystem.Catch(self);
					switch (r.Kind) {
						case FishingCatchSystem.CatchKind.Fish:
							fish++;
							break;
						case FishingCatchSystem.CatchKind.JunkCrate:
							junk++;
							break;
						case FishingCatchSystem.CatchKind.JunkCrateWithMap:
							junkWithMap++;
							break;
					}
				}
				Console.WriteLine("FISHING_CATCH_SAMPLE: attempts=" + attempts + " fish=" + fish +
					" junk=" + junk + " junkWithMap=" + junkWithMap);
			} finally {
				FishingCatchSystem.JunkChanceNumerator = savedJunkNum;
				FishingCatchSystem.JunkChanceDenominator = savedJunkDen;
				FishingCatchSystem.TreasureMapChanceNumerator = savedMapNum;
				FishingCatchSystem.TreasureMapChanceDenominator = savedMapDen;
			}
		}

		/// <summary>With junk chance forced to zero, every catch must be a real catchable fish
		/// item - confirms the fish-variety roll only ever yields known fish defnames, never null
		/// or a stray non-fish item.</summary>
		[SteamFunction]
		public static void TestSampleFishingFishVariety(Character self, ScriptArgs sa) {
			int attempts = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);

			int savedJunkNum = FishingCatchSystem.JunkChanceNumerator;
			try {
				FishingCatchSystem.JunkChanceNumerator = 0; //never junk - isolate the fish roll
				int fishWithItem = 0;
				int distinctSeen = 0;
				bool[] seen = new bool[FishingCatchSystem.FishDefnames.Length];
				for (int i = 0; i < attempts; i++) {
					FishingCatchSystem.CatchResult r = FishingCatchSystem.Catch(self);
					if (r.Kind == FishingCatchSystem.CatchKind.Fish && r.ItemDef != null) {
						fishWithItem++;
						for (int f = 0; f < FishingCatchSystem.FishDefnames.Length; f++) {
							ItemDef fd = ThingDef.GetByDefname(FishingCatchSystem.FishDefnames[f]) as ItemDef;
							if (ReferenceEquals(r.ItemDef, fd) && !seen[f]) {
								seen[f] = true;
								distinctSeen++;
							}
						}
					}
				}
				Console.WriteLine("FISHING_FISH_VARIETY_SAMPLE: attempts=" + attempts +
					" fishWithItem=" + fishWithItem + " distinctKinds=" + distinctSeen);
			} finally {
				FishingCatchSystem.JunkChanceNumerator = savedJunkNum;
			}
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
		public static void TestSetJunkChanceViaFishingSettings(Character self, ScriptArgs sa) {
			int num = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int den = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			FishingSettings.instance.junkChanceNumerator = num;
			FishingSettings.instance.junkChanceDenominator = den;
			Console.WriteLine("FISHING_SETTINGS_JUNK_SET: num=" + FishingCatchSystem.JunkChanceNumerator +
				" den=" + FishingCatchSystem.JunkChanceDenominator);
		}

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
			FishingSettings.instance.gatherDelayBaseSeconds = seconds;
			Console.WriteLine("FISHING_SETTINGS_DELAY_SET: baseSeconds=" + GatheringDelaySystem.BaseDelaySeconds);
		}
	}
}
