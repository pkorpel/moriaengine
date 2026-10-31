using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#879, #880 - server-side helpers for Tests/Protocol/BankAndBookTests.cs.</summary>
	public static class BankAndBookTests {
		[SteamFunction]
		public static void TestGiveItemForBank(Character self) {
			Item item = (Item) ThingDef.GetByDefname("i_gem_amethyst").Create(self.Backpack);
			Console.WriteLine("BANK_ITEM: uid=" + (uint) item.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}

		[SteamFunction]
		public static void TestReportBankItems(Character self) {
			int count = 0;
			foreach (AbstractItem item in self.Bank) {
				count++;
			}
			Console.WriteLine("BANK_ITEMS: count=" + count);
		}

		[SteamFunction]
		public static void TestGiveSpellbookForOpening(Character self) {
			Item book = (Item) ThingDef.GetByDefname("i_spellbook").Create(self.Backpack);
			Console.WriteLine("BOOK_ITEM: uid=" + (uint) book.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}
	}
}
