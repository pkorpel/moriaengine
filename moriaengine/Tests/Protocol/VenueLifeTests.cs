using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1256 - the bath house, the theatre and the pits: the attendant's dialog shows the bath to a client, which takes one;
	/// then the bath's freshness, the stoker, the plays, the theatre's errands and the pits on the server, and a play running on its
	/// real timer to the end. Server side: scripts/Tests/VenueLifeTests.cs.</summary>
	[Collection(VenueLifeCollection.Name)]
	public class VenueLifeTests : LivingWorldTestBase {
		public VenueLifeTests(GameServerPairFixture servers)
			: base(servers, "venuelifetests", "VENUELIFE") {
		}

		[Fact]
		public void BathsPlaysAndPitFights() {
			using (TestGameClient game = this.Login("Lazebnik")) {
				this.Run(game, ".TestVenueLifeSetup", "VENUELIFE_SETUP_DONE");
				string setup = this.LastLine("VENUELIFE_SETUP:");

				game.SendDoubleClick((uint) Number(setup, "attendant"));
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Vykoupat se", talk.Strings);
				Assert.Contains("Co se povida v pare?", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, Number(setup, "bathe"));
				this.Run(game, ".TestVenueLifeAfterClient", "VENUELIFE_CLIENT_DONE");
				this.AssertChecks("theClientsBathMakesFresh");

				this.Run(game, ".TestVenueLifeRules", "VENUELIFE_RULES_DONE");
				this.AssertChecks("theBathHouseOffers", "aBathCostsAndFreshens", "freshnessShowsOnBoosts", "freshShopsAreCheaper", "freshPeopleAreWarmer",
					"theStokerPaysForLogsUpToADay", "theDirectorTellsTheHourAndTakesExtras", "anExtraNeedsMusicianship", "theForgottenLineWaitsForAPrompt",
					"aWrongPromptGetsLaughs", "theExtraIsPaidAtTheEnd", "theRightPromptPays", "withoutAPromptThePrompterWhispers", "aPlayAtDuskOnce",
					"onlyTheDirectorAsks", "theMissingActorIsATheatreActor", "aFreshErrandWarmsTheGiverMore", "aForgottenPropLiesElsewhere",
					"thePitsHaveTwoFighters", "aWinningBetPaysDouble", "betsAreCappedAndGmsDontBet");

				this.Run(game, ".TestVenueLifeRealPlay", "VENUELIFE_REALPLAY_DONE");
				this.AssertChecks("noPlayYet");
				//the scheduler ticks every 20 s; then an opening, 8 lines, a 2-line wait and the claps at a line a second
				string state = "";
				for (int i = 0; (i < 30) && !state.Contains("ended=1"); i++) {
					Thread.Sleep(TimeSpan.FromSeconds(2));
					this.Run(game, ".TestVenueLifeRealPlayState", "VENUELIFE_REALPLAY_STATE");
					state = this.LastLine("VENUELIFE_PLAY:");
				}
				this.AssertChecks("theSchedulerStartedAPlayThatRanToItsEnd");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class VenueLifeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "VenueLife";
	}
}
