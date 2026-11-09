using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - the woodland realm's stories: a client takes the barrel run from an innkeeper, pushes the three barrels off with
	/// double clicks within the time and reports in the dialog; then the vanishing feast, the spiders and the wine from Rhun on the
	/// server. Server side: scripts/Tests/WoodlandStoryTests.cs.</summary>
	[Collection(WoodlandStoriesCollection.Name)]
	public class WoodlandStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public WoodlandStoryTests(GameServerPairFixture servers)
			: base(servers, "woodlandtests", "WOODLAND") {
		}

		[Fact]
		public void TheWoodlandRealmHasItsStories() {
			using (TestGameClient game = this.Login("Elf z lesa")) {
				this.Run(game, ".TestWoodlandSetup", "WOODLAND_SETUP:");
				string setup = this.LastLine("WOODLAND_SETUP:");
				uint innkeeper = (uint) Number(setup, "innkeeper");

				game.SendDoubleClick(innkeeper);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Beh v sudech", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				int errand = 0;
				for (int i = 0; i < 3; i++) {
					this.Run(game, ".TestWoodlandBesideBarrel", "WOODLAND_BARREL:");
					string barrel = this.LastLine("WOODLAND_BARREL:");
					Assert.True(Number(barrel, "uid") != 0, barrel);
					errand = Number(barrel, "errand");
					game.SendDoubleClick((uint) Number(barrel, "uid"));
					System.Threading.Thread.Sleep(300);
				}

				this.Run(game, ".TestWoodlandBesideInnkeeper", "WOODLAND_INNKEEPER:");
				this.AssertChecks("theBarrelsFloatInTime");
				game.SendDoubleClick(innkeeper);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Sudy jsou na rece.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestWoodlandState \"" + Number(setup, "gold") + "\"", "WOODLAND_STATE_DONE");
				this.AssertChecks("theRunIsReported");

				this.Run(game, ".TestWoodlandChain", "WOODLAND_CHAIN_DONE");
				this.AssertChecks("onlyTheWoodlandRealm", "theFeastsLightsVanish", "theSpidersAreCleared", "theWineIsFetchedFromRhun");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WoodlandStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WoodlandStories";
	}
}
