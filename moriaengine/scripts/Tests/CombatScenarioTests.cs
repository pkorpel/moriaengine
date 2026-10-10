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
	/// First demonstration of protocol-level "in-game" testing (see Tests/Protocol): a real
	/// connected client triggers this via a speech command (".TestTitanCombat") once logged in
	/// and in-world, same as any player-typed command. Spawns two titans, orders one to kill the
	/// other via the real NPCAiTimer/WeaponSkillTargetQueuePlugin combat path (the same path the
	/// "kill" pet command and the AddTarget-restart bug both go through), then logs a single
	/// PASS/FAIL marker line to stdout after giving combat time to actually land a hit -
	/// Tests/Protocol/CombatScenarioTests.cs asserts on that marker via
	/// GameServerPairFixture.WaitForGameOutput instead of visual in-game inspection.
	/// </summary>
	public static class CombatScenarioTests {
		private const double CheckDelaySeconds = 15;

		[SteamFunction]
		public static void TestTitanCombat(Character self) {
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			if (titanDef == null) {
				Console.WriteLine("TITAN_COMBAT_TEST: FAIL (c_titan NPCDef not found)");
				return;
			}

			// Spawned already adjacent (not a few tiles off) deliberately: this test's job is to
			// prove a hit lands once combat starts (the actual bug report), not to also exercise
			// NPCAiTimer's approach/pathfinding step - and PlayerCreation.startingPosition's
			// immediate surroundings turn out to be a small elevated platform (self.Z=20 while
			// the raw land tiles right next to it are Z=0), so anything placed more than ~1 tile
			// out risks landing off the edge, unable to move at all. Approach-to-range was
			// exercised and confirmed working separately (see NPCAiTimer.Fight's debug log)
			// during this scenario's development, just not at this exact spawn point.
			NPC titan1 = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC titan2 = (NPC) titanDef.Create(self.X + 2, self.Y, self.Z, self.M);

			NPCAiTimer attackerTimer = NPCAiTimer.GetFor(titan1);
			if (attackerTimer == null) {
				Console.WriteLine("TITAN_COMBAT_TEST: FAIL (titan1 has no NPCAiTimer installed)");
				return;
			}

			int startingHits = titan2.Hits;
			attackerTimer.EngageTarget(titan2);

			// Attached to self (the player, guaranteed to outlive the check), not to titan2:
			// BoundTimer.Cont returns null once its TagHolder IsDeleted (see its own class
			// comment), so a check timer attached directly to titan2 would silently never fire
			// its OnTimeout override in the (most interesting!) case where titan2 actually dies -
			// exactly the outcome this test wants to detect. Holding the victim as a plain field
			// instead sidesteps that entirely; a reference to a deleted Thing stays valid to read
			// (IsDeleted just becomes true), it's only Cont's own null-on-deleted behavior that's
			// the trap here.
			self.AddTimer(TitanCombatCheckTimer.TimerKey, new TitanCombatCheckTimer(titan2, startingHits))
				.DueInSeconds = CheckDelaySeconds;

			Console.WriteLine("TITAN_COMBAT_TEST: STARTED");
		}
	}

	[SaveableClass]
	public class TitanCombatCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_titanCombatCheckTimer_");

		//transient - this timer only ever exists for the lifetime of one test run, never persisted
		private NPC victim;
		private int startingHits;

		[LoadingInitializer]
		public TitanCombatCheckTimer() {
		}

		public TitanCombatCheckTimer(NPC victim, int startingHits) {
			this.victim = victim;
			this.startingHits = startingHits;
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC victim = this.victim;
			if (victim.IsDeleted) {
				Console.WriteLine("TITAN_COMBAT_TEST: PASS (victim titan died - started at " + this.startingHits + " hits)");
				return;
			}
			if (victim.Hits < this.startingHits) {
				Console.WriteLine("TITAN_COMBAT_TEST: PASS (hits " + this.startingHits + " -> " + victim.Hits + ")");
			} else {
				Console.WriteLine("TITAN_COMBAT_TEST: FAIL (no damage dealt - still at " + victim.Hits + " hits)");
			}
		}
	}
}
