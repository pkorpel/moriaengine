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
	/// Server-side helpers for the #456/#457/run-stand-archery follow-up on top of Ranger's
	/// base attack (RangerCombatTests.cs): the ammo's damage multiplier and the stand-still toggle's default behavior
	/// (crossbow=locked, bow=free).
	/// </summary>
	public static class RangerSettingsTests {

		[SteamFunction]
		public static void TestGrantBowForSettingsCheck(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Str = 300; // same #96-follow-up reasoning as RangerCombatTests - keeps a clean
							 // attack value for the direct swing-formula checks below.
			self.Dex = 100; // #860 - a real bow swing takes 2 x hitspeed, 12 s at the starting Dex

			ThingDef bowDef = ThingDef.GetByDefname("i_bow_copper");
			AbstractItem bow = (AbstractItem) bowDef.Create(self.P());
			Console.WriteLine("RANGER_SETTINGS_BOW_GRANTED: uid=" + bow.Uid);

			GrantArrows(self, "i_arrow_copper");
		}

		[SteamFunction]
		public static void TestGrantCrossbowForStandStillCheck(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Str = 300;

			ThingDef crossbowDef = ThingDef.GetByDefname("i_crossbow_copper");
			AbstractItem crossbow = (AbstractItem) crossbowDef.Create(self.P());
			Console.WriteLine("RANGER_SETTINGS_CROSSBOW_GRANTED: uid=" + crossbow.Uid);

			GrantArrows(self, "i_bolt_jagged_copper");
		}

		private static void GrantArrows(Character self, string ammoDefname) {
			ThingDef ammoDef = ThingDef.GetByDefname(ammoDefname);
			Projectile ammo = (Projectile) ammoDef.Create(self.Backpack);
			ammo.Amount = 50;
		}

		/// <summary>Replaces whatever ammo is currently in self's backpack with plain bolts.</summary>
		[SteamFunction]
		public static void TestReplaceAmmoWithPlainBolts(Character self) {
			foreach (Item i in self.Backpack.EnumShallow()) {
				Projectile p = i as Projectile;
				if (p != null) {
					p.Delete();
				}
			}
			GrantArrows(self, "i_xbolt_copper");
			self.InvalidateCombatWeaponValues(); // force a fresh ammo lookup
		}

		[SteamFunction]
		public static void TestReportWeaponAttackVsP(Character self) {
			Console.WriteLine("RANGER_SETTINGS_TEST: attackVsP=" + self.WeaponAttackVsP);
		}

		/// <summary>#512 - reworked from the original snap-back mechanic (see git history for
		/// the old version) after the user found the teleport-back itself was the bug (#513).
		/// New behavior: a stand-still weapon fails the stroke (no damage, no ammo spent) for as
		/// long as Character.Flag_Moving is true, with no position change at all; standing still
		/// again lets the attack resume. Starts a real ranged attack, flips Flag_Moving true for
		/// a window long enough to span several stroke attempts, samples Hits/ammo mid-window
		/// (proves nothing landed WHILE moving - the thing the old mechanic got wrong, since it
		/// let the swing complete right after silently repositioning the attacker), then flips
		/// Flag_Moving back to false and samples again (proves the attack resumes on its own,
		/// no re-target needed). expectBlockedWhileMoving selects which outcome counts as a pass
		/// (crossbow should show zero damage/ammo-use during the moving window; bow should keep
		/// landing hits throughout, unaffected) - two separate no-arg commands rather than one
		/// taking a bool, matching this codebase's existing "typed SteamFunction params beyond
		/// Character self aren't a supported shape" convention (see LevelingTests.cs's own
		/// manually-parsed-ScriptArgs commands for the same reasoning). Flag_Moving is toggled
		/// directly rather than via a real walk request - confirmed safe: the property's only
		/// background writer (CharSyncQueue's post-movement-sync reset) is driven off the
		/// movement-queue pipeline, which a direct property set never enters, so nothing resets
		/// it out from under this test between the two timers below.</summary>
		[SteamFunction]
		public static void TestRangerCrossbowStandStillCheck(Character self) {
			RunStandStillCheck(self, expectBlockedWhileMoving: true);
		}

		[SteamFunction]
		public static void TestRangerBowStandStillCheck(Character self) {
			RunStandStillCheck(self, expectBlockedWhileMoving: false);
		}

		private static void RunStandStillCheck(Character self, bool expectBlockedWhileMoving) {
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 5, self.Y, self.Z, self.M);

			int startingHits = dummy.Hits;
			int startingArrowCount = RangerCombatTests.CountArrows(self);

			self.Trigger_PlayerAttackRequest(dummy);

			self.AddTimer(RangerStartMovingTimer.TimerKey, new RangerStartMovingTimer(self))
				.DueInSeconds = 1;

			self.AddTimer(RangerMidMoveCheckTimer.TimerKey,
				new RangerMidMoveCheckTimer(self, dummy, startingHits, startingArrowCount, expectBlockedWhileMoving))
				.DueInSeconds = 6;

			self.AddTimer(RangerStopMovingTimer.TimerKey, new RangerStopMovingTimer(self))
				.DueInSeconds = 6.5;

			self.AddTimer(RangerFinalStandStillCheckTimer.TimerKey,
				new RangerFinalStandStillCheckTimer(self, dummy, startingArrowCount))
				.DueInSeconds = 12;
			Console.WriteLine("RANGER_STANDSTILL_TEST: STARTED");
		}
	}

	/// <summary>#512 - flips Flag_Moving on, simulating the player starting to walk away while
	/// using a weapon whose stand-still toggle is being tested. No position change at all -
	/// the whole point of the rework is that movement fails the attack without teleporting
	/// anyone anywhere.</summary>
	[SaveableClass]
	public class RangerStartMovingTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerStartMovingTimer_");

		private Character self;

		[LoadingInitializer]
		public RangerStartMovingTimer() {
		}

		public RangerStartMovingTimer(Character self) {
			this.self = self;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.self.IsDeleted) {
				this.self.Flag_Moving = true;
				Console.WriteLine("RANGER_STANDSTILL_TEST: STARTED_MOVING");
			}
		}
	}

	/// <summary>#512 - sampled while Flag_Moving is still true (set by RangerStartMovingTimer
	/// above, cleared later by RangerStopMovingTimer) - proves a stand-still weapon lands zero
	/// damage/spends no ammo WHILE moving (the thing the old snap-back mechanic got wrong: it
	/// let the swing complete right after silently repositioning the attacker back into place).
	/// A free-moving weapon (bow) should show the opposite - damage/ammo already moving by now.</summary>
	[SaveableClass]
	public class RangerMidMoveCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerMidMoveCheckTimer_");

		private Character attacker;
		private NPC dummy;
		private int startingHits, startingArrowCount;
		private bool expectBlockedWhileMoving;

		[LoadingInitializer]
		public RangerMidMoveCheckTimer() {
		}

		public RangerMidMoveCheckTimer(Character attacker, NPC dummy, int startingHits, int startingArrowCount, bool expectBlockedWhileMoving) {
			this.attacker = attacker;
			this.dummy = dummy;
			this.startingHits = startingHits;
			this.startingArrowCount = startingArrowCount;
			this.expectBlockedWhileMoving = expectBlockedWhileMoving;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool dealtDamage = this.dummy.IsDeleted || (this.dummy.Hits < this.startingHits);
			int currentArrowCount = this.attacker.IsDeleted ? 0 : RangerCombatTests.CountArrows(this.attacker);
			bool ammoConsumed = currentArrowCount < this.startingArrowCount;

			if (this.expectBlockedWhileMoving) {
				if (!dealtDamage && !ammoConsumed) {
					Console.WriteLine("RANGER_STANDSTILL_TEST: MIDMOVE_PASS (blocked while moving, as expected)");
				} else {
					Console.WriteLine("RANGER_STANDSTILL_TEST: MIDMOVE_FAIL (expected no damage/ammo-use while moving, " +
						"but dealtDamage=" + dealtDamage + " ammoConsumed=" + ammoConsumed + ")");
				}
			} else {
				if (dealtDamage && ammoConsumed) {
					Console.WriteLine("RANGER_STANDSTILL_TEST: MIDMOVE_PASS (free-moving weapon unaffected by moving)");
				} else {
					Console.WriteLine("RANGER_STANDSTILL_TEST: MIDMOVE_FAIL (expected damage/ammo-use despite moving, " +
						"but dealtDamage=" + dealtDamage + " ammoConsumed=" + ammoConsumed + ")");
				}
			}
		}
	}

	/// <summary>#512 - clears Flag_Moving, simulating the player standing still again.</summary>
	[SaveableClass]
	public class RangerStopMovingTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerStopMovingTimer_");

		private Character self;

		[LoadingInitializer]
		public RangerStopMovingTimer() {
		}

		public RangerStopMovingTimer(Character self) {
			this.self = self;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.self.IsDeleted) {
				this.self.Flag_Moving = false;
				Console.WriteLine("RANGER_STANDSTILL_TEST: STOPPED_MOVING");
			}
		}
	}

	/// <summary>#512 - sampled after standing still again (RangerStopMovingTimer above) - proves
	/// a stand-still weapon's attack resumes on its own once the attacker stops moving, with no
	/// need to re-target/re-issue the attack.</summary>
	[SaveableClass]
	public class RangerFinalStandStillCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerFinalStandStillCheckTimer_");

		private Character attacker;
		private NPC dummy;
		private int startingArrowCount;

		[LoadingInitializer]
		public RangerFinalStandStillCheckTimer() {
		}

		public RangerFinalStandStillCheckTimer(Character attacker, NPC dummy, int startingArrowCount) {
			this.attacker = attacker;
			this.dummy = dummy;
			this.startingArrowCount = startingArrowCount;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool dealtDamage = this.dummy.IsDeleted;
			int currentArrowCount = this.attacker.IsDeleted ? 0 : RangerCombatTests.CountArrows(this.attacker);
			bool ammoConsumed = currentArrowCount < this.startingArrowCount;
			if (!this.dummy.IsDeleted) {
				dealtDamage = this.dummy.Hits < this.dummy.MaxHits;
			}

			if (dealtDamage && ammoConsumed) {
				Console.WriteLine("RANGER_STANDSTILL_TEST: PASS (resumed and landed damage after standing still again)");
			} else {
				Console.WriteLine("RANGER_STANDSTILL_TEST: FAIL (expected the attack to have landed by now - dealtDamage=" +
					dealtDamage + " ammoConsumed=" + ammoConsumed + ")");
			}
		}
	}
}
