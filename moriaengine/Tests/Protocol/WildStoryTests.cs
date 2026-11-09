using System.Collections.Generic;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - the wild's stories: a client takes the cave creature's riddles from an innkeeper, double clicks the dark pool, types
	/// the answer into the creature's dialog and reports; then a wrong answer, the Black Rider at the ferry and the singing hermit on
	/// the server. Server side: scripts/Tests/WildStoryTests.cs.</summary>
	[Collection(WildStoriesCollection.Name)]
	public class WildStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100, AnswerButton = 1, AnswerEntry = 1;

		public WildStoryTests(GameServerPairFixture servers)
			: base(servers, "wildtests", "WILD") {
		}

		[Fact]
		public void TheWildHasItsStories() {
			using (TestGameClient game = this.Login("Poutnik divocinou")) {
				this.Run(game, ".TestWildSetup", "WILD_SETUP:");
				string setup = this.LastLine("WILD_SETUP:");
				uint innkeeper = (uint) Number(setup, "innkeeper");

				game.SendDoubleClick(innkeeper);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Hadanky ve tme", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				this.Run(game, ".TestWildBesidePool", "WILD_POOL:");
				string pool = this.LastLine("WILD_POOL:");
				Assert.True(Number(pool, "uid") != 0, pool);
				int errand = Number(pool, "errand");
				game.SendDoubleClick((uint) Number(pool, "uid"));
				var riddle = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Hlas ze tmy", riddle.Strings);
				this.Run(game, ".TestWildRiddleAnswer", "WILD_ANSWER:");
				string answer = this.LastLine("WILD_ANSWER:").Split(' ')[1].Replace('_', ' ');
				game.SendGumpResponse(riddle.GumpUid, AnswerButton, new Dictionary<int, string> { { AnswerEntry, answer } });
				System.Threading.Thread.Sleep(500);

				this.Run(game, ".TestWildBesideInnkeeper", "WILD_INNKEEPER:");
				this.AssertChecks("theRiddleIsWon");
				game.SendDoubleClick(innkeeper);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Vyhral jsem hru hadanek.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestWildState \"" + Number(setup, "gold") + "\"", "WILD_STATE_DONE");
				this.AssertChecks("theGameIsTold");

				this.Run(game, ".TestWildChain", "WILD_CHAIN_DONE");
				this.AssertChecks("whoGivesThem", "aWrongAnswerBringsTheCreature", "theRiderIsLeftOnTheBank", "eachRiderGoesWithItsErrand", "theHermitFindsTheTraveller");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WildStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WildStories";
	}
}
