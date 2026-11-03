using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - the magic item auction: listing rules, bids, outbids, buyout, withdrawal, the end, realms; the bidders
	/// are players with no client, so their news waits as messages.</summary>
	public static class AuctionTests {
		private static Item shown;

		private static void Report(string name, bool ok) {
			Console.WriteLine("AUCTION: " + name + "=" + ok);
		}

		private static Player Bidder(Player near, Realm realm, int gold) {
			Player bidder = (Player) ThingDef.FindCharDef(0x190).Create(near.X + 2, near.Y, near.Z, near.M);
			bidder.Realm = realm;
			((Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(bidder.Backpack)).Amount = gold;
			return bidder;
		}

		private static Item Ring(Player owner) {
			return MagicItems.MagicItem(owner, new ScriptArgs("i_ring_gold 1 5"));
		}

		[SteamFunction]
		public static void TestAuction(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Realm = Realm.Gondor;
			foreach (AbstractItem old in new List<AbstractItem>(player.Backpack)) {
				old.Delete();
			}
			((Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(player.Backpack)).Amount = 100000;
			long sellerGold = player.Gold;

			Item plain = (Item) ((ItemDef) ThingDef.GetByDefname("i_ring_silver")).Create(player.Backpack);
			Report("aPlainItemIsRefused", AuctionHouse.List(player, plain, 100, 0) != null);
			Item ring = Ring(player);
			Report("aRingGoesUp", (AuctionHouse.List(player, ring, 1000, 0) == null) && AuctionHouse.IsListed(ring) && (ring.TopObj() != player));

			Player first = Bidder(player, Realm.Gondor, 20000), second = Bidder(player, Realm.Gondor, 20000), mordor = Bidder(player, Realm.Mordor, 20000);
			AuctionHouse.PlaceBid(mordor, ring, 2000);
			Report("anotherRealmCantBid", (AuctionHouse.Bidder(ring) == null) && (mordor.Gold == 20000) && !AuctionHouse.Listings(mordor).Contains(ring));
			AuctionHouse.PlaceBid(player, ring, 2000);
			Report("theSellerCantBid", AuctionHouse.Bidder(ring) == null);
			AuctionHouse.PlaceBid(first, ring, 500);
			Report("tooLowIsRefused", (AuctionHouse.Bidder(ring) == null) && (first.Gold == 20000));
			AuctionHouse.PlaceBid(first, ring, 1000);
			Report("aBidPaysAtOnce", (AuctionHouse.Bidder(ring) == first) && (AuctionHouse.Bid(ring) == 1000) && (first.Gold == 19000));
			Report("theNextBidIsFivePercentMore", AuctionHouse.MinBid(ring) == 1050);
			AuctionHouse.PlaceBid(second, ring, 1050);
			Report("theOutbidGetTheirGoldBack", (AuctionHouse.Bidder(ring) == second) && (first.Gold == 20000) && (second.Gold == 18950));
			Report("anOfflineBidderGetsAMessage", MsgsBoard.CountUnread(first) > 0);
			AuctionHouse.Withdraw(player, ring);
			Report("noWithdrawingAfterABid", AuctionHouse.IsListed(ring));
			AuctionHouse.EndNow(ring);
			AuctionHouse.Sweep();
			Report("theWinnerGetsItInTheBank", !AuctionHouse.IsListed(ring) && (ring.Cont == second.Bank) && (AuctionHouse.Seller(ring) == null));
			Report("theSellerGetsTheBidInTheBank", player.Gold == sellerGold + 1050);

			Item bought = Ring(player);
			AuctionHouse.List(player, bought, 100, 3000);
			AuctionHouse.PlaceBid(first, bought, 200);
			AuctionHouse.BuyOut(second, bought);
			Report("aBuyoutEndsItAtOnce", (bought.Cont == second.Bank) && (second.Gold == 18950 - 3000) && (first.Gold == 20000) && (player.Gold == sellerGold + 1050 + 3000));

			Item unsold = Ring(player);
			AuctionHouse.List(player, unsold, 100, 0);
			AuctionHouse.EndNow(unsold);
			AuctionHouse.Sweep();
			Report("noBidTheItemComesBack", !AuctionHouse.IsListed(unsold) && (unsold.Cont == player.Bank));

			int maxListings = AuctionSettings.instance.maxListings;
			AuctionSettings.instance.maxListings = 1;
			Item one = Ring(player), two = Ring(player);
			Report("theListingsAreLimited", (AuctionHouse.List(player, one, 100, 0) == null) && (AuctionHouse.List(player, two, 100, 0) != null));
			AuctionSettings.instance.maxListings = maxListings;
			AuctionHouse.Withdraw(player, one);
			Report("theSellerWithdrawsWithoutABid", !AuctionHouse.IsListed(one) && (one.Cont == player.Bank));

			foreach (Player bidder in new[] { first, second, mordor }) {
				bidder.Delete();
			}
			shown = Ring(player);
			AuctionHouse.List(player, shown, 2000, 0);
			Console.WriteLine("AUCTION_READY " + shown.Name + " |");
		}

		[SteamFunction]
		public static void TestAuctionReport(Character self) {
			Report("theDialogWithdrew", !AuctionHouse.IsListed(shown) && (shown.Cont == ((Player) self).Bank));
			Console.WriteLine("AUCTION_DONE");
		}
	}
}
