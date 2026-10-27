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
	/// #709 - real-user-reported: c_titan_ranger (outside, open ground, ~5 tiles from c_titan)
	/// fired a few shots then stopped attacking entirely. Root cause: WeaponSkillDef.On_Stroke's
	/// "arrow appears in target's backpack" branch set a duped projectile's Amount AFTER moving
	/// it into the target's backpack via the Cont setter - but Cont's setter auto-merges a
	/// stackable item into an already-matching stack in the destination
	/// (AbstractItem.TryStackToAnyInside/Trigger_StackInCont) and DELETES the incoming item as
	/// part of that merge. #667 made arrows/bolts/scrolls stackable, so once the target's
	/// backpack already held one dropped arrow from an earlier shot, the next shot's dupe got
	/// silently deleted mid-merge, then crashed on the very next line trying to set Amount on the
	/// now-deleted object - aborting the whole stroke uncaught and leaving the NPC's combat
	/// sequence stuck (CurrentSkill cleared, never resumed). Fixed by setting Amount before Cont.
	///
	/// Drives a real sustained fight (not a single direct call) since the bug only manifests once
	/// the target's backpack already holds a prior dropped arrow - repeated real strokes are what
	/// make that collision happen, same spirit as this codebase's other real-time combat
	/// scenarios (CombatScenarioTests.cs).
	/// </summary>
	public static class RangedCombatSustainTests {
		internal const double CheckIntervalSeconds = 10;
		internal const int MaxChecks = 4; //40s total - comfortably enough shots (one every ~2s) for the backpack-stacking collision to occur at least once if the bug is present

		[SteamFunction]
		public static void TestRangerSustainedAttack(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			NPCDef rangerDef = ThingDef.GetByDefname("c_titan_ranger") as NPCDef;
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC ranger = (NPC) rangerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC titan = (NPC) titanDef.Create(self.X + 5, self.Y, self.Z, self.M);

			NPCAiTimer rangerTimer = NPCAiTimer.GetFor(ranger);
			rangerTimer.EngageTarget(titan);

			self.AddTimer(RangedSustainCheckTimer.TimerKey, new RangedSustainCheckTimer(ranger, titan, 1))
				.DueInSeconds = CheckIntervalSeconds;

			Console.WriteLine("RANGED_SUSTAIN_TEST: STARTED rangerUid=" + ranger.Uid + " titanUid=" + titan.Uid);
		}
	}

	[SaveableClass]
	public class RangedSustainCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangedSustainCheckTimer_");

		//transient - this timer only ever exists for the lifetime of one test run, never persisted
		private NPC ranger;
		private NPC titan;
		private int checkNumber;

		[LoadingInitializer]
		public RangedSustainCheckTimer() {
		}

		public RangedSustainCheckTimer(NPC ranger, NPC titan, int checkNumber) {
			this.ranger = ranger;
			this.titan = titan;
			this.checkNumber = checkNumber;
			//placeholder non-negative PeriodInSeconds - a timer only ever driven by explicit
			//DueInSeconds reassignment (no fixed period) reads as "disabled" the moment it
			//fires, and the base BoundTimer.OnTimeout() wrapper auto-deletes on exactly that
			//condition (same gotcha already documented on PetHungerTimer).
			this.PeriodInSeconds = RangedCombatSustainTests.CheckIntervalSeconds;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool rangerDead = this.ranger.IsDeleted;
			bool titanDead = !rangerDead && this.titan.IsDeleted;
			bool engaging = !rangerDead && !titanDead && WeaponSkillTargetQueuePlugin.IsActivelyEngaging(this.ranger, this.titan);
			SkillDef currentSkill = rangerDead ? null : this.ranger.CurrentSkill;

			if (this.checkNumber < RangedCombatSustainTests.MaxChecks) {
				this.checkNumber++;
				this.DueInSeconds = RangedCombatSustainTests.CheckIntervalSeconds;
			}

			Console.WriteLine("RANGED_SUSTAIN_TEST: check=" + this.checkNumber +
				" rangerDead=" + rangerDead + " titanDead=" + titanDead +
				" engaging=" + engaging +
				" currentSkill=" + (currentSkill != null ? currentSkill.Defname : "none"));
		}
	}
}
