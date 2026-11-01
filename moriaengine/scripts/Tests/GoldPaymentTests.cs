using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#956 - server-side helpers for Tests/Protocol/GoldPaymentTests.cs.</summary>
	public static class GoldPaymentTests {
		private static Container bag, lockedBox;

		private static Item Gold(Container into, int amount) {
			Item gold = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(into);
			gold.Amount = amount;
			return gold;
		}

		private static string State(Character self) {
			return "pack=" + GoldUtils.CountGoldIn(self.Backpack) + " bag=" + GoldUtils.CountGoldIn(bag) + " bank=" + GoldUtils.CountGoldIn(self.Bank)
				+ " locked=" + GoldUtils.CountGoldIn(lockedBox);
		}

		/// <summary>Loose 100, 200 in a bag, 400 in the bank, and 800 behind a lock that doesn't count.</summary>
		[SteamFunction]
		public static void TestPayReset(Character self) {
			self.Account.PLevel = 1;
			foreach (Container container in new[] { self.Backpack, (Container) self.Bank }) {
				foreach (AbstractItem item in new System.Collections.Generic.List<AbstractItem>(container)) {
					item.Delete();
				}
			}
			bag = (Container) ((ItemDef) ThingDef.GetByDefname("i_bag")).Create(self.Backpack);
			lockedBox = (Container) ((ItemDef) ThingDef.GetByDefname("i_bag")).Create(self.Backpack);
			lockedBox.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_container_locked");
			Gold(self.Backpack, 100);
			Gold(bag, 200);
			Gold(self.Bank, 400);
			Gold(lockedBox, 800);
			Console.WriteLine("PAY_RESET: " + State(self) + " gm=" + self.IsGM);
		}

		[SteamFunction]
		public static void TestPay(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim('"').Split(' ');
			PaySource source = (PaySource) Enum.Parse(typeof(PaySource), args[1]);
			bool paid = GoldUtils.Pay(self, long.Parse(args[0]), source);
			Console.WriteLine("PAY_RESULT: paid=" + paid + " " + State(self));
		}

		[SteamFunction]
		public static void TestPayGm(Character self, ScriptArgs sa) {
			self.Account.PLevel = (byte) (sa.Args.Contains("on") ? self.Account.MaxPLevel : 1);
			Console.WriteLine("PAY_GM: " + self.IsGM);
		}
	}
}
