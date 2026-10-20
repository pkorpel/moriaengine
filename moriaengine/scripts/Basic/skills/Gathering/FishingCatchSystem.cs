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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Fishing's catch system (#271, mirrors Mining #333 / Lumberjacking #422 in spirit, but
	/// deliberately NOT a skill-gated resource-tier ladder - see below).
	///
	/// Why no fish-tier ladder: real Erebor fishing used a `w_fish1..N` skill-gated tier ladder,
	/// but those tier items DO NOT EXIST in moriaengine content. What does exist
	/// (i_fish_raw_whole / i_fish_small / i_fish_big_1..4) is a set of equal-value COSMETIC
	/// graphic variants (all T_FISH, "Fisherman" subsection, no surviving VALUE/rarity ordering).
	/// Fabricating skill thresholds to turn cosmetic equivalents into a rarity ladder would mean
	/// inventing tuning numbers with no real source - against this repo's standing convention
	/// (wire the real mechanism, don't hand-author unconfirmed tuning - see Mining's own history).
	/// So this is an honest "fish variety" roll instead of a fake tier ladder: a normal cast
	/// yields one of the real catchable fish graphics, picked uniformly.
	///
	/// Fishing's one genuinely-unique mechanic (not shared with Mining/Lumberjacking): the
	/// "junk fish" catch. On a tunable fraction of successful casts you haul up a junk crate
	/// instead of a fish (real Erebor's i_junkfish1..10 "crate pulled from the water"). The crate
	/// itself is real content (i_crate_small). Each junk crate carries an embedded, tunable chance
	/// of having contained a real treasure map - but moriaengine has NO treasure-map decode/dig
	/// mechanic at all (confirmed: only a blank T_MAP craftable i_MAP_ROLLED and a spawn-point
	/// TreasureChestDef exist). So, mirroring exactly how Mining shipped functional-now and
	/// deferred region-restriction to #338, the treasure-map payoff is a PLACEHOLDER (grants the
	/// generic i_MAP_ROLLED) pending a real follow-up (#530). The mechanism, the roll, and the
	/// GM-tunable chances are all real and wired now.
	/// </summary>
	public static class FishingCatchSystem {

		/// <summary>The real catchable fish graphics in moriaengine (all equal-value cosmetic
		/// variants - see class doc). Mutable so FishingSettings could re-point them, though
		/// they're content-tied defnames, not tuning numbers, so not exposed there by default -
		/// same scope decision as the ore/wood tier item defnames.</summary>
		public static readonly string[] FishDefnames = {
			"i_fish_raw_whole", "i_fish_small", "i_fish_big_1", "i_fish_big_2", "i_fish_big_3", "i_fish_big_4"
		};

		/// <summary>Chance (out of JunkChanceDenominator) that a successful cast hauls up a junk
		/// crate instead of a fish. Default 1/10 - a judgment call (no confirmed real rate; real
		/// Erebor's junkfish items each had their own independent spawn weighting we can't
		/// reconstruct), deliberately modest so normal fishing still mostly yields fish. GM-tunable
		/// via FishingSettings.</summary>
		public static int JunkChanceNumerator = 1;
		public static int JunkChanceDenominator = 10;

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

		public enum CatchKind { Fish, JunkCrate, JunkCrateWithMap }

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

		/// <summary>Resolves one successful cast: rolls junk-vs-fish, and (for junk) the embedded
		/// treasure-map sub-chance. Pure resolution only - does NOT create any item or touch the
		/// world; the caller creates whatever the result names (so this stays unit-testable
		/// without a backpack/world). ItemDef may be null if a defname has no matching content -
		/// the caller fails safe, same precedent as MonsterAmbushSystem.</summary>
		public static CatchResult Catch(Character fisher) {
			if (RollHit(JunkChanceNumerator, JunkChanceDenominator)) {
				ItemDef crate = ThingDef.GetByDefname(JunkCrateDefname) as ItemDef;
				if (RollHit(TreasureMapChanceNumerator, TreasureMapChanceDenominator)) {
					ItemDef map = ThingDef.GetByDefname(TreasureMapDefname) as ItemDef;
					return new CatchResult(CatchKind.JunkCrateWithMap, crate, map);
				}
				return new CatchResult(CatchKind.JunkCrate, crate, null);
			}

			return new CatchResult(CatchKind.Fish, PickFishDef(), null);
		}

		private static bool RollHit(int numerator, int denominator) {
			if (denominator <= 0 || numerator <= 0) {
				return false;
			}
			return Globals.dice.Next(denominator) < numerator;
		}

		private static ItemDef PickFishDef() {
			string defname = FishDefnames[Globals.dice.Next(FishDefnames.Length)];
			return ThingDef.GetByDefname(defname) as ItemDef;
		}
	}
}
