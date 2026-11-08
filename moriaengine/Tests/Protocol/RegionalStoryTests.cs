using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1300 - the regional stories: a client takes the elves' letters from a Hobitin innkeeper, finds the elves' chest at the
	/// Grey Havens and puts them in with a double click; then the hobbit post, the horse thieves and the Citadel helm on the server.
	/// Server side: scripts/Tests/RegionalStoryTests.cs.</summary>
	[Collection(RegionalStoriesCollection.Name)]
	public class RegionalStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1;

		public RegionalStoryTests(GameServerPairFixture servers)
			: base(servers, "storytests", "STORIES") {
		}

		[Fact]
		public void HobbitsRiddersGuardsAndElvesHaveTheirStories() {
			using (TestGameClient game = this.Login("Posel na zapad")) {
				this.Run(game, ".TestStoriesSetup", "STORIES_SETUP:");
				string setup = this.LastLine("STORIES_SETUP:");
				uint innkeeper = (uint) Number(setup, "innkeeper");

				game.SendDoubleClick(innkeeper);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Dopisy elfu", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				this.Run(game, ".TestStoriesBesideChest", "STORIES_CHEST:");
				this.AssertChecks("theChestIsAtTheHavens", "theJournalSaysWhere");
				uint chest = (uint) Number(this.LastLine("STORIES_CHEST:"), "uid");
				Assert.True(chest != 0, this.LastLine("STORIES_CHEST:"));
				game.SendDoubleClick(chest);
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestStoriesState \"" + Number(setup, "gold") + "\"", "STORIES_STATE_DONE");
				this.AssertChecks("theLettersSailed");

				this.Run(game, ".TestStoriesChain", "STORIES_CHAIN_DONE");
				this.AssertChecks("thePostIsThreeLettersTimedInHobitinOnly", "roundsRaiseTheRankAndItsShare", "horseThievesFromTheRightPlacesKilledCaughtReported", "othersKillsReplacedOnlyTwiceTheCount",
					"theHelmIsInTheTavernFoundAndReturned", "theLettersOnlyFromHobitin");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class RegionalStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RegionalStories";
	}
}
