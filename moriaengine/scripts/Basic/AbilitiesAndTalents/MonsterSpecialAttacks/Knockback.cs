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
		private FieldValue distance;

		public KnockbackAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("knockbackChance", new double[] { 333, 333 }, typeof(double[]));
			//user-requested default (was a hardcoded single tile) - PushAway below walks this
			//many tiles outward, stopping at the first blocked one, so a real obstacle still
			//shortens the push rather than being skipped over.
			this.distance = this.InitTypedField("knockbackDistance", (double) 3, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to knock the target back on a
		/// landed hit (melee OR ranged - DamageManager.ProcessSwing rolls this identically for
		/// any weapon type, confirmed real content already combines both: c_troll_strelec has
		/// a bow equipped alongside this ability) - deliberately shadows AbilityDef.Chance
		/// (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Tiles to push the defender away, in a straight line - PushAway stops early
		/// (at the furthest still-open tile) if a wall/water/map-edge is hit before reaching
		/// this many tiles.</summary>
		public double Distance {
			get { return (double) this.distance.CurrentValue; }
			set { this.distance.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static KnockbackAbilityDef knockbackAbilityDef;
		internal static KnockbackAbilityDef KnockbackDef {
			get {
				if (knockbackAbilityDef == null) {
					knockbackAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_knockback") as KnockbackAbilityDef;
				}
				return knockbackAbilityDef;
			}
		}

		/// <summary>#knockback - on a landed melee swing, a chance to push the defender away
		/// from the attacker by up to KnockbackAbilityDef.Distance tiles (default 3, #758),
		/// stopping early at the first blocked tile - same tile-validity checks as PullToward
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

			PushAway(attacker, defender, (int) def.Distance);
		}

		/// <summary>Walks up to `maxDistance` tiles away from `attacker`, stopping at the
		/// furthest tile that's still open (no water/impassable/map-edge) - a blocked tile
		/// SHORTENS the push rather than cancelling it outright, unlike the old single-tile
		/// version this replaced.</summary>
		private static void PushAway(Character attacker, Character defender, int maxDistance) {
			Direction dir = Point2D.GetDirFromTo(attacker, defender);
			Map map = defender.GetMap();
			int dx = directionDx[(int) dir];
			int dy = directionDy[(int) dir];
			int steps = ComputeMaxPushSteps(maxDistance, step => {
				int x = defender.X + (dx * step);
				int y = defender.Y + (dy * step);
				if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
					return false;
				}
				int tileId = map.GetTileId(x, y);
				return !TileData.HasFlag(TileData.GetTileFlags(tileId), TileFlag.Impassable);
			});
			if (steps > 0) {
				int x = defender.X + (dx * steps);
				int y = defender.Y + (dy * steps);
				int z = map.GetTileZ(x, y);
				defender.Go(x, y, z, defender.M);
			}
		}

		/// <summary>Pure "walk outward until blocked" step counter, factored out of PushAway so
		/// it's directly unit-testable without a live Map/Character (see
		/// Tests/KnockbackPushDistanceTests.cs) - `isStepOpen(step)` answers "is the tile at
		/// this many steps out still open" for steps 1..maxDistance in order; returns the
		/// largest step reached before the first `false` (0 if even step 1 is blocked).</summary>
		public static int ComputeMaxPushSteps(int maxDistance, Func<int, bool> isStepOpen) {
			int reached = 0;
			for (int step = 1; step <= maxDistance; step++) {
				if (!isStepOpen(step)) {
					break;
				}
				reached = step;
			}
			return reached;
		}
	}
}
