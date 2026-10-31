using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#889 - server-side helpers for Tests/Protocol/BankStatusTests.cs.</summary>
	public static class BankStatusTests {
		private static string Stones(float weight) {
			return weight.ToString("0.###", CultureInfo.InvariantCulture);
		}

		private static Item Add(string defname, Container cont, int amount) {
			Item item = (Item) ThingDef.GetByDefname(defname).Create(cont);
			item.Amount = amount;
			return item;
		}

		[SteamFunction]
		public static void TestBankStatusBase(Character self) {
			self.Str = 100;
			Container unused = self.Backpack; //made on first use, and it weighs
			Console.WriteLine("BANK_BASE: weight=" + Stones(self.Weight) + " end");
		}

		/// <summary>Gold on the character and in the bank, then what a full recount says.</summary>
		[SteamFunction]
		public static void TestBankStatusGold(Character self) {
			Add("i_gold", self.Backpack, 1000);
			Add("i_gold", self.Bank, 5000);
			float counted = self.Weight;
			self.FixWeight();
			Console.WriteLine("BANK_GOLD: weight=" + Stones(counted) + " recounted=" + Stones(self.Weight) + " gold=" + self.Gold +
				" load=" + MovementStamina.LoadPercent(MovementStamina.CarriedWeight(self), self.Str) + " end");
		}

		/// <summary>Moving a stack's weight: on creation, on a changed amount, and on the way out.</summary>
		[SteamFunction]
		public static void TestBankStatusStack(Character self) {
			Container pack = self.Backpack; //made on first use, and it weighs
			float before = self.Weight;
			Item picks = Add("i_lockpick", pack, 10);
			float ten = self.Weight - before;
			picks.Amount = 30;
			float thirty = self.Weight - before;
			picks.Delete();
			Console.WriteLine("BANK_STACK: ten=" + Stones(ten) + " thirty=" + Stones(thirty) + " after=" + Stones(self.Weight - before) + " end");
		}

		/// <summary>A pile to split by hand, and a bag of gold duped: both pass through the status
		/// while the new item is half made.</summary>
		[SteamFunction]
		public static void TestBankStatusPile(Character self) {
			Item pile = Add("i_gold", self.Backpack, 100);
			Console.WriteLine("BANK_PILE: uid=" + (uint) pile.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestBankStatusBagDupe(Character self) {
			Container bag = (Container) ThingDef.GetByDefname("i_bag").Create(self.Backpack);
			Add("i_gold", bag, 500);
			bag.Dupe();
			Console.WriteLine("BANK_DUPE: gold=" + self.Gold + " bagAlive=" + !bag.IsDeleted + " end");
		}

		[SteamFunction]
		public static void TestBankStatusCounts(Character self) {
			float counted = self.Weight;
			self.FixWeight();
			Console.WriteLine("BANK_COUNTS: gold=" + self.Gold + " weight=" + Stones(counted) + " recounted=" + Stones(self.Weight) + " end");
		}

		/// <summary>Sphere's "3.0", ".1" and "10", and an item no script gave a weight.</summary>
		[SteamFunction]
		public static void TestBankStatusDefs(Character self) {
			ItemDef bankDef = (ItemDef) ThingDef.GetByDefname("i_bankBox");
			Console.WriteLine("BANK_DEFS: backpack=" + Stones(ThingDef.GetByDefname("i_backpack").Weight) +
				" lockpick=" + Stones(ThingDef.GetByDefname("i_lockpick").Weight) +
				" tiledata=" + Stones(bankDef.Weight) + " wanted=" + bankDef.DispidInfo.Weight + " end");
		}

		[SteamFunction]
		public static void TestBankStatusOpen(Character self) {
			self.Bank.OpenBankTo(self);
		}
	}
}
