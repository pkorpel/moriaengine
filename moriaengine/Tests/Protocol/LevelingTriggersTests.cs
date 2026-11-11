using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1416 - @killCredit and @gainExperience. Server side: scripts/Tests/LevelingTriggersTests.cs. Shares the
	/// movement holds' server and account.</summary>
	[Collection(MovementHoldsCollection.Name)]
	public class LevelingTriggersTests : LivingWorldTestBase {
		public LevelingTriggersTests(GameServerPairFixture servers)
			: base(servers, "movementholds", "KILLCREDIT") {
		}

		[Fact]
		public void KillCreditAndExperienceAreTriggers() {
			using (TestGameClient game = this.Login("Lovcik")) {
				this.Run(game, ".TestLevelingTriggers", "KILLCREDIT_DONE");
				this.AssertChecks("monsterKillCredit", "gainExperienceChangesIt", "gainExperienceCancelled", "worthlessKillCredit",
					"playerKillCredit", "immuneDeathCredit");
			}
		}
	}
}
