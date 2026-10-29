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
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Fishing's catch system (#271, reworked onto the Erebor archive's region resources in #813).
	///
	/// What a water spot holds is rolled from its region's t_water region type
	/// (sphereregion.scp, RegionResourceSystem) - e.g. r_default_water: 100 nothing, 15 w_fish1,
	/// 8 w_fish2, 5 w_fish3, 1 w_fish4 and 1 each of the junk fish w_jf1-10. Like Sphere's resource
	/// spots (and the mining/lumberjacking veins) the roll holds for that spot until its pool
	/// (AMOUNT) is fished out, then the spot rests for REGEN and rolls again; a "nothing" spot rests
	/// straight away. Under-skilled for what the spot holds = you catch w_fish1 instead (Erebor's
	/// fixresource). Until #813 this was a uniform pick between fish graphics plus an invented 1/10
	/// junk roll, on the stale belief that the w_fish tiers' items didn't exist - they do.
	///
	/// Junk fish: the archive's i_junkfish1-10 are barrels whose contents script (t_junkfish /
	/// randfishjunk) isn't converted, so a junk-fish catch keeps this system's own payoff - a junk
	/// crate, with a tunable chance of a treasure map. The treasure map is a PLACEHOLDER (the
	/// generic i_MAP_ROLLED) until a real treasure-map system exists (#530).
	/// </summary>
	public static class FishingCatchSystem {

		/// <summary>Of the junk crates hauled up, the chance (out of TreasureMapChanceDenominator)
		/// that this one "contained a treasure map". Default 1/20 - judgment call, kept rare since
		/// the payoff is only a placeholder until a real treasure-map system exists. GM-tunable.
		/// </summary>
		public static int TreasureMapChanceNumerator = 1;
		public static int TreasureMapChanceDenominator = 20;

		public static string JunkCrateDefname = "i_crate_small";

		/// <summary>Placeholder treasure-map item granted when a junk crate "contained a map" -
		/// the generic blank rolled map, the closest real content. Replaced by a real treasure-map
		/// item once the follow-up treasure-map system (#530) is built. GM-tunable so that swap
		/// needs no code change.</summary>
		public static string TreasureMapDefname = "i_MAP_ROLLED";

		/// <summary>What an under-skilled fisher catches instead (Erebor's fixresource).</summary>
		public static string LowestFishResource = "w_fish1";

		/// <summary>How long a water spot with no region type at all stays empty.</summary>
		public static int EmptySpotRegenSeconds = 30 * 60;

		public enum CatchKind { Fish, JunkCrate, JunkCrateWithMap, Nothing }

		/// <summary>The outcome of one successful cast, resolved by Catch below.</summary>
		public struct CatchResult {
			public readonly CatchKind Kind;
			public readonly ItemDef ItemDef;
			public readonly ItemDef BonusMapDef; //non-null only for JunkCrateWithMap

			public CatchResult(CatchKind kind, ItemDef itemDef, ItemDef bonusMapDef) {
				this.Kind = kind;
				this.ItemDef = itemDef;
				this.BonusMapDef = bonusMapDef;
			}
		}

		private class Spot {
			public RegionResourceDef Resource; //null = nothing here
			public int RemainingPool;
			public double RegenReadyAtSeconds;
			public bool Untouched = true;
		}

		private static readonly Dictionary<(byte, int, int), Spot> spots = new Dictionary<(byte, int, int), Spot>();

		/// <summary>Resolves one successful cast at the given water tile. Pure resolution plus the
		/// spot's own bookkeeping - does NOT create any item; the caller creates whatever the result
		/// names. ItemDef may be null if a defname has no matching content (caller fails safe).</summary>
		public static CatchResult Catch(Character fisher, byte mapPlane, int x, int y) {
			Spot spot;
			if (!spots.TryGetValue((mapPlane, x, y), out spot)) {
				spot = new Spot();
				spots[(mapPlane, x, y)] = spot;
			}
			if (spot.Untouched || ((spot.RemainingPool <= 0) && (Globals.TimeInSeconds >= spot.RegenReadyAtSeconds))) {
				Assign(spot, mapPlane, x, y);
			}
			if (spot.RemainingPool <= 0) {
				return new CatchResult(CatchKind.Nothing, null, null);
			}

			RegionResourceDef caught = spot.Resource;
			if (fisher.GetRealSkillValue(SkillName.Fishing) < caught.SkillMin) {
				caught = RegionResourceDef.GetByDefname(LowestFishResource) ?? caught; //fixresource
			}
			spot.RemainingPool--;
			if (spot.RemainingPool <= 0) {
				spot.RegenReadyAtSeconds = Globals.TimeInSeconds + spot.Resource.RegenSeconds;
			}

			if (IsJunk(caught)) {
				ItemDef crate = ThingDef.GetByDefname(JunkCrateDefname) as ItemDef;
				if (RollHit(TreasureMapChanceNumerator, TreasureMapChanceDenominator)) {
					ItemDef map = ThingDef.GetByDefname(TreasureMapDefname) as ItemDef;
					return new CatchResult(CatchKind.JunkCrateWithMap, crate, map);
				}
				return new CatchResult(CatchKind.JunkCrate, crate, null);
			}
			return new CatchResult(CatchKind.Fish, caught.ReapDef, null);
		}

		private static void Assign(Spot spot, byte mapPlane, int x, int y) {
			spot.Untouched = false;
			RegionResourceDef resource = RegionResourceSystem.Pick(mapPlane, x, y, "t_water");
			if ((resource == null) || resource.IsNothing) {
				spot.Resource = null;
				spot.RemainingPool = 0;
				spot.RegenReadyAtSeconds = Globals.TimeInSeconds + (((resource != null) && (resource.RegenSeconds > 0)) ? resource.RegenSeconds : EmptySpotRegenSeconds);
				return;
			}
			spot.Resource = resource;
			spot.RemainingPool = Globals.dice.Next(resource.AmountMin, resource.AmountMax + 1);
		}

		/// <summary>The archive's junk fish (w_jf1-10 reap i_junkfish1-10).</summary>
		public static bool IsJunk(RegionResourceDef resource) {
			return (resource.Reap != null) && resource.Reap.StartsWith("i_junkfish", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Test-only: puts a known resource and pool on a spot, bypassing the region roll.</summary>
		internal static void ForceSpotForTest(byte mapPlane, int x, int y, RegionResourceDef resource, int pool) {
			spots[(mapPlane, x, y)] = new Spot { Resource = resource, RemainingPool = pool, Untouched = false };
		}

		private static bool RollHit(int numerator, int denominator) {
			if (denominator <= 0 || numerator <= 0) {
				return false;
			}
			return Globals.dice.Next(denominator) < numerator;
		}
	}
}
