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
	/// Mining's monster ambush mechanic (#335, Phase C of #332) - real Erebor's own
	/// `miningfix.scp` `randompotvora`: a 1/750 chance per gather action (success or fail - the
	/// real `fixresource` rolls this unconditionally, not gated on whether the resource-type-fix
	/// itself did anything) spawns one monster near the miner, weighted via a `rand(128)` bucket
	/// roll (same boundary values as the real source: >64/>32/>16/>8/>2/else).
	///
	/// Content-substitution judgment call (the issue's own explicitly-flagged open question,
	/// not silently decided): moriaengine has no "rock elemental" or "copper spirit" NPC
	/// content at all. Shipped the issue's own first proposed option ("ship something
	/// functional now") rather than treating this as a blocking content-authoring gap -
	/// substituted `c_elem_earth_m` (the one earth-elemental variant that does exist) for BOTH
	/// the earth-elemental and rock-elemental tiers, and `c_golem_copper` (the only golem
	/// variant that exists) for BOTH the copper-spirit and random-golem tiers. All 6 defnames
	/// are plain mutable static fields specifically so a later GM-tunable `.settings` category
	/// (#337) can swap any substitution out without a code change - matching the issue's own
	/// explicit ask ("GM-tunable... which monster defnames back each weight tier").
	///
	/// Deliberate simplification, not enforced: the real source's cave-in roll and ambush roll
	/// are mutually exclusive on the same swing (cave-in's own `return 1` skips the ambush check
	/// entirely that swing). Not replicated here - the two rolls are independent, so a single
	/// swing could (extremely rarely, ~1/90000) trigger both. Judged not worth a signature
	/// change to MiningVeinSystem.Mine's existing callers for a nearly imperceptible edge case.
	/// </summary>
	public static class MonsterAmbushSystem {
		public static int AmbushChanceDenominator = 750; //#337 - mutable so MiningSettings can expose it as GM-tunable

		public static string RatDefname = "c_rat";
		public static string TrollDefname = "c_troll";
		public static string EarthElementalDefname = "c_elem_earth_m";
		public static string RockElementalDefname = "c_elem_earth_m"; //substitution, no real equivalent exists
		public static string CopperSpiritDefname = "c_golem_copper"; //substitution, no real equivalent exists
		public static string GolemDefname = "c_golem_copper"; //only golem variant that exists

		/// <summary>Rolls for an ambush and, if it fires, spawns the weighted-picked monster at
		/// `location`. No-op (including the roll itself not mattering) if the picked defname
		/// has no matching content - fails safe rather than throwing.</summary>
		public static void TryAmbush(IPoint4D location) {
			if (location.M == Camp.Plane) {
				return; //#1107 - Erebor's randompotvora never on plane 1, the labour camp's
			}
			if (Globals.dice.Next(AmbushChanceDenominator) != 0) {
				return;
			}

			string defname = PickWeightedMonsterDefname();
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname(defname);
			if (npcDef != null) {
				npcDef.Create(location);
			}
		}

		/// <summary>Real Erebor's own `rand(128)` bucket boundaries from `randompotvora` -
		/// >64 (~49%) / >32 (~25%) / >16 (~12.5%) / >8 (~6.25%) / >2 (~4.7%) / else (~2.3%,
		/// rarest tier).</summary>
		private static string PickWeightedMonsterDefname() {
			int roll = Globals.dice.Next(128);
			if (roll > 64) {
				return RatDefname;
			}
			if (roll > 32) {
				return TrollDefname;
			}
			if (roll > 16) {
				return EarthElementalDefname;
			}
			if (roll > 8) {
				return RockElementalDefname;
			}
			if (roll > 2) {
				return CopperSpiritDefname;
			}
			return GolemDefname;
		}
	}
}
