using System;
using System.Linq;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1250 - the errand engine: a player double clicks a resident, asks "Potrebujes neco?", takes the offer, finds it in
	/// the journal's Ukoly tab, and hands the letter to the second resident in that one's dialog; then every step kind, the pay
	/// and the limits on the server. Server side: scripts/Tests/ErrandTests.cs (a test template stands in for step 7's).</summary>
	[Collection(ErrandCollection.Name)]
	public class ErrandTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100, ErrandsSide = 31;

		public ErrandTests(GameServerPairFixture servers)
			: base(servers, "errandtests", "ERRAND") {
		}

		[Fact]
		public void ResidentsGiveErrandsAndPayForThem() {
			using (TestGameClient game = this.Login("Pomocnik")) {
				this.Run(game, ".TestErrandSetup", "ERRAND_SETUP_DONE");
				this.AssertChecks("aGiverCanOffer");
				string setup = this.LastLine("ERRAND_SETUP:");
				uint giver = (uint) Number(setup, "giver");
				uint other = (uint) Number(setup, "other");
				string otherName = setup.Substring(setup.IndexOf("othername=", StringComparison.Ordinal) + 10).Trim();

				//the giver's dialog, the question, the offer, taken
				game.SendDoubleClick(giver);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Potrebujes neco?", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Zkusebni ukol", offer.Strings);
				Assert.Contains(offer.Strings, s => s.Contains(otherName, StringComparison.Ordinal));
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				//the journal's Ukoly tab shows it, with whom to see
				game.SendSpeech(".questy");
				var journal = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(journal.GumpUid, ErrandsSide);
				journal = game.ReadGumpDialogWithStrings(Wait);
				string texts = string.Join("|", journal.Strings);
				Assert.Contains("Zkusebni ukol (1/4)", texts);
				Assert.Contains("Odnes dopis: " + otherName, texts);
				game.SendGumpResponse(journal.GumpUid, 0);

				//the second resident's dialog has the letter's line, once the player stands beside them
				this.Run(game, ".TestErrandGoTo \"other\"", "ERRAND_WENT:");
				game.SendDoubleClick(other);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(talk.Strings, s => s.StartsWith("Nesu ti dopis od ", StringComparison.Ordinal));
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + 10); //the first errand taken is number 1
				this.Run(game, ".TestErrandAfterClient", "ERRAND_AFTER_DONE");
				this.AssertChecks("theClientTookIt", "theLetterWasDelivered");

				this.Run(game, ".TestErrandRules", "ERRAND_RULES_DONE");
				this.AssertChecks("theGiverGivesNoSecond", "theWrongResidentCant", "noApplesNoStep", "twoApplesTaken", "notInTheMorning",
					"notFromAfar", "butInTheEvening", "theAnswerIsKept", "itsDoneAndPaid", "oneADayFromAGiver", "aNewDayANewOne", "theSameAgainPaysLess",
					"theLetterIsInThePack", "givingUpTakesTheLetterBack", "anEnemyOfTheCityGetsNone", "errandsAreNumbered", "anErrandRunsOut", "aCapOnTakenErrands");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ErrandCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Errand";
	}
}
