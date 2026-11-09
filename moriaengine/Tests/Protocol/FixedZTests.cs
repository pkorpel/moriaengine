using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1324 - a Go into a hall under terrain stays in the hall (GetFixedZ took the land above as the floor). Server side:
	/// scripts/Tests/FixedZTests.cs.</summary>
	[Collection(FixedZCollection.Name)]
	public class FixedZTests : LivingWorldTestBase {
		public FixedZTests(GameServerPairFixture servers)
			: base(servers, "fixedztests", "FIXEDZ") {
		}

		[Fact]
		public void AHallUnderTheMountainKeepsItsFloor() {
			using (TestGameClient game = this.Login("Pod horou")) {
				this.Run(game, ".TestFixedZ", "FIXEDZ_DONE");
				this.AssertChecks("theHallUnderTheMountain", "goKeepsOneInTheHall", "aFloorAboveTheGround");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class FixedZCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "FixedZ";
	}
}
