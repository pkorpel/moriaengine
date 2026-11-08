using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1296 - deeds and titles: the server plays kills, an errand, cities, a troll, a festival and a city's respect
	/// (scripts/Tests/DeedTests.cs), then the client reads the journal's Skutky tab with the counts and titles.</summary>
	[Collection(DeedsCollection.Name)]
	public class DeedTests : LivingWorldTestBase {
		private const int DeedsTab = 34;

		public DeedTests(GameServerPairFixture servers)
			: base(servers, "deedtests", "DEEDS") {
		}

		[Fact]
		public void DeedsCountAndTheJournalShowsTheirTitles() {
			using (TestGameClient game = this.Login("Lovec Skutku")) {
				this.Run(game, ".TestDeedsChain", "DEEDS_CHAIN_DONE");
				this.AssertChecks("slayerCounts", "aTierPaysAndNames", "petsAndWorthlessDontCount", "aGmDoesNoDeeds", "eachTierOnce", "anErrandCounts",
					"citiesCountOnce", "anEventWonCounts", "anEnemyDoesNone", "aFestivalCounts", "respectMakesAFriend");

				game.SendSpeech(".questy");
				var journal = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(journal.GumpUid, DeedsTab);
				journal = game.ReadGumpDialogWithStrings(Wait);
				string texts = string.Join("|", journal.Strings);
				Assert.DoesNotContain("Zatim nemas zadne skutky.", texts);
				int slayer = journal.Strings.IndexOf("Lovec");
				Assert.True(slayer >= 0, texts);
				Assert.Equal("1000/10000", journal.Strings[slayer + 1]); //ten kills and the jump to a thousand
				Assert.Equal("Postrach divociny", journal.Strings[slayer + 2]);
				int explorer = journal.Strings.IndexOf("Cestovatel");
				Assert.True(explorer >= 0, texts);
				Assert.Equal("3/6", journal.Strings[explorer + 1]);
				Assert.Equal("Poutnik", journal.Strings[explorer + 2]);
				int courier = journal.Strings.IndexOf("Posel");
				Assert.True(courier >= 0, texts);
				Assert.Equal("1/5", journal.Strings[courier + 1]);
				Assert.Equal("bez titulu", journal.Strings[courier + 2]);
				Assert.Contains("Vitany host", journal.Strings); //friend of the cities
				game.SendGumpResponse(journal.GumpUid, 0);
			}
		}
	}

	[CollectionDefinition(Name)]
	public class DeedsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Deeds";
	}
}
