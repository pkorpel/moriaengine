using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - Edoras's stories: a client on horseback takes the eored's muster from a stable hand, rides to the three banners
	/// with double clicks and reports in the dialog; then the muster on foot and the song for the hall on the server. Server side:
	/// scripts/Tests/EdorasStoryTests.cs.</summary>
	[Collection(EdorasStoriesCollection.Name)]
	public class EdorasStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public EdorasStoryTests(GameServerPairFixture servers)
			: base(servers, "edorastests", "EDORAS") {
		}

		[Fact]
		public void EdorasHasItsStories() {
			using (TestGameClient game = this.Login("Jezdec Rohanu")) {
				this.Run(game, ".TestEdorasSetup", "EDORAS_SETUP:");
				string setup = this.LastLine("EDORAS_SETUP:");
				Assert.Contains("riding=True", setup);
				uint hand = (uint) Number(setup, "hand");

				game.SendDoubleClick(hand);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Sraz eoredu", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				int errand = 0;
				for (int i = 0; i < 3; i++) {
					this.Run(game, ".TestEdorasBesideBanner", "EDORAS_BANNER:");
					string banner = this.LastLine("EDORAS_BANNER:");
					Assert.True(Number(banner, "uid") != 0, banner);
					errand = Number(banner, "errand");
					game.SendDoubleClick((uint) Number(banner, "uid"));
					System.Threading.Thread.Sleep(300);
				}

				this.Run(game, ".TestEdorasBesideHand", "EDORAS_HAND:");
				this.AssertChecks("theBannersAreRidden");
				game.SendDoubleClick(hand);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Vsechny korouhve vedi.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestEdorasState \"" + Number(setup, "gold") + "\"", "EDORAS_STATE_DONE");
				this.AssertChecks("theMusterIsReported");

				this.Run(game, ".TestEdorasChain", "EDORAS_CHAIN_DONE");
				this.AssertChecks("onlyEdoras", "onFootABannerDoesntCount", "theSongNeedsMusicAndAnInstrument", "theSongIsPlayedInTheHall");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class EdorasStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "EdorasStories";
	}
}
