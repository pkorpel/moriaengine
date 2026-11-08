using System.Collections.Generic;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1298 - the games: a client wrestles a tavern patron through the dialogs and answers a scholar's riddle, then the
	/// rest of the games' rules on the server. Server side: scripts/Tests/GameTests.cs.</summary>
	[Collection(GamesCollection.Name)]
	public class GameTests : LivingWorldTestBase {
		private const int FirstBet = 1, AnswerButton = 1, AnswerEntry = 1;

		public GameTests(GameServerPairFixture servers)
			: base(servers, "gametests", "GAMES") {
		}

		[Fact]
		public void TavernBarracksAndLibraryGames() {
			using (TestGameClient game = this.Login("Hrac Pak")) {
				this.Run(game, ".TestGamesSetup", "GAMES_SETUP_DONE");
				string setup = this.LastLine("GAMES_SETUP:");
				uint patron = (uint) Number(setup, "patron"), scholar = (uint) Number(setup, "scholar");
				long gold = Number(setup, "gold");

				//arm wrestling with the patron: his line, the bets, a bout won
				this.Run(game, ".TestGamesArmWins", "GAMES_ROLLS:");
				game.SendDoubleClick(patron);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Dame paku?", talk.Strings);
				Assert.Contains("Hadanku?", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, Number(setup, "arm"));
				var bets = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(bets.Strings, s => s.StartsWith("Paka: ", System.StringComparison.Ordinal));
				game.SendGumpResponse(bets.GumpUid, FirstBet);

				//a riddle from the scholar, answered in the dialog's line
				this.Run(game, ".TestGamesRiddleNext", "GAMES_ROLLS: riddle");
				string before = this.LastLine("GAMES_ROLLS: riddle");
				game.SendDoubleClick(scholar);
				talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, Number(setup, "riddle"));
				var riddle = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(riddle.Strings, s => s.Contains("Ma zuby, a nekouse."));
				game.SendGumpResponse(riddle.GumpUid, AnswerButton, new Dictionary<int, string> { { AnswerEntry, "Hreben" } });

				this.Run(game, ".TestGamesAfterClient \"" + gold + " " + Number(before, "exp") + " " + Number(before, "level") + "\"", "GAMES_CLIENT_DONE");
				this.AssertChecks("theClientWonABout", "theClientSolvedARiddle");

				this.Run(game, ".TestGamesRules", "GAMES_RULES_DONE");
				this.AssertChecks("whoOffersWhat", "armWinLoseDrawAndRest", "armCappedAnHour", "drinkWinLoseDrawOnceAnHour", "aPatronDrinksAgainstYou",
					"archeryWinLoseTieAndRest", "riddleStaysOnlyItIsAnsweredWrongIsToldNextWaits", "riddleRightPaysAndAFriendOnceADay", "riddlesArePlainAscii",
					"aGmDoesNotPlayForMoney");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class GamesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Games";
	}
}
