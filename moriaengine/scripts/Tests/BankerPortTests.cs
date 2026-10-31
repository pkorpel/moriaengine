using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#883 - server-side helpers for Tests/Protocol/BankerPortTests.cs.</summary>
	public static class BankerPortTests {
		private static NPC banker;

		/// <summary>A realm banker next to me, me dressed or not, of its realm or not; my gold in the bank.</summary>
		[SteamFunction]
		public static void TestBankerPortSetup(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim().Trim('"').Split(' ');
			self.Account.PLevel = 1; //a GM pays nothing for cheques and purses
			self.Str = 100;
			if (banker != null && !banker.IsDeleted) {
				banker.Delete();
			}
			banker = (NPC) ThingDef.GetByDefname("c_banker_g").Create(self.X + 1, self.Y, self.Z, self.M);
			self.Realm = (args[0] == "gondor") ? Realm.Gondor : Realm.Mordor;
			AbstractItem pants = self.FindLayer(LayerNames.Pants);
			if (pants != null) {
				pants.Delete();
			}
			AbstractItem skirt = self.FindLayer(LayerNames.Skirt);
			if (skirt != null) {
				skirt.Delete();
			}
			AbstractItem robe = self.FindLayer(LayerNames.Robe);
			if (robe != null) {
				robe.Delete();
			}
			if (args[1] == "dressed") {
				self.TryEquip((Item) ThingDef.GetByDefname("i_pants_long").Create(self.Backpack));
			}
			foreach (AbstractItem item in new System.Collections.Generic.List<AbstractItem>(self.Bank)) {
				item.Delete();
			}
			GoldUtils.GrantToBank(self, uint.Parse(args[2]));
			Console.WriteLine("BANKER_SETUP: realm=" + banker.Realm + " x=" + self.X + " y=" + self.Y + " bank=" + (uint) self.Bank.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}

		[SteamFunction]
		public static void TestBankerPortDress(Character self) {
			self.TryEquip((Item) ThingDef.GetByDefname("i_pants_long").Create(self.Backpack));
			Console.WriteLine("BANKER_DRESSED: done");
		}

		[SteamFunction]
		public static void TestBankerPortStep(Character self) {
			self.Go(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("BANKER_STEP: done");
		}

		[SteamFunction]
		public static void TestBankerPortGive(Character self, ScriptArgs sa) {
			Item gold = (Item) GoldUtils.GoldDef.Create(self.Backpack);
			gold.Amount = int.Parse(sa.Args.Trim().Trim('"'));
			Console.WriteLine("BANKER_GOLD: uid=" + (uint) gold.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}

		[SteamFunction]
		public static void TestBankerPortBadCheck(Character self) {
			CashiersItem check = CashiersItem.CreateCheck(self, CashiersItem.MaxCheck);
			check.Value = CashiersItem.MaxCheck + 1;
			Console.WriteLine("BANKER_BADCHECK: done");
		}

		/// <summary>The gold, cheques and purses I hold, and the first purse's uid.</summary>
		[SteamFunction]
		public static void TestBankerPortReport(Character self) {
			string items = "";
			uint firstPurse = 0, firstCheck = 0;
			foreach (AbstractItem item in self.Backpack) {
				CashiersItem cashiers = item as CashiersItem;
				if (cashiers != null) {
					items += "," + cashiers.Def.PrettyDefname + ":" + cashiers.Value;
					if (cashiers.IsCheck) {
						firstCheck = (uint) cashiers.FlaggedUid;
					} else if (firstPurse == 0) {
						firstPurse = (uint) cashiers.FlaggedUid;
					}
				}
			}
			Console.WriteLine("BANKER_REPORT: bank=" + GoldUtils.CountGoldIn(self.Bank) + " pack=" + GoldUtils.CountGoldIn(self.Backpack) +
				" purse=" + firstPurse + " check=" + firstCheck + " items=" + items + " end");
		}
	}
}
