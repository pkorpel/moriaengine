using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - a DIY vendor beside the player: a top button "Obchod" leading to a page with an offer selling coal and one
	/// buying it back.</summary>
	public static class DiyVendorTests {
		private static NPC vendor;

		[SteamFunction]
		public static void TestDiyVendorPrepare(Character self) {
			vendor = (NPC) ((NPCDef) ThingDef.GetByDefname("c_diy_vendor")).Create(self.X + 1, self.Y, self.Z, self.M);
			Thing store = DiyVendor.Store(vendor);
			DiyVendor.SetIntro(store, "Vitej u Bohouse");
			DiyVendor.SetButton(store, 0, 1, "Obchod", "Uhli za dobrou cenu");
			DiyVendor.SetOffer(store, 1, 1, "Prodej uhli", DiyVendor.Prodej);
			DiyVendor.SetOffer(store, 1, 2, "Vykup uhli", DiyVendor.Vykup);
			Item coal = (Item) ((ItemDef) ThingDef.GetByDefname("i_coal")).Create(self.Backpack);
			DiyVendor.AddGoods(store, 1, 1, coal);
			DiyVendor.AddGoods(store, 1, 2, coal);
			coal.Delete();
			DiyGoods selling = DiyVendor.Goods(store, 1, 1, 1);
			selling.Price = 10;
			selling.Stacks = true;
			DiyVendor.SetGoods(store, 1, 1, 1, selling);
			DiyGoods buying = DiyVendor.Goods(store, 1, 2, 1);
			buying.Price = 4;
			buying.Current = 5;
			buying.Max = 5;
			DiyVendor.SetGoods(store, 1, 2, 1, buying);
			Console.WriteLine("DIY_READY gold=" + self.Gold + " coal=" + DiyVendor.CountIn(self.Backpack, ThingDef.GetByDefname("i_coal")) + " ");
		}

		[SteamFunction]
		public static void TestDiyVendorBecomeGm(Character self) {
			self.Account.PLevel = Globals.MaximalPlevel;
			Console.WriteLine("DIY_GM plevel=" + self.Account.PLevel);
		}

		[SteamFunction]
		public static void TestDiyVendorReport(Character self) {
			int coal = DiyVendor.CountIn(self.Backpack, ThingDef.GetByDefname("i_coal"));
			Thing store = DiyVendor.Store(vendor);
			//Smazat moves the rest up and lowers the count: Erebor's doubled its last line
			Item one = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_iron")).Create(self.Backpack);
			Item two = (Item) ((ItemDef) ThingDef.GetByDefname("i_bandage")).Create(self.Backpack);
			DiyVendor.AddGoods(store, 3, 1, one);
			DiyVendor.AddGoods(store, 3, 1, two);
			DiyVendor.RemoveGoods(store, 3, 1, 1);
			bool removed = (DiyVendor.GoodsCount(store, 3, 1) == 1) && (DiyVendor.Goods(store, 3, 1, 1).Defname == two.Def.PrettyDefname);
			one.Delete();
			two.Delete();
			DiyVendor.SetButton(store, 0, 2, "Nahore", "");
			bool refused = DiyVendor.SlotTakenAtTop(store, 1, 2) && !DiyVendor.SlotTakenAtTop(store, 0, 2) && !DiyVendor.SlotTakenAtTop(store, 1, 3);
			Console.WriteLine("DIY_REPORT: gold=" + self.Gold + " coal=" + coal + " offer=" + DiyVendor.OfferName(store, 0, 1) +
				" kind=" + DiyVendor.OfferKind(store, 0, 1) + " removed=" + removed + " refused=" + refused + " end");
			vendor.Delete();
		}
	}
}
