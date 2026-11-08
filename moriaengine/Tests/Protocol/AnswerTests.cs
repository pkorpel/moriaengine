using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1307 - the user's answers to #1237's saved questions: citizenship wants Vazeny, three captain's orders a day, the
	/// pilot cities' factions by workplace. Server side: scripts/Tests/AnswerTests.cs. (The quest answers are in the quests' own
	/// tests: Q007, Q017, Q041Q099.)</summary>
	[Collection(AnswersCollection.Name)]
	public class AnswerTests : LivingWorldTestBase {
		public AnswerTests(GameServerPairFixture servers)
			: base(servers, "answertests", "ANSWERS") {
		}

		[Fact]
		public void TheUsersAnswersHold() {
			using (TestGameClient game = this.Login("Odpovedi")) {
				this.Run(game, ".TestAnswersChain", "ANSWERS_CHAIN_DONE");
				this.AssertChecks("citizenshipWantsVazeny", "threeCaptainOrdersADay", "pilotFactionsByWorkplace");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class AnswersCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Answers";
	}
}
