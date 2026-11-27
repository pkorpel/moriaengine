using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1558 - chain probes for a cast, written against the chain before #1558 (the lines they print show the old
	/// behaviour) and then changed to expect the reworked one. Server side: scripts/Tests/TriggerChainSpellTests.cs.</summary>
	[Collection(TriggerChainSpellCollection.Name)]
	public class TriggerChainSpellTests : LivingWorldTestBase {
		public TriggerChainSpellTests(GameServerPairFixture servers)
			: base(servers, "triggerchainspell", "CHAINS") {
		}

		[Fact]
		public void ACastIsADenyThenItsStartThenNotifications() {
			using (TestGameClient game = this.Login("Kouzelnik")) {
				this.Run(game, ".TestChainCast", "CHAINS_CAST_DONE");
				this.AssertChecks("startOrder", "successOrder", "refusedStopsBeforeTheCast");
			}
		}

		/// <summary>#1558 (pkorpel's decision) - a starving caster's attempt hurts and fails.</summary>
		[Fact]
		public void AStarvingCastHurtsAndFails() {
			using (TestGameClient game = this.Login("Hladovy")) {
				this.Run(game, ".TestChainStarvingCast", "CHAINS_STARVING_DONE");
				this.AssertChecks("starvingHurts", "starvingFails");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TriggerChainSpellCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TriggerChainSpell";
	}
}
