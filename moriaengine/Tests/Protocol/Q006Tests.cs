using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1264 - q006 Erioluv vnuk: a client greets Eriol, gets his dialog and accepts the torches; the server then walks the
	/// rest of the chain through the dialogs' buttons. Server side: scripts/Tests/Q006Tests.cs.</summary>
	[Collection(Q006Collection.Name)]
	public class Q006Tests : LivingWorldTestBase {
		public Q006Tests(GameServerPairFixture servers)
			: base(servers, "q006tests", "Q006") {
		}

		[Fact]
		public void EriolsGrandson() {
			using (TestGameClient game = this.Login("Eriolova Pomocnice")) {
				this.Run(game, ".TestQ006Setup", "Q006_SETUP_DONE");

				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Eriol"));
				Assert.Contains(gump.Strings, s => s.Contains("Pokracuj"));
				Assert.Contains(gump.Strings, s => s.Contains("Prinesu je"));
				this.Run(game, ".TestQ006Button \"gq006_acceptSubquest_1\"", "Q006_BUTTON:");
				string line = this.LastLine("Q006_BUTTON:");
				int id = int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture);
				game.SendGumpResponse(gump.GumpUid, id);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stage=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ006State", "Q006_STATE:");
					state = this.LastLine("Q006_STATE:");
				}
				Assert.Contains("stage=1", state);

				this.Run(game, ".TestQ006Chain", "Q006_CHAIN_DONE");
				this.AssertChecks("theClientAcceptedTheTorches", "theJournalListsIt", "tenTorchesAndNoFewer", "aShrunkDogOnly", "threeLesserThenTenGreaterCures",
					"aFullBagOfThighs", "qworetKnowsNothing", "eriolSendsToUnas", "unasWantsLevelFifteen", "unasFourBagsThenTheParchment",
					"theInnkeeperTakesTheParchment", "eriolAsksForTheIngredients", "withoutAlchemyItEndsWithoutTheRobe", "atAlchemyHundredEriolsIngredientsMakeThePotion",
					"thePotionForTheRobe", "theRobeGivesItsOwnerTen", "aNewRobeForTwoMillion", "journalTextsArePlain", "theQuestBagsCantBeLost",
					"anAlchemistMixesItAtEriols", "thePotionIsNotBrewable");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class Q006Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q006";
	}
}
