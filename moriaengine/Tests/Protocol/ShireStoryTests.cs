using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - the Shire's stories: a client takes the mushroom thieves from a Hobitin farmer, catches the three outside the
	/// town with double clicks and tells the farmer it let them off (half the pay and the mushrooms); then the birthday party and the
	/// silver spoons on the server. Server side: scripts/Tests/ShireStoryTests.cs.</summary>
	[Collection(ShireStoriesCollection.Name)]
	public class ShireStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100, ShareChoice = 1;

		public ShireStoryTests(GameServerPairFixture servers)
			: base(servers, "shiretests", "SHIRE") {
		}

		[Fact]
		public void TheShireHasItsStories() {
			using (TestGameClient game = this.Login("Hobit z Kraje")) {
				this.Run(game, ".TestShireSetup", "SHIRE_SETUP:");
				string setup = this.LastLine("SHIRE_SETUP:");
				uint farmer = (uint) Number(setup, "farmer");

				game.SendDoubleClick(farmer);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Zlodeji hub", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				int errand = 0;
				for (int i = 0; i < 3; i++) {
					this.Run(game, ".TestShireBesideThief", "SHIRE_THIEF:");
					string thief = this.LastLine("SHIRE_THIEF:");
					Assert.True(Number(thief, "uid") != 0, thief);
					errand = Number(thief, "errand");
					game.SendDoubleClick((uint) Number(thief, "uid"));
					System.Threading.Thread.Sleep(300);
				}

				this.Run(game, ".TestShireBesideFarmer", "SHIRE_FARMER:");
				this.AssertChecks("theThievesAreCaught");
				game.SendDoubleClick(farmer);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Pustil jsem je. Podelili jsme se o houby.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10) + ShareChoice);
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestShireState \"" + Number(setup, "gold") + "\"", "SHIRE_STATE_DONE");
				this.AssertChecks("letOffForTheMushrooms");

				this.Run(game, ".TestShireChain", "SHIRE_CHAIN_DONE");
				this.AssertChecks("onlyInHobitin", "turnedInPaysInFull", "letOffPaysHalf", "theBirthdayPartyIsPreparedAndHeld", "everyoneNearIsAGuestOnceADay", "theSpoonsAreTakenBack");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ShireStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ShireStories";
	}
}
