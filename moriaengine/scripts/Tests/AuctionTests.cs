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

			//the user, in a real client: a magic weapon's or armor's name doesn't say its material, the auction must
			Item sword = MagicItems.MagicItem(player, new ScriptArgs("i_sword_viking_mithril 1 5"));
			Item staff = MagicItems.MagicItem(player, new ScriptArgs("i_staff_quarter_copper 1 5"));
			Item arms = MagicItems.MagicItem(player, new ScriptArgs("i_bone_arms_mithril 1 5"));
			Report("aSwordShowsItsMetal", (AuctionHouse.MaterialOf(sword) == "mithril") && (AuctionHouse.ShownName(sword) == sword.Name + " (mithril)"));
			Report("aStaffShowsItsWood", AuctionHouse.MaterialOf(staff) == "spruce");
			Report("armorShowsItsMetal", AuctionHouse.MaterialOf(arms) == "mithril");
			Report("aRingHasNoMaterial", (AuctionHouse.MaterialOf(plain) == null) && (AuctionHouse.ShownName(plain) == plain.Name));
			foreach (Item made in new[] { sword, staff, arms }) {
				made.Delete();
			}

			foreach (Player bidder in new[] { first, second, mordor }) {
				bidder.Delete();
			}
			shown = MagicItems.MagicItem(player, new ScriptArgs("i_staff_quarter_mithril 1 5"));
			AuctionHouse.List(player, shown, 2000, 0);
			Console.WriteLine("AUCTION_READY " + shown.Name + " |");
		}

		private static readonly TagKey restartTK = TagKey.Acquire("_auction_restart_");

		/// <summary>Before the restart: ring A with a bid, ring B with none, both running.</summary>
		[SteamFunction]
		public static void TestAuctionRestartPrepare(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Realm = Realm.Gondor;
			Player bidder = Bidder(player, Realm.Gondor, 20000);
			bidder.Name = "Auction Bidder";
			Item a = Ring(player), b = Ring(player);
			a.SetTag(restartTK, "A");
			b.SetTag(restartTK, "B");
			AuctionHouse.List(player, a, 1000, 9000);
			AuctionHouse.List(player, b, 100, 0);
			AuctionHouse.PlaceBid(bidder, a, 1500);
			Console.WriteLine("AUC_RESTART_READY listedA=" + AuctionHouse.IsListed(a) + " listedB=" + AuctionHouse.IsListed(b) + " bid=" + AuctionHouse.Bid(a) +
				" bidderGold=" + bidder.Gold + " ends=" + AuctionHouse.Ends(a).Ticks + " end");
		}

		private static Item Restarted(Container box, string which) {
			if (box != null) {
				foreach (AbstractItem thing in box) {
					if ((thing is Item item) && Equals(item.GetTag(restartTK), which)) {
						return item;
					}
				}
			}
			return null;
		}

		/// <summary>After the restart, before anything touches AuctionHouse.Box: the box, its own sweep timer, both listings as
		/// they were; then both listings end, for the restored timer to settle.</summary>
		[SteamFunction]
		public static void TestAuctionRestartCheck(Character self) {
			Container box = AuctionHouse.FindSavedBox();
			bool timer = (box != null) && (box.GetTimer(AuctionSweepTimer.Key) != null);
			Item a = Restarted(box, "A"), b = Restarted(box, "B");
			Character seller = (a == null) ? null : AuctionHouse.Seller(a), bidder = (a == null) ? null : AuctionHouse.Bidder(a);
			Console.WriteLine("AUC_RESTART_CHECK box=" + (box != null) + " timer=" + timer + " listedA=" + AuctionHouse.IsListed(a) + " listedB=" + AuctionHouse.IsListed(b) +
				" magic=" + ((a != null) && MagicItems.IsMagic(a)) + " seller=" + seller?.Name?.Replace(' ', '_') + " bidder=" + bidder?.Name?.Replace(' ', '_') +
				" realm=" + ((a == null) ? "" : AuctionHouse.RealmOf(a).ToString()) + " start=" + ((a == null) ? 0 : AuctionHouse.Start(a)) +
				" buyout=" + ((a == null) ? 0 : AuctionHouse.Buyout(a)) + " bid=" + ((a == null) ? 0 : AuctionHouse.Bid(a)) + " bidderGold=" + (bidder?.Gold ?? -1) +
				" ends=" + ((a == null) ? 0 : AuctionHouse.Ends(a).Ticks) + " end");
			if ((a != null) && (b != null)) {
				AuctionHouse.EndNow(a);
				AuctionHouse.EndNow(b);
			}
		}

		/// <summary>Whether the restored sweep has settled both: A to the bidder's bank, its 1500 to the seller's, B back.</summary>
		[SteamFunction]
		public static void TestAuctionRestartSettled(Character self) {
			Item a = null, b = null;
			foreach (Thing thing in Thing.AllThings) {
				if (thing is Item item) {
					if (Equals(item.GetTag(restartTK), "A")) {
						a = item;
					} else if (Equals(item.GetTag(restartTK), "B")) {
						b = item;
					}
				}
			}
			Character aOwner = a?.TopObj() as Character, bOwner = b?.TopObj() as Character;
			bool settled = (a != null) && (b != null) && !AuctionHouse.IsListed(a) && !AuctionHouse.IsListed(b);
			Console.WriteLine("AUC_RESTART_SETTLED settled=" + settled + " aTo=" + aOwner?.Name?.Replace(' ', '_') + " aInBank=" + ((aOwner != null) && (a.Cont == aOwner.Bank)) +
				" bTo=" + bOwner?.Name?.Replace(' ', '_') + " bInBank=" + ((bOwner != null) && (b.Cont == bOwner.Bank)) +
				" sellerBankGold=" + ((bOwner == null) ? -1 : GoldUtils.CountGoldIn(bOwner.Bank)) + " end");
		}

		[SteamFunction]
		public static void TestAuctionReport(Character self) {
			Report("theDialogWithdrew", !AuctionHouse.IsListed(shown) && (shown.Cont == ((Player) self).Bank));
			Console.WriteLine("AUCTION_DONE");
		}
	}
}
