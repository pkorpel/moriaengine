using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1017 - server side in scripts/Tests/MagicItemTests.cs.</summary>
	[Collection(MagicItemCollection.Name)]
	public class MagicItemTests {
		private const string TestAccount = "protocoltest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(60);
		private readonly GameServerPairFixture fixture;

		public MagicItemTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void MagicItemsRollIdentifyAndWork() {
			using (TestGameClient game = this.Login("Enchanter")) {
				game.SendSpeech(".TestMagicItems");
				Assert.True(this.fixture.WaitForGameOutput("MAGIC_DONE", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech(".TestMagicCombat");
				Assert.True(this.fixture.WaitForGameOutput("MAGIC_COMBAT_DONE", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech(".TestMagicExchanger");
				Assert.True(this.fixture.WaitForGameOutput("MAGIC_EXCHANGER_READY", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech("dobry den");
				var dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Vetesnik", dialog.Strings);
				Assert.Contains("Identifikovat", dialog.Strings);
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in new[] { "aBagUnpacks", "aPouchHasOneProperty", "unidentifiedCantBeWorn", "identifiedItHasAName", "everyRollMakesAnItem",
						"everyPropertyIsFromItsTable", "aBlackBagIsValoriteOrBetter", "theInfoLines", "wornItWorks", "takenOffItStops", "namedByItsGraphic",
						"theWearerHasItsPlugin", "aSlayerCountsByTheMonstersPercent", "theSlayersGoThroughTheTriggers", "takenOffThePluginGoes", /*#1476*/ "notAgainstAnotherKind", "leechHealsByTheFormula", "freezingParalyses", "aBlockStopsAHarmfulSpell", "aReflexSendsItBack", "aMagicMountGivesItsPowers", "magicClothingWantsAMasterAndMagicThread", "aGmCopiesAMagicItem", "bagsAreCounted",
						"theVetesnikIsAnExchanger", "identifyingCostsAndReveals", "tooFewToExchange", "tenPropertiesBuyABag", "aCrushingPathStaff", "aSharpsPathKryss", "noPathNoMace" }) {
					string marker = "MAGIC_ITEM: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
			}
		}

		/// <summary>#1017 - stalls and fields, on this server too: server side in scripts/Tests/RentedPlaceTests.cs.</summary>
		[Fact]
		public void StallsAndFieldsAreRentedLikeHouses() {
			using (TestGameClient game = this.Login("Merchant")) {
				game.SendSpeech(".TestRentedPlaces");
				Assert.True(this.fixture.WaitForGameOutput("RENTED_DONE", Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in new[] { "kindsByRegion", "rentBySize", "claimed", "theOwnersMenuRank", "locked", "evictionTakesTheLocked", "andLeavesTheRest", "theDogGuards" }) {
					string marker = "RENTED: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
			}
		}

		/// <summary>#1017 - Erebor's player vendor: server side in scripts/Tests/EreborVendorTests.cs, then its buy list by speech.</summary>
		[Fact]
		public void EreborsPlayerVendorTrades() {
			using (TestGameClient game = this.Login("Trader")) {
				game.SendSpeech(".TestEreborVendor");
				Assert.True(this.fixture.WaitForGameOutput("VENDOR_DONE", Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in new[] { "aDeedOnlyInOwnStall", "aDeedPlacesAVendor", "stockedIntoTheBank", "buyingPaysTheVendorNinetyPercent", "unitsSellOneByOne",
						"theVendorBuysBack", "theEarningsGoToTheBank", "theGoodsGoToTheBank", "aStallPlacesItsVendor",
						"aSectionGroupsGoods", "stockedIntoTheSection", "nobodyPicksUpFromAVendor", "boughtFromTheSection", "anEmptySectionGoesBack", "punishmentTakesTheVendorAway", "andBringsItBack" }) {
					string marker = "VENDOR: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
				game.SendSpeech("Kupec nakup");
				var list = game.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(15));
				Assert.Contains("Nazev zbozi", list.Strings);
				Assert.Contains("Klobouk", list.Strings);
			}
		}

		/// <summary>#1070 - the magic item auction: server side in scripts/Tests/AuctionTests.cs, then .aukce's dialogs, where
		/// the seller takes a listing back.</summary>
		[Fact]
		public void TheAuctionSellsMagicItems() {
			using (TestGameClient game = this.Login("Auctioneer")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(".TestAuction");
				Assert.True(this.fixture.WaitForGameOutputSince("AUCTION_READY", mark, Wait), this.fixture.GameOutputSnapshot());
				string ready = this.fixture.GameOutputSnapshot().Substring(mark);
				uint staff = uint.Parse(Between(ready, "AUCTION_READY uid=", " "), System.Globalization.CultureInfo.InvariantCulture);
				uint pack = uint.Parse(Between(ready, " pack=", " "), System.Globalization.CultureInfo.InvariantCulture);
				string name = Between(ready, " name=", " |");
				var none = new System.Collections.Generic.Dictionary<int, string>();

				//the user's way to start one: the list's Pridat predmet, the prices, then the item
				game.SendDoubleClick(pack); //the client must know the pack's contents to target one
				game.SendSpeech(".aukce 100 200"); //parameters are ignored now
				var auction = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Aukce magickych predmetu", auction.Strings);
				Assert.Contains("Pridat predmet", auction.Strings);
				game.SendGumpResponse(auction.GumpUid, 3, none);
				var start = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Nova aukce", start.Strings);
				game.SendGumpResponse(start.GumpUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 10, "5000" }, { 11, "100" } });
				start = game.ReadGumpDialogWithStrings(Wait); //a buyout under the start brings it back
				Assert.Contains("Nova aukce", start.Strings);
				game.SendGumpResponse(start.GumpUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 10, "2.000" }, { 11, "0" } });
				Thread.Sleep(500);
				game.SendTargetUid(staff);
				auction = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(name + " (elven)", auction.Strings); //the user: a weapon's material must show
				Assert.Contains("2.000 (od)", auction.Strings);
				game.SendGumpResponse(auction.GumpUid, 100, none);
				var listing = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Prodava:", listing.Strings);
				Assert.Contains("Material:", listing.Strings);
				Assert.Contains("elven", listing.Strings);
				Assert.Contains("Stahnout", listing.Strings);
				game.SendGumpResponse(listing.GumpUid, 3, new System.Collections.Generic.Dictionary<int, string>());
				Thread.Sleep(500);

				game.SendSpeech(".TestAuctionReport");
				Assert.True(this.fixture.WaitForGameOutputSince("AUCTION_DONE", mark, Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot().Substring(mark);
				foreach (string check in new[] { "aPlainItemIsRefused", "aRingGoesUp", "anotherRealmCantBid", "theSellerCantBid", "tooLowIsRefused", "aBidPaysAtOnce",
						"theNextBidIsFivePercentMore", "theOutbidGetTheirGoldBack", "anOfflineBidderGetsAMessage", "noWithdrawingAfterABid", "theWinnerGetsItInTheBank",
						"theSellerGetsTheBidInTheBank", "aBuyoutEndsItAtOnce", "noBidTheItemComesBack", "theListingsAreLimited", "theSellerWithdrawsWithoutABid",
						"aSwordShowsItsMetal", "aStaffShowsItsWood", "armorShowsItsMetal", "aRingHasNoMaterial", "pricesAreChecked", "theDialogWithdrew" }) {
					string marker = "AUCTION: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
			}
		}

		private static string Between(string text, string before, string after) {
			int at = text.IndexOf(before, StringComparison.Ordinal);
			Assert.True(at >= 0, "no '" + before + "' in: " + text);
			at += before.Length;
			return text.Substring(at, text.IndexOf(after, at, StringComparison.Ordinal) - at);
		}

		private TestGameClient Login(string name) {
			Exception firstFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				int redirectPort = this.RedirectPort();
				var game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(TestAccount, TestAccount);
					game.ReadCharacterList();
					game.SendCreateCharacter(name);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose();
					firstFailure = firstFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as " + name, firstFailure);
		}

		private int RedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestAccount);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MagicItemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MagicItem";
	}
}
