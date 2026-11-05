using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1203 - a save reports where its time went, file by file, and the measurement mode writes the same, complete
	/// files. Server side: scripts/Tests/SaveTimingsTests.cs.</summary>
	[Collection(SaveTimingsCollection.Name)]
	public class SaveTimingsTests {
		private const string Account = "savetimings";

		[Fact]
		public void ASaveReportsItsTimingsFileByFile() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect(Account, "Save Timer")) {
				CommandPermissionTests.Ask(fixture, client, ".TestSaveTimings", "SAVE_TIMINGS_CHECK:");
				string output = fixture.GameOutputSnapshot();
				string check = output.Substring(output.IndexOf("SAVE_TIMINGS_CHECK:", StringComparison.Ordinal));
				check = check.Substring(0, check.IndexOf('\n') < 0 ? check.Length : check.IndexOf('\n'));
				foreach (string part in new[] { "success=True", "modes=True", "sameFiles=True", "noTmp=True", "allComplete=True",
						"everyFileTimed=True", "bytesMatch=True", "sumsFit=True", "sameContent=True" }) {
					Assert.True(check.Contains(part, StringComparison.Ordinal), part + " missing:\n" + check + "\n" + output);
				}
				Assert.Contains("things.sav", check);
				Assert.Contains("Save timings: total=", output); //the log line every save writes
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SaveTimingsCollection {
		public const string Name = "SaveTimings";
	}
}
