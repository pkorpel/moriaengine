using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1216 - every range query of the map finds what a brute-force look over all things finds, by range and by
	/// rectangle, and a foreach over it allocates nothing. Server side: scripts/Tests/RangeQueryTests.cs.</summary>
	[Collection(SectorListsCollection.Name)]
	public class RangeQueryTests {
		[Fact]
		public void RangeQueriesMatchABruteForceLookAndAllocateNothing() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect("rangequeries", "Range Querier")) {
				CommandPermissionTests.Ask(fixture, client, ".TestRangeQueries", "RANGE_QUERIES_END");
				string output = fixture.GameOutputSnapshot();
				int start = output.IndexOf("RANGE_QUERIES:", StringComparison.Ordinal);
				string report = output.Substring(start, output.IndexOf("RANGE_QUERIES_END", start, StringComparison.Ordinal) - start).TrimEnd();
				Assert.True(report == "RANGE_QUERIES: match=True noAlloc=True end", "the server reported: " + report);
			}
		}
	}
}
