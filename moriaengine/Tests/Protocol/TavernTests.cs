using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1254 - the tavern: the innkeeper's dialog shows the tavern's lines to a client, which asks for a rumour and takes a
	/// room through it; then who offers what, rumours, the room and its login booster, dice and the bard on the server, and the
	/// bard's song going on line by line. Server side: scripts/Tests/TavernTests.cs.</summary>
	[Collection(TavernCollection.Name)]
	public class TavernTests : LivingWorldTestBase {
		public TavernTests(GameServerPairFixture servers)
			: base(servers, "taverntests", "TAVERN") {
		}

		[Fact]
		public void TheTavernsPeopleOfferItsLife() {
			using (TestGameClient game = this.Login("Hospodsky")) {
				this.Run(game, ".TestTavernSetup", "TAVERN_SETUP_DONE");
				this.AssertChecks("fiveActionsAreRegistered");
				string setup = this.LastLine("TAVERN_SETUP:");
				uint innkeeper = (uint) Number(setup, "innkeeper");

				game.SendDoubleClick(innkeeper);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Co je noveho?", talk.Strings);
				Assert.Contains("Pokoj na noc", talk.Strings);
				Assert.Contains("Zahrajeme kostky?", talk.Strings);
				Assert.DoesNotContain("Zahraj neco", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, Number(setup, "rumour"));

				game.SendDoubleClick(innkeeper);
				var again = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(again.GumpUid, Number(setup, "room"));
				this.Run(game, ".TestTavernAfterClient", "TAVERN_CLIENT_DONE");
				this.AssertChecks("theInnkeeperToldARumour", "theRoomIsTaken");

				this.Run(game, ".TestTavernRules", "TAVERN_RULES_DONE");
				this.AssertChecks("theInnkeeperOffersRumoursARoomAndDice", "aPatronHasNoRooms", "theBardSingsInTheEvening", "outsideTheTavernNothing",
					"rumoursTellOfOthers", "rumoursArePlainAscii", "aRumourAMinute", "aRoomCostsItsPrice", "notTwiceBeforeSleeping", "theNextLoginGivesABooster",
					"onlyOnce", "aRoomOnceARealDay", "diceWinLoseTie", "gamesAreCappedAnHour", "aGmDoesNotGamble", "theBardSings", "aTipMakesAFriendOnceADay");

				Thread.Sleep(TimeSpan.FromSeconds(6)); //a line every 4 s: the second has come, the shortest song (4 lines) hasn't ended
				this.Run(game, ".TestTavernSongLater", "TAVERN_SONG_DONE");
				this.AssertChecks("theSongGoesOnLineByLine");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TavernCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Tavern";
	}
}
