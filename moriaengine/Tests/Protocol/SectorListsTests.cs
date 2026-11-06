using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1214 - a map sector keeps characters and ground items apart; every range query still finds each thing once
	/// through making, moving across sectors, disconnecting, picking up, dropping and deleting. Server side:
	/// scripts/Tests/SectorListsTests.cs.</summary>
	[Collection(SectorListsCollection.Name)]
	public class SectorListsTests {
		[Fact]
		public void RangeQueriesFindEachThingOnceThroughItsLife() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect("sectorlists", "Sector Lister")) {
				//the end marker, not the start: the start can be read while the rest of the line is still on its way
				CommandPermissionTests.Ask(fixture, client, ".TestSectorLists", "SECTOR_LISTS_END");
				string output = fixture.GameOutputSnapshot();
				int start = output.IndexOf("SECTOR_LISTS:", StringComparison.Ordinal);
				string report = output.Substring(start, output.IndexOf("SECTOR_LISTS_END", start, StringComparison.Ordinal) - start).TrimEnd();
				const string Expected = "SECTOR_LISTS: made=True movedAway=True movedHere=True disconnected=True reconnected=True fit=True pickedUp=True " +
					"dropped=True player=True deleted=True end";
				Assert.True(report == Expected, "the server reported: " + report);
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SectorListsCollection {
		public const string Name = "SectorLists";
	}
}
