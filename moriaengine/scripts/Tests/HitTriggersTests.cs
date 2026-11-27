using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1411 - @hit on the attacker, then @getHit on the defender, for a landed blow, before its damage: the
	/// attacker's handler sets the damage, the defender's sees it and can change it again. #1558 - neither cancels any more;
	/// a dodged blow raises the attacker's @hit (Dodged), no @getHit, no damage, and @afterSwing. A monster swings at the
	/// player (a monster's blow on a player keeps its damage: no minimal-damage rule). Protocol side:
	/// Tests/Protocol/MovementHoldsTests.cs.</summary>
	public static class HitTriggersTests {
		internal static int hits, getHits, afterSwings;
		internal static double getHitSaw;
		internal static bool zeroIt, sawDodged;

		[SteamFunction]
		public static void TestHitTriggers(Character self) {
			self.Account.PLevel = 1;
			hits = 0;
			getHits = 0;
			getHitSaw = 0;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			NPC orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(self.X + 1, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(orc, 300);
			orc.AddTriggerGroup(SingletonScript<E_HitProbe>.Instance);
			self.AddTriggerGroup(SingletonScript<E_GetHitProbe>.Instance);
			self.MaxHits = 1000;
			try {
				int zeroedLost = Landed(self, orc, true);
				bool zeroedSaw = Math.Abs(getHitSaw - 37.5) < 0.01;
				int keptLost = Landed(self, orc, false);
				int hitsBefore = hits, getHitsBefore = getHits;
				int dodgedLost = Dodged(self, orc);
				bool dodged = (hits == hitsBefore + 1) && sawDodged && (getHits == getHitsBefore) && (afterSwings == 1) && (dodgedLost == 0);
				Console.WriteLine("HOLDS_HIT: hits=" + hits + " getHits=" + getHits + " zeroedLost=" + zeroedLost + " keptLost=" + keptLost
					+ " dodgedLost=" + dodgedLost + " sawDodged=" + sawDodged + " afterSwings=" + afterSwings
					+ " saw=" + getHitSaw.ToString(CultureInfo.InvariantCulture));
				Console.WriteLine("HOLDS_CHECK: hitThenGetHit=" + ((hits == 3) && (getHits == 2) && zeroedSaw && (zeroedLost == 0) && (keptLost > 0)));
				Console.WriteLine("HOLDS_CHECK: dodgedHit=" + dodged);
			} finally {
				zeroIt = false;
				orc.RemoveTriggerGroup(SingletonScript<E_HitProbe>.Instance);
				self.RemoveTriggerGroup(SingletonScript<E_GetHitProbe>.Instance);
				orc.Delete();
				self.Hits = self.MaxHits;
			}
			Console.WriteLine("HOLDS_HIT_DONE");
		}

		/// <summary>One swing (the player has no dodge); the hits it cost.</summary>
		private static int Landed(Character self, NPC orc, bool zero) {
			zeroIt = zero;
			self.Hits = self.MaxHits;
			afterSwings = 0;
			DamageManager.ProcessSwing(orc, self);
			return self.MaxHits - self.Hits;
		}

		/// <summary>#1558 - one swing the player dodges for sure (a warrior's a_weapon_dodge, each point worth the whole roll);
		/// the hits it cost.</summary>
		private static int Dodged(Character self, NPC orc) {
			Player player = (Player) self;
			ProfessionDef original = player.Profession;
			player.Profession = Professions.Warrior;
			AbilityDef dodge = AbilityDef.GetByDefname("a_weapon_dodge");
			int perPoint = CombatSettings.instance.weaponDodgePerPoint;
			CombatSettings.instance.weaponDodgePerPoint = 1000;
			self.SetRealAbilityPoints(dodge, 1);
			sawDodged = false;
			try {
				return Landed(self, orc, false);
			} finally {
				CombatSettings.instance.weaponDodgePerPoint = perPoint;
				self.SetRealAbilityPoints(dodge, 0);
				player.Profession = original;
			}
		}
	}

	public partial class E_HitProbe : CompiledTriggerGroup {
		public void On_Hit(Character self, WeaponSwingArgs swingArgs) {
			HitTriggersTests.hits++;
			HitTriggersTests.sawDodged |= swingArgs.Dodged;
			swingArgs.DamageAfterAC = 37.5;
		}

		public void On_AfterSwing(Character self, WeaponSwingArgs swingArgs) {
			HitTriggersTests.afterSwings++;
		}
	}

	public partial class E_GetHitProbe : CompiledTriggerGroup {
		public void On_GetHit(Character self, WeaponSwingArgs swingArgs) {
			HitTriggersTests.getHits++;
			HitTriggersTests.getHitSaw = swingArgs.DamageAfterAC;
			if (HitTriggersTests.zeroIt) {
				swingArgs.DamageAfterAC = 0;
			}
		}
	}
}
