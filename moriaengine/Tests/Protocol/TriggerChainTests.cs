using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1558 - chain probes for the double click and the step, written against the chains before #1558 (the lines
	/// they print show the old order) and then changed to expect the reworked ones. Server side: scripts/Tests/TriggerChainTests.cs.</summary>
	[Collection(TriggerChainCollection.Name)]
	public class TriggerChainTests : LivingWorldTestBase {
		public TriggerChainTests(GameServerPairFixture servers)
			: base(servers, "triggerchain", "CHAIN") {
		}

		[Fact]
		public void TheDoubleClickChainAsksTheClickerThenTheTarget() {
			using (TestGameClient game = this.Login("Klikac")) {
				this.Run(game, ".TestChainDClick", "CHAIN_DCLICK_DONE");
				this.AssertChecks("dclickItem", "dclickChar");
			}
		}

		/// <summary>Telekinesis's double click: only the reach test is skipped.</summary>
		[Fact]
		public void SkippingTheReachTestKeepsTheOtherDenies() {
			using (TestGameClient game = this.Login("Telekinetik")) {
				this.Run(game, ".TestChainDClickFromAfar", "CHAIN_AFAR_DONE");
				this.AssertChecks("afarRefusedByReach", "skipReachRunsTheChain", "skipReachKeepsTargetDenies");
			}
		}

		[Fact]
		public void TheStepIsADenyChainThenNotifications() {
			using (TestGameClient game = this.Login("Krokar")) {
				this.Run(game, ".TestChainStep", "CHAIN_STEP_DONE");
				this.AssertChecks("stepOrder", "teleportNotAStep", "denyStepStopsAll", "regionDenyStops");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TriggerChainCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TriggerChain";
	}
}
