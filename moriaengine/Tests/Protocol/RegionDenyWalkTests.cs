using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1579 - a pickup and a drop inside a stall ask the stall and its town; the town's deny refuses the drop; a drop
	/// out of reach is refused (the default reach check never ran before). Server side: scripts/Tests/RegionDenyWalkTests.cs.</summary>
	[Collection(RegionDenyWalkCollection.Name)]
	public class RegionDenyWalkTests : LivingWorldTestBase {
		public RegionDenyWalkTests(GameServerPairFixture servers)
			: base(servers, "regionwalk", "REGIONWALK") {
		}

		[Fact]
		public void TheRegionDenyWalksAskEveryRegion() {
			using (TestGameClient game = this.Login("Odkladac")) {
				this.Run(game, ".TestRegionDenyWalk", "REGIONWALK_DONE");
				this.AssertChecks("pickupAsksEveryRegion", "dropAsksEveryRegion", "parentDeniesDrop", "farDropRefused");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class RegionDenyWalkCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RegionDenyWalk";
	}
}
