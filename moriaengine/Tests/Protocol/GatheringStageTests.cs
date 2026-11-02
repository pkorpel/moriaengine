using System;
using System.Text.RegularExpressions;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's f_tezba_pauza.</summary>
	[Collection(GatheringStageCollection.Name)]
	public class GatheringStageTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public GatheringStageTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		[Fact]
		public void DarknessStopsABlow() {
			using var client = CommandPermissionTests.Connect("gathertest", "Dark Fighter");
			this.Ask(client, ".TestDarkBlow", "DARK_BLOW:");
			Expect(client, "Je spatne videt");
			this.Ask(client, ".TestLightBack", "LIGHT_BACK:");
		}

		[Fact]
		public void ATryIsHeldBackAndChecked() {
			using var client = CommandPermissionTests.Connect("gathertest", "Gather Tester");
			this.Ask(client, ".TestGatherReady", "GATHER_READY:");

			Assert.Contains("True hidden=False", this.Ask(client, ".TestGatherBegin \"1\"", "GATHER_BEGAN:"));
			Assert.Contains("False", this.Ask(client, ".TestGatherBegin \"1\"", "GATHER_BEGAN:"));
			Expect(client, "Jeste nemuzes pouzit skill");

			this.Ask(client, ".TestGatherClear", "GATHER_CLEARED:");
			Assert.Contains("False", this.Ask(client, ".TestGatherBegin \"4\"", "GATHER_BEGAN:"));
			Expect(client, "You are too far away");

			string delays = this.Ask(client, ".TestGatherDelays", "GATHER_DELAYS:");
			Assert.Contains("at100=2 at110=1.7 strokes=2-6", delays);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GatheringStageCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GatheringStages";
	}
}
