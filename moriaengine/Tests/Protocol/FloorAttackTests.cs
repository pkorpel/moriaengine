using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1148 - monsters on the ground under a player on a house floor. Server side: scripts/Tests/FloorAttackTests.cs.</summary>
	[Collection(FloorAttackCollection.Name)]
	public class FloorAttackTests {
		private readonly GameServerPairFixture fixture;

		public FloorAttackTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestFloorAttack \"" + what + "\"", "FLOOR_TEST: " + what + " ");
		}

		private void Expect(TestGameClient client, string what, string expected) {
			string line = this.Do(client, what);
			Assert.True(line.Contains(expected), line);
		}

		[Fact]
		public void NothingReachesThroughTheHouseFloor() {
			using var client = CommandPermissionTests.Connect("floortests", "Floor Walker");
			//the user's spot: a house floor (0x2E6C, Surface, not Noshoot) at z 45 over the street at z 0
			this.Expect(client, "probe", "at=2946,1229,45 col=land=0 [0x2E6C z45 h0 Background, Surface]");
			//below can't see up through it, beside below neither, nor the player down; on the same floor they can
			this.Expect(client, "los", "sameColumnSees=False sameColumnInteract=False besideSees=False playerSeesMage=False sameFloorSees=True");
			//a swing from the street is 45/8 = 5 away (Sphere's 3D distance), on the floor beside 1
			this.Expect(client, "swing", "fromBelow=5 sameFloor=1");
			//pushed along the floor, the player stays on it
			this.Expect(client, "knock", "to=2946,1226,45");
			//Go onto the floor stays on it; to the ground, from above, or with no height, to what's at or below
			this.Expect(client, "go", "floor=45 ground=0 fromAbove=45 flat=0");
			//the user's test: titan, titan mage and titan ranger below, fighting the player for a while: nothing lands
			this.Expect(client, "live", "started");
			Thread.Sleep(6000);
			this.Expect(client, "liveafter", "lost=0 z=45");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class FloorAttackCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "FloorAttack";
	}
}
