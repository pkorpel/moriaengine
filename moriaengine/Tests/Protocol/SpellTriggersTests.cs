using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1419 - @spellPower and @spellSuccess. Server side: scripts/Tests/SpellTriggersTests.cs. Shares the
	/// movement holds' server and account.</summary>
	[Collection(MovementHoldsCollection.Name)]
	public class SpellTriggersTests : LivingWorldTestBase {
		public SpellTriggersTests(GameServerPairFixture servers)
			: base(servers, "movementholds", "SPELLTRIG") {
		}

		[Fact]
		public void SpellPowerAndSuccessAreTriggers() {
			using (TestGameClient game = this.Login("Kouzelnik")) {
				this.Run(game, ".TestSpellTriggers", "SPELLTRIG_DONE");
				this.AssertChecks("spellPowerRaised", "spellPowerBeforeDefense", "spellSuccessRaised", "spellPowerChangesIt", "groundSpellPower");
			}
		}
	}
}
