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
	/// Fishing's monster ambush mechanic (#271) - mirrors Mining's MonsterAmbushSystem (#335) /
	/// Lumberjacking's LumberjackingAmbushSystem (#431) shape exactly (same unconditional
	/// 1/750-per-cast roll, fired from both On_Success and On_Fail), but with the water-specific
	/// monster table #271's own research named (frog / sea-serpent / kraken / water-elemental /
	/// water-spirit).
	///
	/// Unlike Mining (which had to substitute c_elem_earth_m / c_golem_copper for its missing
	/// rock-elemental / copper-spirit) and Lumberjacking (which substituted for its forest-spirit),
	/// Fishing's table is almost entirely a genuine 1:1 content match - c_bullfrog, c_sea_serpent,
	/// c_kraken and c_elem_water_m all really exist. Only "water spirit" has no exact content, so
	/// it's substituted to c_elem_water_m (same "nearest real equivalent" judgment call as the
	/// sibling systems, documented not silently dropped). All defnames are mutable statics so
	/// FishingSettings (#271 Phase E equivalent) can swap any of them GM-side without a code change.
	///
	/// Same deliberate simplification as Mining/Lumberjacking: the real source conditions the
	/// ambush table on a Tutorial-map variant; moriaengine has no tutorial-region equivalent, so
	/// only the main table is ported.
	/// </summary>
	public static class FishingAmbushSystem {
		public static int AmbushChanceDenominator = 750; //#271 - mutable so FishingSettings can expose it as GM-tunable

		public static string FrogDefname = "c_bullfrog";
		public static string SeaSerpentDefname = "c_sea_serpent";
		public static string KrakenDefname = "c_kraken";
		public static string WaterElementalDefname = "c_elem_water_m";
		public static string WaterSpiritDefname = "c_elem_water_m"; //substitution - no "water spirit" content exists, nearest real equivalent

		/// <summary>Rolls for an ambush and, if it fires, spawns the weighted-picked monster at
		/// `location`. No-op (including the roll itself not mattering) if the picked defname has no
		/// matching content - fails safe rather than throwing, same precedent as the sibling
		/// systems.</summary>
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

		/// <summary>5-bucket rand(128) roll - same bucket-boundary precedent as Mining's own 6-tier
		/// and Lumberjacking's 5-tier roll: >64 (~49%) frog / >32 (~25%) sea serpent / >16 (~12.5%)
		/// water elemental / >4 (~9.4%) water spirit / else (~3.1%, rarest) kraken. Frog is the
		/// common nuisance, kraken the rare big threat - matching the categories' own obvious
		/// danger ordering (no real skill-range table survives for Fishing any more than it did for
		/// Lumberjacking, same open-question framing).</summary>
		private static string PickWeightedMonsterDefname() {
			int roll = Globals.dice.Next(128);
			if (roll > 64) {
				return FrogDefname;
			}
			if (roll > 32) {
				return SeaSerpentDefname;
			}
			if (roll > 16) {
				return WaterElementalDefname;
			}
			if (roll > 4) {
				return WaterSpiritDefname;
			}
			return KrakenDefname;
		}
	}
}
