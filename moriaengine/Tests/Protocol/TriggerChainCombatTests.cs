using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1558 - chain probes for the swing, written against the chain before #1558 (the lines they print show the old
	/// order) and then changed to expect the reworked one. Server side: scripts/Tests/TriggerChainCombatTests.cs.</summary>
	[Collection(TriggerChainCombatCollection.Name)]
	public class TriggerChainCombatTests : LivingWorldTestBase {
		public TriggerChainCombatTests(GameServerPairFixture servers)
			: base(servers, "triggerchaincombat", "CHAINC") {
		}

		[Fact]
		public void TheSwingIsADenyThenCalculationsThenNotifications() {
			using (TestGameClient game = this.Login("Bojovnik")) {
				this.Run(game, ".TestChainSwing", "CHAINC_SWING_DONE");
				this.AssertChecks("landedOrder", "swingCancelStopsAll", "refusedDamageIsADeny");
			}
		}

		/// <summary>A dodged blow: the attacker's @hit still runs (flagged dodged), the defender's @getHit doesn't.</summary>
		[Fact]
		public void ADodgedBlowRaisesOnlyTheAttackersHit() {
			using (TestGameClient game = this.Login("Uhybac")) {
				this.Run(game, ".TestChainDodged", "CHAINC_DODGED_DONE");
				this.AssertChecks("dodgedOrder");
			}
		}

		[Fact]
		public void DrawingASwingAsksTheAttackersDeny() {
			using (TestGameClient game = this.Login("Napraznik")) {
				this.Run(game, ".TestChainDrawSwing", "CHAINC_DRAW_DONE");
				this.AssertChecks("drawRaisesDenySwing", "deniedSwingRetries");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TriggerChainCombatCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TriggerChainCombat";
	}
}
