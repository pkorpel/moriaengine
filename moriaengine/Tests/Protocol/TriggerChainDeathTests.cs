using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1558 - chain probes for a death, written against the chain before #1558 (the lines they print show the old
	/// behaviour) and then changed to expect the reworked one. Server side: scripts/Tests/TriggerChainDeathTests.cs.</summary>
	[Collection(TriggerChainDeathCollection.Name)]
	public class TriggerChainDeathTests : LivingWorldTestBase {
		public TriggerChainDeathTests(GameServerPairFixture servers)
			: base(servers, "triggerchaindeath", "CHAIND") {
		}

		[Fact]
		public void ADeathIsADenyThenNotifications() {
			using (TestGameClient game = this.Login("Vrah")) {
				this.Run(game, ".TestChainDeath", "CHAIND_DEATH_DONE");
				this.AssertChecks("deathOrder");
			}
		}

		/// <summary>A refused death leaves the victim alive at 1 hit (or what a handler set), and the blow counts only the
		/// hits really taken.</summary>
		[Fact]
		public void ARefusedDeathLeavesOneHit() {
			using (TestGameClient game = this.Login("Milosrdny")) {
				this.Run(game, ".TestChainRefusedDeath", "CHAIND_REFUSED_DONE");
				this.AssertChecks("refusedIsOnlyTheDeny", "refusedLeavesOneHit", "refusedReportsRealDamage", "handlersHitsStay",
					"refusedKillLeavesOneHit");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TriggerChainDeathCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TriggerChainDeath";
	}
}
