using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1411 - @hit on the attacker, then @getHit on the defender, for a landed blow, before its damage: the
	/// attacker's handler sets the damage, the defender's sees it and can change it again; a cancelled @hit ends the blow
	/// as a dodged one: no damage, no @getHit, @afterSwing still raised. A monster swings at the player (a monster's blow
	/// on a player keeps its damage: no minimal-damage rule). Protocol side: Tests/Protocol/MovementHoldsTests.cs.</summary>
	public static class HitTriggersTests {
		internal static int hits, getHits, afterSwings;
		internal static double getHitSaw;
		internal static bool zeroIt, cancelIt;

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
				int zeroedLost = Landed(self, orc, true, false);
				bool zeroedSaw = Math.Abs(getHitSaw - 37.5) < 0.01;
				int keptLost = Landed(self, orc, false, false);
				int hitsBefore = hits, getHitsBefore = getHits;
				int cancelledLost = Landed(self, orc, false, true);
				bool cancelled = (hits == hitsBefore + 1) && (getHits == getHitsBefore) && (afterSwings == 1) && (cancelledLost == 0);
				Console.WriteLine("HOLDS_HIT: hits=" + hits + " getHits=" + getHits + " zeroedLost=" + zeroedLost + " keptLost=" + keptLost
					+ " cancelledLost=" + cancelledLost + " afterSwings=" + afterSwings
					+ " saw=" + getHitSaw.ToString(CultureInfo.InvariantCulture));
				Console.WriteLine("HOLDS_CHECK: hitThenGetHit=" + ((hits == 3) && (getHits == 2) && zeroedSaw && (zeroedLost == 0) && (keptLost > 0)));
				Console.WriteLine("HOLDS_CHECK: cancelledHit=" + cancelled);
			} finally {
				cancelIt = false;
				zeroIt = false;
				orc.RemoveTriggerGroup(SingletonScript<E_HitProbe>.Instance);
				self.RemoveTriggerGroup(SingletonScript<E_GetHitProbe>.Instance);
				orc.Delete();
				self.Hits = self.MaxHits;
			}
			Console.WriteLine("HOLDS_HIT_DONE");
		}

		/// <summary>Swings until one blow lands (dodges don't reach @hit); the hits it cost. afterSwings counts the landed
		/// swing's @afterSwing.</summary>
		private static int Landed(Character self, NPC orc, bool zero, bool cancel) {
			zeroIt = zero;
			cancelIt = cancel;
			int before = hits;
			for (int i = 0; (i < 300) && (hits == before); i++) {
				self.Hits = self.MaxHits;
				afterSwings = 0;
				DamageManager.ProcessSwing(orc, self);
			}
			return self.MaxHits - self.Hits;
		}
	}

	public class E_HitProbe : CompiledTriggerGroup {
		public TriggerResult On_Hit(Character self, WeaponSwingArgs swingArgs) {
			HitTriggersTests.hits++;
			if (HitTriggersTests.cancelIt) {
				return TriggerResult.Cancel;
			}
			swingArgs.DamageAfterAC = 37.5;
			return TriggerResult.Continue;
		}

		public void On_AfterSwing(Character self, WeaponSwingArgs swingArgs) {
			HitTriggersTests.afterSwings++;
		}
	}

	public class E_GetHitProbe : CompiledTriggerGroup {
		public void On_GetHit(Character self, WeaponSwingArgs swingArgs) {
			HitTriggersTests.getHits++;
			HitTriggersTests.getHitSaw = swingArgs.DamageAfterAC;
			if (HitTriggersTests.zeroIt) {
				swingArgs.DamageAfterAC = 0;
			}
		}
	}
}
