using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - abilities reacting to a blow, from new_events.scp and macros.scp.</summary>
	[Collection(MonsterHitReactionCollection.Name)]
	public class MonsterHitReactionTests {
		private readonly GameServerPairFixture fixture;

		public MonsterHitReactionTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void TheyReact() {
			using var client = CommandPermissionTests.Connect("hitreact", "Hit Reactor");
			Assert.Contains("gold=True", this.Ask(client, ".TestHitReactDrops", "HIT_DROP:"));
			Assert.Contains("small=True", this.Ask(client, ".TestHitReactScales", "HIT_SCALES:"));

			//raised by the first blow, which still lands; then mana pays, until it can't
			Assert.Contains("first=30 raised=True second=0 mana=70 third=500 gone=True", this.Ask(client, ".TestHitReactShield", "HIT_SHIELD:"));

			//a tenth of its hits; past its heals it never starts again
			Assert.Contains("started=True healed=True hits=200 second=False restarted=False", this.Ask(client, ".TestHitReactRegen", "HIT_REGEN:"));

			//a mage wears down the magic ones, a thief the rest, and near immunity stays
			Assert.Contains("fire=475 sharp=500 poison=960 bleed=475", this.Ask(client, ".TestHitReactResist", "HIT_RESIST:"));

			Assert.Contains("max=750 hits=750", this.Ask(client, ".TestHitReactPhoenix", "HIT_PHOENIX:"));
			Assert.Contains("before=True after=False", this.Ask(client, ".TestHitReactPeaceful", "HIT_PEACE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MonsterHitReactionCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterHitReaction";
	}
}
