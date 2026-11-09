using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - Fornost's stories: a client takes the Rangers' watch from a Fornost guard, reads the three tracks with double
	/// clicks and reports the right way; then the barrow at night and the watch's rules on the server. Server side:
	/// scripts/Tests/FornostStoryTests.cs.</summary>
	[Collection(FornostStoriesCollection.Name)]
	public class FornostStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public FornostStoryTests(GameServerPairFixture servers)
			: base(servers, "fornosttests", "FORNOST") {
		}

		[Fact]
		public void FornostHasItsStories() {
			using (TestGameClient game = this.Login("Hranicar severu")) {
				this.Run(game, ".TestFornostSetup", "FORNOST_SETUP:");
				string setup = this.LastLine("FORNOST_SETUP:");
				uint guard = (uint) Number(setup, "guard");

				game.SendDoubleClick(guard);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Hlidka hranicaru", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				int errand = 0, right = 0;
				for (int i = 0; i < 3; i++) {
					this.Run(game, ".TestFornostBesideTrack", "FORNOST_TRACK:");
					string track = this.LastLine("FORNOST_TRACK:");
					Assert.True(Number(track, "uid") != 0, track);
					errand = Number(track, "errand");
					right = Number(track, "right");
					game.SendDoubleClick((uint) Number(track, "uid"));
					System.Threading.Thread.Sleep(300);
				}

				this.Run(game, ".TestFornostBesideGuard", "FORNOST_GUARD:");
				this.AssertChecks("theTracksAreRead");
				game.SendDoubleClick(guard);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Sli na sever.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10) + right);
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestFornostState \"" + Number(setup, "gold") + "\"", "FORNOST_STATE_DONE");
				this.AssertChecks("theRightWayPays");

				this.Run(game, ".TestFornostChain", "FORNOST_CHAIN_DONE");
				this.AssertChecks("onlyFornostsRangers", "theBarrowWakesAtNightAndIsLaid", "readingNeedsTracking", "theWrongWayPaysNothing");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class FornostStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "FornostStories";
	}
}
