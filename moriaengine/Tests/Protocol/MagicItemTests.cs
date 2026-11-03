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
						"aSlayerCountsByTheMonstersPercent", "notAgainstAnotherKind", "leechHealsByTheFormula", "freezingParalyses", "aBlockStopsAHarmfulSpell", "aReflexSendsItBack",
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
						"theVendorBuysBack", "theEarningsGoToTheBank", "theGoodsGoToTheBank", "aStallPlacesItsVendor" }) {
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
