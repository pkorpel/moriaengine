using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1217 - map sectors read from the memory-mapped .mul files equal the files read the plain way, on facet 0
	/// and facet 2. Server side: scripts/Tests/MapFileTests.cs.</summary>
	public class MapFileTests {
		[Fact]
		public void SectorsReadFromMappedFilesEqualTheFiles() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect("mapfiles", "Map Reader")) {
				int mark = fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMapFiles");
				bool ended = fixture.WaitForGameOutputSince("MAP_FILES_END", mark, TimeSpan.FromSeconds(60));
				string output = fixture.GameOutputSnapshot();
				Assert.True(ended, "no end marker; the server said since the command:\n" + output.Substring(Math.Min(mark, output.Length)));
				int start = output.IndexOf("MAP_FILES:", mark, StringComparison.Ordinal);
				string report = output.Substring(start, output.IndexOf("MAP_FILES_END", start, StringComparison.Ordinal) - start).TrimEnd();
				Assert.True(report == "MAP_FILES: sectors=12 same=True end", "the server reported: " + report);
			}
		}
	}
}
