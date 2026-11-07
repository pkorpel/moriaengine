using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1260 - the harbour, the stable, the market, the bakery and the barracks: the harbourmaster's dialog shows the dock
	/// work to a client, which unloads a ship; then every action and errand on the server. Server side: scripts/Tests/TradeVenueTests.cs.</summary>
	[Collection(TradeVenueCollection.Name)]
	public class TradeVenueTests : LivingWorldTestBase {
		public TradeVenueTests(GameServerPairFixture servers)
			: base(servers, "tradevenuetests", "TRADEVENUE") {
		}

		[Fact]
		public void TheCitysTradesHaveWorkAndErrands() {
			using (TestGameClient game = this.Login("Pristavni")) {
				this.Run(game, ".TestTradeVenueSetup", "TRADEVENUE_SETUP_DONE");
				string setup = this.LastLine("TRADEVENUE_SETUP:");

				game.SendDoubleClick((uint) Number(setup, "master"));
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Pomoct s vykladkou", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, Number(setup, "unload"));
				this.Run(game, ".TestTradeVenueAfterClient", "TRADEVENUE_CLIENT_DONE");
				this.AssertChecks("theClientUnloadedTheShip");

				this.Run(game, ".TestTradeVenueRules", "TRADEVENUE_RULES_DONE");
				this.AssertChecks("eachVenueOffersItsWork", "eachKindOfWorkRestsOnItsOwn", "theStallByDayTheSentryByNight", "aStallIsPaidAtTheEndOnceADay",
					"aSentryPaysAndRaisesTheCity", "targetPracticeHitsByArcheryAndRests", "eachErrandOnlyFromItsRole", "aFishOrderWantsThreeFish",
					"theHintNamesTheRightAnswer", "theRightAnswerPaysInFull", "theBreadRoundIsThreeHomesInTwentyMinutes", "aRecruitWalksThePatrol");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TradeVenueCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TradeVenue";
	}
}
