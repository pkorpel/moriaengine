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
	/// base attack (RangerCombatTests.cs): RangerSettings damage/speed override, the #457
	/// arrow-tier placeholder bonus, and the stand-still toggle's default behavior
	/// (crossbow=locked, bow=free).
	/// </summary>
	public static class RangerSettingsTests {

		[SteamFunction]
		public static void TestGrantBowForSettingsCheck(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Str = 300; // same #96-follow-up reasoning as RangerCombatTests - keeps a clean
							 // attack value for the direct swing-formula checks below.

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

		/// <summary>Replaces whatever ammo is currently in self's backpack with a fresh stack
		/// of the given defname - used to swap arrow tiers mid-test for #457's bonus check.</summary>
		[SteamFunction]
		public static void TestReplaceArrowsWithValorite(Character self) {
			foreach (Item i in self.Backpack.EnumShallow()) {
				Projectile p = i as Projectile;
				if (p != null) {
					p.Delete();
				}
			}
			GrantArrows(self, "i_arrow_valorite");
			self.InvalidateCombatWeaponValues(); // force a fresh ammo lookup/tier recompute
		}

		[SteamFunction]
		public static void TestReportWeaponAttackVsP(Character self) {
			Console.WriteLine("RANGER_SETTINGS_TEST: attackVsP=" + self.WeaponAttackVsP);
		}

		[SteamFunction]
		public static void TestSetRangerBowDamage100(Character self) {
			RangerSettings.instance.bowDamage = 100;
			self.InvalidateCombatWeaponValues();
		}

		[SteamFunction]
		public static void TestSetRangerBowDamage1000(Character self) {
			RangerSettings.instance.bowDamage = 1000;
			self.InvalidateCombatWeaponValues();
		}

		[SteamFunction]
		public static void TestResetRangerBowDamage(Character self) {
			RangerSettings.instance.bowDamage = 5;
			self.InvalidateCombatWeaponValues();
		}

		/// <summary>#454 point 3 - starts a real ranged attack and schedules a mid-fight
		/// "disturb" that manually displaces the attacker 3 tiles towards the target (still well
		/// within range either way, so a failure to snap back doesn't show up as a stalled/
		/// aborted attack instead of a real behavioral difference), then a final check mirroring
		/// RangerStandoffCheckTimer's own pattern. expectReverted selects which outcome counts as
		/// a pass (crossbow should end up back at startX/startY; bow should end up at the
		/// disturbed position instead) - two separate no-arg commands rather than one taking a
		/// bool, matching this codebase's existing "typed SteamFunction params beyond Character
		/// self aren't a supported shape" convention (see LevelingTests.cs's own manually-parsed-
		/// ScriptArgs commands for the same reasoning).</summary>
		[SteamFunction]
		public static void TestRangerCrossbowStandStillCheck(Character self) {
			RunStandStillCheck(self, expectReverted: true);
		}

		[SteamFunction]
		public static void TestRangerBowStandStillCheck(Character self) {
			RunStandStillCheck(self, expectReverted: false);
		}

		private static void RunStandStillCheck(Character self, bool expectReverted) {
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 5, self.Y, self.Z, self.M);

			int startX = self.X;
			int startY = self.Y;
			int startingHits = dummy.Hits;
			int startingArrowCount = RangerCombatTests.CountArrows(self);

			self.Trigger_PlayerAttackRequest(dummy);

			self.AddTimer(RangerDisturbPositionTimer.TimerKey, new RangerDisturbPositionTimer(self, 3, 0))
				.DueInSeconds = 1;

			self.AddTimer(RangerStandStillCheckTimer.TimerKey,
				new RangerStandStillCheckTimer(self, dummy, startX, startY, startingHits, startingArrowCount, expectReverted))
				.DueInSeconds = 15;
			Console.WriteLine("RANGER_STANDSTILL_TEST: STARTED");
		}
	}

	/// <summary>One-shot mid-fight displacement - simulates the player trying to walk away
	/// while using a weapon whose stand-still toggle is being tested.</summary>
	[SaveableClass]
	public class RangerDisturbPositionTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerDisturbPositionTimer_");

		private Character self;
		private int dx, dy;

		[LoadingInitializer]
		public RangerDisturbPositionTimer() {
		}

		public RangerDisturbPositionTimer(Character self, int dx, int dy) {
			this.self = self;
			this.dx = dx;
			this.dy = dy;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.self.IsDeleted) {
				this.self.P(this.self.X + this.dx, this.self.Y + this.dy, this.self.Z, this.self.M);
				Console.WriteLine("RANGER_STANDSTILL_TEST: DISTURBED to (" + this.self.X + "," + this.self.Y + ")");
			}
		}
	}

	/// <summary>Mirrors RangerStandoffCheckTimer, plus the expectReverted branch needed to tell
	/// a stand-still weapon (should be back at startX/startY despite the disturbance above) from
	/// a free-moving one (should still be at the disturbed position).</summary>
	[SaveableClass]
	public class RangerStandStillCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerStandStillCheckTimer_");

		private Character attacker;
		private NPC dummy;
		private int startX, startY, startingHits, startingArrowCount;
		private bool expectReverted;

		[LoadingInitializer]
		public RangerStandStillCheckTimer() {
		}

		public RangerStandStillCheckTimer(Character attacker, NPC dummy, int startX, int startY,
				int startingHits, int startingArrowCount, bool expectReverted) {
			this.attacker = attacker;
			this.dummy = dummy;
			this.startX = startX;
			this.startY = startY;
			this.startingHits = startingHits;
			this.startingArrowCount = startingArrowCount;
			this.expectReverted = expectReverted;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool isAtStart = !this.attacker.IsDeleted && (this.attacker.X == this.startX) && (this.attacker.Y == this.startY);
			bool positionMatchesExpectation = this.expectReverted ? isAtStart : !isAtStart;
			bool dealtDamage = this.dummy.IsDeleted || (this.dummy.Hits < this.startingHits);
			int currentArrowCount = this.attacker.IsDeleted ? 0 : RangerCombatTests.CountArrows(this.attacker);
			bool ammoConsumed = currentArrowCount < this.startingArrowCount;

			if (positionMatchesExpectation && dealtDamage && ammoConsumed) {
				Console.WriteLine("RANGER_STANDSTILL_TEST: PASS (expectReverted=" + this.expectReverted +
					", landed damage, consumed ammo)");
			} else if (!positionMatchesExpectation) {
				Console.WriteLine("RANGER_STANDSTILL_TEST: FAIL (expectReverted=" + this.expectReverted +
					" but attacker ended at (" + (this.attacker.IsDeleted ? -1 : this.attacker.X) + "," +
					(this.attacker.IsDeleted ? -1 : this.attacker.Y) + "), started at (" + this.startX + "," + this.startY + "))");
			} else if (!dealtDamage) {
				Console.WriteLine("RANGER_STANDSTILL_TEST: FAIL (position matched expectation but dealt no damage)");
			} else {
				Console.WriteLine("RANGER_STANDSTILL_TEST: FAIL (dealt damage but never consumed ammo - startingArrowCount=" +
					this.startingArrowCount + " currentArrowCount=" + currentArrowCount + ")");
			}
		}
	}
}
