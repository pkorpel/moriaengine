using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - Erebor's player vendor: deed, stock, buy, units, buy-back, purse, stall placement.</summary>
	public static class EreborVendorTests {
		[SteamFunction]
		public static void TestEreborVendor(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			foreach (AbstractItem old in new System.Collections.Generic.List<AbstractItem>(player.Backpack)) {
				old.Delete();
			}
			Item deed = (Item) ((ItemDef) ThingDef.GetByDefname("i_deed_erebor_vendor")).Create(player.Backpack);
			deed.Trigger_DClickFromAfar(player); //the client hasn't opened its pack, so a reach test would refuse
			Report("aDeedOnlyInOwnStall", !deed.IsDeleted);
			Stall stall = (Stall) House.Create((FlaggedRegion) StaticRegion.GetByDefname("a_stanek_13"));
			stall.Claim(player);
			player.P(stall);
			deed.Trigger_DClickFromAfar(player);
			EreborVendor vendor = stall.Vendor;
			Report("aDeedPlacesAVendor", (vendor != null) && deed.IsDeleted && vendor.IsOwner(player) && vendor.Flag_Invulnerable);
			if (vendor == null) {
				Console.WriteLine("VENDOR_DONE");
				return;
			}
			vendor.Name = "Kupec";
			EreborVendors.StartSession(vendor, player);
			Item shirt = (Item) ((ItemDef) ThingDef.GetByDefname("i_shirt_plain")).Create(player.Backpack);
			Item entry = EreborVendors.StockItem(vendor, player, shirt, "Pekna kosile", 100);
			Report("stockedIntoTheBank", (entry != null) && (shirt.TopObj() == vendor) && (vendor.Entries.Count == 1) && (EreborVendor.Price(entry) == 100));
			((Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(player.Backpack)).Amount = 1000;
			EreborVendors.Buy(vendor, player, entry, 1);
			Report("buyingPaysTheVendorNinetyPercent", (shirt.TopObj() == player) && (player.Gold == 900) && (vendor.Gold == 90) && (vendor.Entries.Count == 0));

			Container quiver = (Container) ((ItemDef) ThingDef.GetByDefname("i_backpack")).Create(player.Backpack);
			((Item) ((ItemDef) ThingDef.GetByDefname("i_arrow")).Create(quiver)).Amount = 10;
			Item units = EreborVendors.StockUnits(vendor, player, quiver, "Sipy", 5);
			EreborVendors.Buy(vendor, player, units, 4);
			Report("unitsSellOneByOne", (EreborVendor.Units(units) == 6) && (player.Gold == 880) && (vendor.Gold == 108));

			EreborVendors.SetBuyBack(vendor, shirt.Def, 50, 2);
			vendor.Gold = 200;
			bool offered = EreborVendors.Offers(vendor, player).Exists(o => o.Key == shirt);
			bool sold = EreborVendors.Sell(vendor, player, shirt, 50);
			EreborVendors.BuyBackOf(vendor, shirt.Def, out int price, out int limit);
			Report("theVendorBuysBack", offered && sold && (shirt.TopObj() == vendor) && (vendor.Gold == 145) && (player.Gold == 930) && (limit == 1));
			EreborVendors.PayOut(vendor, player);
			Report("theEarningsGoToTheBank", (vendor.Gold == 0) && (player.Bank.Count > 0));
			EreborVendors.Collect(vendor, player);
			Report("theGoodsGoToTheBank", (vendor.Goods.Count == 0) && (shirt.TopObj() == player));

			stall.SetVendorSpot(player, stall);
			stall.PlaceVendor(player);
			Report("aStallPlacesItsVendor", (stall.Vendor == vendor) && (vendor.X == stall.X) && (vendor.Y == stall.Y));

			Item hat = (Item) ((ItemDef) ThingDef.GetByDefname("i_hat_floppy")).Create(player.Backpack);
			EreborVendors.StockItem(vendor, player, hat, "Klobouk", 10);
			EreborVendors.EndSession(player);
			vendor.P(player.X + 1, player.Y, player.Z, player.M);
			Console.WriteLine("VENDOR_DONE");
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("VENDOR: " + name + "=" + ok);
		}
	}
}
