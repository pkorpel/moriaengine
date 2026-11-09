using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - Dol Amroth's stories: a client takes the corsair spies from the harbourmaster, catches the two with double clicks
	/// and brings the letter in the dialog; then the swan knights' tourney and the lighthouse on the server. Server side:
	/// scripts/Tests/DolAmrothStoryTests.cs.</summary>
	[Collection(DolAmrothStoriesCollection.Name)]
	public class DolAmrothStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public DolAmrothStoryTests(GameServerPairFixture servers)
			: base(servers, "dolamrothtests", "DOLAMROTH") {
		}

		[Fact]
		public void DolAmrothHasItsStories() {
			using (TestGameClient game = this.Login("Labuti rytir")) {
				this.Run(game, ".TestDolAmrothSetup", "DOLAMROTH_SETUP:");
				string setup = this.LastLine("DOLAMROTH_SETUP:");
				uint master = (uint) Number(setup, "master");

				game.SendDoubleClick(master);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Spehove korzaru", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				int errand = 0;
				for (int i = 0; i < 2; i++) {
					this.Run(game, ".TestDolAmrothBesideSpy", "DOLAMROTH_SPY:");
					string spy = this.LastLine("DOLAMROTH_SPY:");
					Assert.True(Number(spy, "uid") != 0, spy);
					errand = Number(spy, "errand");
					game.SendDoubleClick((uint) Number(spy, "uid"));
					System.Threading.Thread.Sleep(300);
				}

				this.Run(game, ".TestDolAmrothBesideMaster", "DOLAMROTH_MASTER:");
				this.AssertChecks("theSpiesAreCaughtWithTheLetter");
				game.SendDoubleClick(master);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Tohle mel jeden z nich u sebe.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestDolAmrothState \"" + Number(setup, "gold") + "\"", "DOLAMROTH_STATE_DONE");
				this.AssertChecks("theLetterIsDelivered");

				this.Run(game, ".TestDolAmrothChain", "DOLAMROTH_CHAIN_DONE");
				this.AssertChecks("onlyDolAmrothsOwn", "theTourneyIsRiddenMountedAndArmed", "theLighthouseIsLitInTheEvening", "aTownsThingsAreInThatTown");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class DolAmrothStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DolAmrothStories";
	}
}
