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

using SteamEngine.Persistence;
using SteamEngine.Regions;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_knockback. Real archive
	/// unit is a 0-100 scale, NOT the 0-1000 permille the other abilities use (confirmed in
	/// source: `{0 100} &lt; tag.npc_knockback`, default flat 1-in-3 when unset) - the converter
	/// multiplies by 10 on the way in (see ConvertedCharDef.cs) so NPCDef.KnockbackChance stays
	/// on the SAME 0-1000 scale as every other ability here, not a special case a future reader
	/// has to remember.</summary>
	[ViewableClass]
	public class KnockbackAbilityDef : PassiveAbilityDef {
		private FieldValue chance;

		public KnockbackAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("knockbackChance", new double[] { 333, 333 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to knock the target back on a
		/// landed melee swing - deliberately shadows AbilityDef.Chance (different shape/scale)
		/// rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static KnockbackAbilityDef knockbackAbilityDef;
		private static KnockbackAbilityDef KnockbackDef {
			get {
				if (knockbackAbilityDef == null) {
					knockbackAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_knockback") as KnockbackAbilityDef;
				}
				return knockbackAbilityDef;
			}
		}

		/// <summary>#knockback - on a landed melee swing, a chance to push the defender one
		/// tile further away from the attacker - same tile-validity checks as PullToward
		/// (Roundattack.cs), just the inverse direction (away from, not toward, the attacker).</summary>
		public static void TryApplyKnockback(Character attacker, Character defender) {
			KnockbackAbilityDef def = KnockbackDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.KnockbackChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			PushAway(attacker, defender);
		}

		private static void PushAway(Character attacker, Character defender) {
			Direction dir = Point2D.GetDirFromTo(attacker, defender);
			int x = defender.X + directionDx[(int) dir];
			int y = defender.Y + directionDy[(int) dir];
			Map map = defender.GetMap();
			if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
				return; //no safe spot to land the push - skip it rather than clip into something
			}
			int tileId = map.GetTileId(x, y);
			if (TileData.HasFlag(TileData.GetTileFlags(tileId), TileFlag.Impassable)) {
				return;
			}
			int z = map.GetTileZ(x, y);
			defender.Go(x, y, z, defender.M);
		}
	}
}
