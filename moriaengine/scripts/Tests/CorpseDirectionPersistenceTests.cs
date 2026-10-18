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
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Regression coverage for #411: Corpse.direction is a plain .ct Var (private Direction
	/// direction, Corpse.ct), not a tag - a different save/load code path than #407's tag
	/// fix, generated via ClassTemplate.LoadFieldStatement/GeneratedCodeUtil instead of
	/// ObjectSaver's generic tag dispatch. #407's Save-side change (embedding "(Enum:Name)
	/// value" for every enum, unconditionally) broke this path's LoadLine, which parsed a
	/// bare ConvertTools.ParseByte(valueString) straight into the field - confirms the fix
	/// (excluding enums from the "simple direct-parse" codegen decision) actually produces
	/// a Corpse that survives a real save/reload with a non-default Direction.
	/// </summary>
	public static class CorpseDirectionPersistenceTests {
		[SteamFunction]
		public static void TestKillNpcWithDirectionAndReportCorpse(Character self) {
			NPCDef rabbitDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC rabbit = (NPC) rabbitDef.Create(self.X + 1, self.Y, self.Z, self.M);
			rabbit.Direction = Direction.SouthWest; //non-default (North/0) - exercises the
			//"this.direction != (Direction)0" ShouldBeSaved gate, same as the original bug.
			self.AddTimer(DeferredCorpseKillTimer.TimerKey, new DeferredCorpseKillTimer(rabbit)).DueInSeconds = 0;
		}
	}

	public class DeferredCorpseKillTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_deferredCorpseKillTimer_");

		private NPC victim;

		[LoadingInitializer]
		public DeferredCorpseKillTimer() {
		}

		public DeferredCorpseKillTimer(NPC victim) {
			this.victim = victim;
		}

		protected override void OnTimeout(TagHolder cont) {
			int x = this.victim.X;
			int y = this.victim.Y;
			var map = this.victim.GetMap();
			this.victim.CauseDeath(this.victim);
			Corpse corpse = null;
			foreach (Thing thing in map.GetThingsInRange(x, y, 1)) {
				corpse = thing as Corpse;
				if (corpse != null) {
					break;
				}
			}
			Console.WriteLine("CORPSE_DIRECTION_TEST: uid=" + corpse.Uid + " direction=" + corpse.Direction);
		}
	}
}
