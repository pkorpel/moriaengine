using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1116 - the PvP batch: league, arenas, RP rank, conquest. Server side: scripts/Tests/PvpTests.cs.</summary>
	[Collection(PvpCollection.Name)]
	public class PvpTests {
		private readonly GameServerPairFixture fixture;

		public PvpTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		/// <summary>Step 1: Erebor's tag.arena and tag.bf come through the converter.</summary>
		[Fact]
		public void RegionsKnowArenasAndBattlefields() {
			using var client = CommandPermissionTests.Connect("pvp", "Pvp Region Tester");
			string regions = this.Ask(client, ".TestPvpRegionTags", "PVP_REGIONS:");
			Assert.Contains("a_Arena_Mordor=True a_Mordorska_bojova_arena=True a_Nova_Arena_Equip_Room=True a_Arena_Lamy=False a_Murdoc=False", regions);
			Assert.Contains("a_Pelenorska_pole=1 a_Anorien=2 a_Vychodni_breh=-1 a_Minas_Morgul=-2 a_Rhovanion=0", regions);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PvpCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Pvp";
	}
}
