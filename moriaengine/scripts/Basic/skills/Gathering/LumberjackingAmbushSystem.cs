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
	/// Lumberjacking's monster ambush mechanic (#431, Phase C of #421) - mirrors Mining's
	/// MonsterAmbushSystem (#335) shape exactly (same unconditional 1/750-per-swing roll, fired
	/// from both On_Success and On_Fail), but with a tree-specific monster table instead of
	/// Mining's rat/troll/golem progression, per #421/#423's own framing.
	///
	/// Category weights are a judgment call, not sourced from a real Erebor table - #421/#423/
	/// #270 only name the categories (wolf/grizzly-bear/forest-elemental/forest-spirit/
	/// occasional wood-tier-keyed Reaper); no skill-range table exists anywhere in this repo's
	/// research, same open-question framing #335 itself had for its own table. Shipped as a
	/// 5-bucket rand(128) roll (one fewer than Mining's 6, since this table has one fewer named
	/// category), with the Reaper deliberately the rarest tier to match the issue's own
	/// "occasionally" wording.
	///
	/// Unlike every other category (one fixed defname each), the Reaper's specific variant is
	/// keyed off the wood tier actually yielded by the chop that triggered this roll - a genuine,
	/// real content match (c_reaper_spruce..c_reaper_elven exist 1:1 against WoodTiers.All,
	/// better coverage than Mining got for its own rock-elemental/copper-spirit substitutions).
	/// Falls back to the baseline ReaperSpruceDefname when no tier is available (On_Fail has no
	/// chop result to key off of, and an exhausted vein yields null too) - a judgment call, not
	/// sourced from any real table either.
	/// </summary>
	public static class LumberjackingAmbushSystem {
		public static int AmbushChanceDenominator = 750; //#425 - mutable so a future LumberjackingSettings can expose it

		public static string WolfDefname = "c_wolf";
		public static string GrizzlyBearDefname = "c_bear_grizzly";
		public static string ForestElementalDefname = "c_elem_forrest";
		public static string ForestSpiritDefname = "c_spirit_copper"; //substitution - no "forest spirit" content exists, nearest generic Spirit

		public static string ReaperSpruceDefname = "c_reaper_spruce";
		public static string ReaperChestnutDefname = "c_reaper_chesnut"; //real DEFNAME spelling in golems.def
		public static string ReaperOakDefname = "c_reaper_oak";
		public static string ReaperTeakDefname = "c_reaper_teak";
		public static string ReaperMahaganDefname = "c_reaper_mahagon";
		public static string ReaperEbenDefname = "c_reaper_eben";
		public static string ReaperElvenDefname = "c_reaper_elven";

		/// <summary>Rolls for an ambush and, if it fires, spawns the weighted-picked monster at
		/// `location`. `tierJustChopped` is the wood tier actually yielded by the triggering
		/// chop (null on a failed chop or an exhausted vein) - only consulted if the roll lands
		/// on the Reaper category. No-op (including the roll itself not mattering) if the picked
		/// defname has no matching content - fails safe rather than throwing.</summary>
		public static void TryAmbush(IPoint4D location, WoodTier tierJustChopped) {
			if (Globals.dice.Next(AmbushChanceDenominator) != 0) {
				return;
			}

			string defname = PickWeightedMonsterDefname(tierJustChopped);
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname(defname);
			if (npcDef != null) {
				npcDef.Create(location);
			}
		}

		/// <summary>5-bucket rand(128) roll - >64 (~49%) wolf / >32 (~25%) grizzly bear / >16
		/// (~12.5%) forest elemental / >4 (~9.4%) forest spirit / else (~3.1%, rarest) a
		/// wood-tier-keyed Reaper.</summary>
		private static string PickWeightedMonsterDefname(WoodTier tierJustChopped) {
			int roll = Globals.dice.Next(128);
			if (roll > 64) {
				return WolfDefname;
			}
			if (roll > 32) {
				return GrizzlyBearDefname;
			}
			if (roll > 16) {
				return ForestElementalDefname;
			}
			if (roll > 4) {
				return ForestSpiritDefname;
			}
			return ReaperDefnameForTier(tierJustChopped);
		}

		private static string ReaperDefnameForTier(WoodTier tier) {
			if (tier == WoodTiers.Iron) {
				return ReaperChestnutDefname;
			}
			if (tier == WoodTiers.Verite) {
				return ReaperOakDefname;
			}
			if (tier == WoodTiers.Valorite) {
				return ReaperTeakDefname;
			}
			if (tier == WoodTiers.Obsidian) {
				return ReaperMahaganDefname;
			}
			if (tier == WoodTiers.Adamantinum) {
				return ReaperEbenDefname;
			}
			if (tier == WoodTiers.Mithril) {
				return ReaperElvenDefname;
			}
			return ReaperSpruceDefname; //Copper, null (On_Fail/exhausted vein), or anything unresolvable
		}
	}
}
