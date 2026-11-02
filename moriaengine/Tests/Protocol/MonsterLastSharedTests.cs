using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - the walking bomb, the illusionist, the Menivec, the summon killer and the terror elemental.</summary>
	[Collection(MonsterLastSharedCollection.Name)]
	public class MonsterLastSharedTests {
		private readonly GameServerPairFixture fixture;

		public MonsterLastSharedTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void TheyDoWhatEreborsDid() {
			using var client = CommandPermissionTests.Connect("lastshared", "Last Tester");
			this.Ask(client, ".TestLastReady", "LAST_READY:");
			Assert.Contains("dead=True hurt=True corpses=0", this.Ask(client, ".TestLastBlowUp", "LAST_BLOW:"));
			Assert.Contains("lit=True", this.Ask(client, ".TestLastFuse", "LAST_FUSE:"));
			Assert.Contains("copies=5 oneHit=True", this.Ask(client, ".TestLastIllusion", "LAST_ILLUSION:"));
			Assert.Contains("changed=True name=Menivec back=True rest=True", this.Ask(client, ".TestLastShapeshift", "LAST_SHIFT:"));
			Assert.Contains("summon=200 plain=100", this.Ask(client, ".TestLastSummonKiller", "LAST_KILLER:"));

			//two tiles away: half of the default 50
			Assert.Contains("lost=25", this.Ask(client, ".TestLastTerror", "LAST_TERROR:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MonsterLastSharedCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterLastShared";
	}
}
