using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - the stories of Umbar, Harad and Rhun: a client takes the mumak's calf from Harad's caravan master, catches it with
	/// a double click and reports in the dialog; then the corsair raid, the spice caravan and the horse trade on the server. Server side:
	/// scripts/Tests/SouthEastStoryTests.cs.</summary>
	[Collection(SouthEastStoriesCollection.Name)]
	public class SouthEastStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public SouthEastStoryTests(GameServerPairFixture servers)
			: base(servers, "southeasttests", "SOUTHEAST") {
		}

		[Fact]
		public void UmbarHaradAndRhunHaveTheirStories() {
			using (TestGameClient game = this.Login("Honak z Haradu")) {
				this.Run(game, ".TestSouthEastSetup", "SOUTHEAST_SETUP:");
				string setup = this.LastLine("SOUTHEAST_SETUP:");
				Assert.True(setup.Contains("talk=True"), setup);
				uint master = (uint) Number(setup, "master");

				game.SendDoubleClick(master);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Utekle mumaci mlade", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				this.Run(game, ".TestSouthEastBesideCalf", "SOUTHEAST_CALF:");
				string calf = this.LastLine("SOUTHEAST_CALF:");
				Assert.True(Number(calf, "uid") != 0, calf);
				int errand = Number(calf, "errand");
				game.SendDoubleClick((uint) Number(calf, "uid"));
				System.Threading.Thread.Sleep(300);

				this.Run(game, ".TestSouthEastBesideMaster", "SOUTHEAST_MASTER:");
				this.AssertChecks("theCalfIsCaught");
				game.SendDoubleClick(master);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Mlade je zpatky.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestSouthEastState \"" + Number(setup, "gold") + "\"", "SOUTHEAST_STATE_DONE");
				this.AssertChecks("theCalfIsReported");

				this.Run(game, ".TestSouthEastChain", "SOUTHEAST_CHAIN_DONE");
				this.AssertChecks("onlyTheirOwnCities", "theRaidIsPrepared", "theSpicesAreRecovered", "theHorsesAreSold");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class SouthEastStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SouthEastStories";
	}
}
