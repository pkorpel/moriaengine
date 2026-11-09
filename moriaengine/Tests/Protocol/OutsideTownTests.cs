using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1329 - an Outside errand step's things lie outside the giver's town: the road out is walked until it leaves the town's
	/// area, not a fixed 30 steps (which stayed inside Edoras about half the time, Minas Tirith always). Server side: scripts/Tests/OutsideTownTests.cs.</summary>
	[Collection(OutsideTownCollection.Name)]
	public class OutsideTownTests : LivingWorldTestBase {
		private const int Cities = 14; //WorldEvents.Cities
		public OutsideTownTests(GameServerPairFixture servers)
			: base(servers, "outsidetests", "OUTSIDE") {
		}

		[Fact]
		public void OutsideThingsLieOutsideTheTown() {
			using (TestGameClient game = this.Login("Za hradbami")) {
				this.Run(game, ".TestOutsideTown", "OUTSIDE_DONE");
				this.AssertChecks("theOldWalkStaysInside", "edorasThingsLieOutside", "everyTownsThingsLieOutside");
				for (int i = 0; i < Cities; i++) {
					this.Run(game, ".TestOutsideAreas \"" + i + "\"", "OUTSIDE_AREAS_DONE " + i + " end");
				}
				this.Run(game, ".TestOutsideAreasReport", "OUTSIDE_AREAS_REPORTED");
				this.AssertChecks("everyAreasRoadLeavesIt");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class OutsideTownCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "OutsideTown";
	}
}
