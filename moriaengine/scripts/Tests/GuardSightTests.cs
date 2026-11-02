using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 - helpers for Tests/Protocol/GuardSightTests.cs.</summary>
	public static class GuardSightTests {
		private static NPC guard;

		[SteamFunction]
		public static void TestSightReady(Character self, ScriptArgs sa) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			guard = (NPC) ThingDef.GetByDefname(sa.Args.Trim().Trim('"')).Create((ushort) (self.X + 3), self.Y, self.Z, self.M);
			guard.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			Console.WriteLine("SIGHT_READY: realm=" + guard.Realm + " guard=" + GuardSpeech.IsGuard(guard) + " mode=" + guard.AggroMode);
		}

		/// <summary>What the guard makes of me as each kind of player.</summary>
		[SteamFunction]
		public static void TestSightReport(Character self) {
			self.Account.PLevel = 1;
			string report = "";
			foreach (Realm realm in new[] { Realm.None, Realm.Gondor, Realm.Mordor }) {
				self.Realm = realm;
				Notoriety.ClearCriminal(self);
				report += " " + realm + "=" + GuardSpeech.AttacksOnSight(guard, self);
				Notoriety.FlagCriminal(self);
				report += " " + realm + "Crim=" + GuardSpeech.AttacksOnSight(guard, self);
				Notoriety.ClearCriminal(self);
			}
			self.Realm = Realm.None;
			Console.WriteLine("SIGHT_REPORT:" + report);
		}

		[SteamFunction]
		public static void TestSightGm(Character self) {
			self.Account.PLevel = 7;
			self.Realm = Realm.Mordor;
			Console.WriteLine("SIGHT_GM: attacked=" + GuardSpeech.AttacksOnSight(guard, self));
			self.Realm = Realm.None;
		}

		[SteamFunction]
		public static void TestSightAxe(Character self) {
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
			bool without = GuardSpeech.AttacksOnSight(guard, self);
			Item axe = (Item) ThingDef.GetByDefname("i_axe_copper").Create(self.Backpack);
			bool with = GuardSpeech.AttacksOnSight(guard, self);
			axe.Delete();
			self.Realm = Realm.None;
			self.Account.PLevel = 7;
			Console.WriteLine("SIGHT_AXE: without=" + without + " with=" + with);
		}

		[SteamFunction]
		public static void TestSightSummon(Character self) {
			NPC summon = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create((ushort) (self.X + 2), self.Y, self.Z, self.M);
			summon.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			SummonSpellDef.MarkAsSummonedCreature(summon);
			MonsterSpecialAttacks.TryReactToHit(summon, guard);
			Console.WriteLine("SIGHT_SUMMON: dead=" + (summon.IsDeleted || summon.Flag_Dead));
		}
	}
}
