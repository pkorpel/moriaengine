using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - Minas Tirith's stories: a client takes the Steward's archives from a librarian, searches the three shelves with
	/// double clicks, gets the page from the borrower in the dialog and returns it to the librarian; then the forgotten herb on the
	/// server. Server side: scripts/Tests/MinasTirithStoryTests.cs.</summary>
	[Collection(MinasTirithStoriesCollection.Name)]
	public class MinasTirithStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public MinasTirithStoryTests(GameServerPairFixture servers)
			: base(servers, "tirithtests", "TIRITH") {
		}

		[Fact]
		public void MinasTirithHasItsStories() {
			using (TestGameClient game = this.Login("Pisar Citadely")) {
				this.Run(game, ".TestTirithSetup", "TIRITH_SETUP:");
				string setup = this.LastLine("TIRITH_SETUP:");
				uint librarian = (uint) Number(setup, "librarian");

				game.SendDoubleClick(librarian);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Zahada v archivu", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				int errand = 0;
				for (int i = 0; i < 3; i++) {
					this.Run(game, ".TestTirithBesideShelf", "TIRITH_SHELF:");
					string shelf = this.LastLine("TIRITH_SHELF:");
					Assert.True(Number(shelf, "uid") != 0, shelf);
					Assert.Contains("inArchive=True", shelf);
					errand = Number(shelf, "errand");
					game.SendDoubleClick((uint) Number(shelf, "uid"));
					System.Threading.Thread.Sleep(300);
				}

				//the borrower hands the page back, then the librarian takes it
				string[] lines = { "Mas stranu z kroniky Spravcu?", "Tady je ztracena strana." };
				for (int step = 1; step <= 2; step++) {
					this.Run(game, ".TestTirithBesideStep", "TIRITH_STEP:");
					string at = this.LastLine("TIRITH_STEP:");
					Assert.Equal(step, Number(at, "index"));
					game.SendDoubleClick((uint) Number(at, "uid"));
					talk = game.ReadGumpDialogWithStrings(Wait);
					Assert.Contains(lines[step - 1], talk.Strings);
					game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
					System.Threading.Thread.Sleep(500);
				}
				this.Run(game, ".TestTirithState \"" + Number(setup, "gold") + "\"", "TIRITH_STATE_DONE");
				this.AssertChecks("thePageIsBack");

				this.Run(game, ".TestTirithChain", "TIRITH_CHAIN_DONE");
				this.AssertChecks("onlyMinasTirithsOwn", "theLastShelfNamesTheBorrower", "theForgottenHerbIsFoundAndBrought");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class MinasTirithStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MinasTirithStories";
	}
}
