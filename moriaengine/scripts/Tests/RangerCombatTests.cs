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
	/// Server-side helpers for Ranger's ranged attack (#454 follow-up). Unlike Mystic, Ranger's
	/// damage path is the engine's EXISTING generic physical-weapon formula (WeaponType.Bow/XBow
	/// already maps to DamageType.PhysicalArchery in CombatCalculator.GetWeaponDamageType) - no
	/// new damage-type branch, confirmed by this test landing damage through the unmodified
	/// path. The gap closed here is attack RANGE (CombatSettings.Ranges.bow/crossbow/
	/// heavyCrossbow) - unlike Mystic's profession-gated override, this applies to ANY player
	/// wielding one of these weapons, since range is a property of the weapon, not the wielder's
	/// profession.
	/// </summary>
	public static class RangerCombatTests {

		/// <summary>Creates a real bow on the ground at self's feet (same "ground, not
		/// backpack" reasoning as ProfessionTests.TestGrantItem - a freshly-granted backpack
		/// item isn't reachable without first opening the backpack) and real arrows directly in
		/// self's backpack (ammo doesn't need the pickup/equip dance, just needs to exist there
		/// for CombatCalculator.TryFindProjectile to find it).</summary>
		[SteamFunction]
		public static void TestGrantBowAndArrows(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			ThingDef bowDef = ThingDef.GetByDefname("i_bow_copper");
			AbstractItem bow = (AbstractItem) bowDef.Create(self.P());
			Console.WriteLine("RANGER_BOW_GRANTED: uid=" + bow.Uid);

			ThingDef arrowDef = ThingDef.GetByDefname("i_arrow_copper");
			Projectile arrows = (Projectile) arrowDef.Create(self.Backpack);
			arrows.Amount = 20;
		}

		/// <summary>#454 Phase A equivalent - confirms a REAL player-equipped bow actually
		/// fires, consumes ammo, and lands damage at range (not melee), through the real
		/// WeaponSkillDef swing loop (Trigger_PlayerAttackRequest - the same path a live
		/// client's "attack" uses), not a direct formula call. Zero test coverage existed for
		/// this before #454's own research - "presumably works, same code path as NPCs" was
		/// unproven. Call only after the client has actually equipped the bow granted by
		/// TestGrantBowAndArrows (real pickup+drop-to-wear packets, not a server shortcut).</summary>
		[SteamFunction]
		public static void TestRangerStandsOffAtRange(Character self) {
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 5, self.Y, self.Z, self.M); // beyond melee, within the bow range setting (8)

			int startX = self.X;
			int startY = self.Y;
			int startingHits = dummy.Hits;
			int startingArrowCount = CountArrows(self);

			self.Trigger_PlayerAttackRequest(dummy);

			self.AddTimer(RangerStandoffCheckTimer.TimerKey,
				new RangerStandoffCheckTimer(self, dummy, startX, startY, startingHits, startingArrowCount))
				.DueInSeconds = 15;
			Console.WriteLine("RANGER_STANDOFF_TEST: STARTED");
		}

		internal static int CountArrows(Character self) {
			int total = 0;
			foreach (Item i in self.Backpack.EnumShallow()) {
				Projectile p = i as Projectile;
				if (p != null) {
					total += p.Amount;
				}
			}
			return total;
		}
	}

	/// <summary>Mirrors MysticStandoffCheckTimer's pattern, plus an ammo-consumption check -
	/// the one extra thing a ranged weapon needs to prove beyond what Mystic's mind-power
	/// attack (no ammo at all) needed to.</summary>
	[SaveableClass]
	public class RangerStandoffCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_rangerStandoffCheckTimer_");

		private Character attacker;
		private NPC dummy;
		private int startX, startY, startingHits, startingArrowCount;

		[LoadingInitializer]
		public RangerStandoffCheckTimer() {
		}

		public RangerStandoffCheckTimer(Character attacker, NPC dummy, int startX, int startY, int startingHits, int startingArrowCount) {
			this.attacker = attacker;
			this.dummy = dummy;
			this.startX = startX;
			this.startY = startY;
			this.startingHits = startingHits;
			this.startingArrowCount = startingArrowCount;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool stayedPut = !this.attacker.IsDeleted && (this.attacker.X == this.startX) && (this.attacker.Y == this.startY);
			bool dealtDamage = this.dummy.IsDeleted || (this.dummy.Hits < this.startingHits);
			int currentArrowCount = this.attacker.IsDeleted ? 0 : RangerCombatTests.CountArrows(this.attacker);
			bool ammoConsumed = currentArrowCount < this.startingArrowCount;

			if (stayedPut && dealtDamage && ammoConsumed) {
				Console.WriteLine("RANGER_STANDOFF_TEST: PASS (ranger stayed at range, landed damage, consumed ammo)");
			} else if (!stayedPut) {
				Console.WriteLine("RANGER_STANDOFF_TEST: FAIL (ranger moved from (" + this.startX + "," + this.startY + ") to (" +
					(this.attacker.IsDeleted ? -1 : this.attacker.X) + "," + (this.attacker.IsDeleted ? -1 : this.attacker.Y) + "))");
			} else if (!dealtDamage) {
				Console.WriteLine("RANGER_STANDOFF_TEST: FAIL (stayed at range but dealt no damage)");
			} else {
				Console.WriteLine("RANGER_STANDOFF_TEST: FAIL (dealt damage but never consumed ammo - startingArrowCount=" +
					this.startingArrowCount + " currentArrowCount=" + currentArrowCount + ")");
			}
		}
	}
}
