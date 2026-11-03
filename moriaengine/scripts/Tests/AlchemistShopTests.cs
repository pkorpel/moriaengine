using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - an alchemist beside the player, of the player's realm or not.</summary>
	public static class AlchemistShopTests {
		private static NPC alchemist;

		[SteamFunction]
		public static void TestAlchemistPrepare(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
			alchemist = (NPC) ((NPCDef) ThingDef.GetByDefname(AlchemistShop.NpcDef)).Create(self.X + 1, self.Y, self.Z, self.M);
			alchemist.Realm = Realm.Gondor;
			((Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(self.Backpack)).Amount = 50000;
			Console.WriteLine("ALCHEMIST_READY gold=" + self.Gold + " alchemist=" + ((uint) alchemist.Uid));
		}

		[SteamFunction]
		public static void TestAlchemistOtherRealm(Character self) {
			alchemist.Realm = Realm.Mordor;
			Console.WriteLine("ALCHEMIST_OTHER_REALM");
		}

		[SteamFunction]
		public static void TestAlchemistReport(Character self) {
			int candles = 0, sight = 0;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == ThingDef.GetByDefname("i_svice_zivota")) {
					candles++;
				} else if (item.Def == ThingDef.GetByDefname("i_potion_HPsightLess")) {
					sight += item.Amount;
				}
			}
			Console.WriteLine("ALCHEMIST_REPORT: gold=" + self.Gold + " candles=" + candles + " sight=" + sight + " end");
		}
	}
}
