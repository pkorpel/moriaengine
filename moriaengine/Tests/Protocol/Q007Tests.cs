using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1266 - q007: a client greets Haradir, gets his dialog and promises the piglet; the server then walks the
	/// rest of the chain through the dialogs' buttons. Server side: scripts/Tests/Q007Tests.cs.</summary>
	[Collection(Q007Collection.Name)]
	public class Q007Tests : LivingWorldTestBase {
		public Q007Tests(GameServerPairFixture servers)
			: base(servers, "q007tests", "Q007") {
		}

		[Fact]
		public void HaradirsPiglet() {
			using (TestGameClient game = this.Login("Haradiruv Posel")) {
				this.Run(game, ".TestQ007Setup", "Q007_SETUP_DONE");

				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Haradir"));
				Assert.Contains(gump.Strings, s => s.Contains("Mas me slovo, ze se za tim hospodskym vypravim."));
				this.Run(game, ".TestQ007Button \"haradir:1\"", "Q007_BUTTON:");
				string line = this.LastLine("Q007_BUTTON:");
				int id = int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture);
				game.SendGumpResponse(gump.GumpUid, id);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stage=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ007State", "Q007_STATE:");
					state = this.LastLine("Q007_STATE:");
				}
				Assert.Contains("stage=1", state);

				this.Run(game, ".TestQ007Chain", "Q007_CHAIN_DONE");
				this.AssertChecks("theClientPromisedThePiglet", "theJournalListsAllThree", "deniedThenSentToRachel", "rachelWantsAThousandBatWings",
					"theBowForThePiglet", "thePigletToHaradir", "theInnkeepersDrinks", "bothSendForTheStones", "theChestGivesTheStonesOncePiratesAreDead",
					"theStonesForTheHammer", "theHammerGivesTen", "theStonesForTheBook", "theBookForThePen", "thePenMakesARecallRune",
					"rodrigoSellsRumoursAndDolphins", "eachNpcOpensItsOwnDialog", "journalTextsArePlain");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class Q007Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q007";
	}
}
