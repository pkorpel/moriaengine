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
			//many tiles outward, stopping at the first blocked step, so a real obstacle still
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
		/// (at the last tile reached) at anything that would stop a walking character: a wall,
		/// water, a cliff, the map edge or another character (#781).</summary>
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
		/// stopping early at the first blocked step - the same forced-step walk (ForceMove) as
		/// PullToward (Roundattack.cs), just the inverse direction (away from, not toward, the attacker).</summary>
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

		/// <summary>#781 - pushes `defender` up to `maxDistance` tiles straight away from `attacker`,
		/// one real step at a time (see WalkForced): a wall, a closed door, water, a cliff or another
		/// character ends the push at the last tile reached - right by a wall that's no push at all.
		/// It used to look only at the terrain tile under each step, so house walls (statics) never
		/// stopped it and it knocked people inside buildings.</summary>
		private static void PushAway(Character attacker, Character defender, int maxDistance) {
			Direction dir = Point2D.GetDirFromTo(attacker, defender);
			ForceMove(defender, maxDistance, (x, y) => dir, (x, y) => false);
		}

		/// <summary>#781 - moves `victim` by forced steps (knockback, pull) using the same rules as
		/// walking - Map.CheckMovement with the victim's own movement settings, so statics, walls,
		/// diagonal corners and floor heights all count - and never onto a living character's tile.
		/// Lands on the real Z of the last tile reached.</summary>
		internal static void ForceMove(Character victim, int maxSteps, Func<int, int, Direction> nextDir, Func<int, int, bool> arrived) {
			Map map = victim.GetMap();
			IMovementSettings settings = victim.MovementSettings;
			AStar.StepFunc step = delegate (int x, int y, int z, Direction d, out int newX, out int newY, out int newZ) {
				if (!map.CheckMovement(new Point3D(x, y, z), settings, d, false, out newX, out newY, out newZ)) {
					return false;
				}
				return CharacterCollision.FindObstacle(victim, map, newX, newY, newZ) == null;
			};
			int endX, endY, endZ;
			int steps = WalkForced(victim.X, victim.Y, victim.Z, maxSteps, nextDir, arrived, step, out endX, out endY, out endZ);
			if (steps > 0) {
				victim.Go(endX, endY, endZ, victim.M);
			}
		}

		/// <summary>#781 - pure "walk until blocked": from (x, y, z), up to maxSteps steps in
		/// nextDir(x, y) each, stopping early once arrived(x, y) or at the first step `step` refuses.
		/// Returns the steps taken; (endX, endY, endZ) is where they lead (the start if none).
		/// Unit-tested on synthetic grids (Tests/KnockbackPushDistanceTests.cs).</summary>
		public static int WalkForced(int x, int y, int z, int maxSteps, Func<int, int, Direction> nextDir, Func<int, int, bool> arrived,
				AStar.StepFunc step, out int endX, out int endY, out int endZ) {
			endX = x;
			endY = y;
			endZ = z;
			int taken = 0;
			while ((taken < maxSteps) && !arrived(endX, endY)) {
				int nx, ny, nz;
				if (!step(endX, endY, endZ, nextDir(endX, endY), out nx, out ny, out nz)) {
					break;
				}
				endX = nx;
				endY = ny;
				endZ = nz;
				taken++;
			}
			return taken;
		}
	}
}
