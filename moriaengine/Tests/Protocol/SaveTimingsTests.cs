using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1203, #1209 - a save reports where its time went, file by file; a background save writes the same, complete files;
	/// two in a row don't overlap; a failed write fails the save (#1205). Server side: scripts/Tests/SaveTimingsTests.cs.</summary>
	[Collection(SaveTimingsCollection.Name)]
	public class SaveTimingsTests {
		private const string Account = "savetimings";

		[Fact]
		public void ASaveReportsItsTimingsFileByFile() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect(Account, "Save Timer")) {
				//#1360 - before any save: the boot's load reserved the buffers for the first one
				string pool = CommandPermissionTests.Ask(fixture, client, ".TestSavePool", "SAVE_POOL:");
				Assert.True(pool.Contains("loaded=True reserved=True", StringComparison.Ordinal), pool);

				CommandPermissionTests.Ask(fixture, client, ".TestSaveTimings", "SAVE_TIMINGS_FAIL:"); //the last line it prints
				string output = fixture.GameOutputSnapshot();
				string check = output.Substring(output.IndexOf("SAVE_TIMINGS_CHECK:", StringComparison.Ordinal));
				check = check.Substring(0, check.IndexOf('\n') < 0 ? check.Length : check.IndexOf('\n'));
				foreach (string part in new[] { "success=True", "modes=True", "sameFiles=True", "noTmp=True", "allComplete=True",
						"everyFileTimed=True", "bytesMatch=True", "sumsFit=True", "sameContent=True", "firstEndedFirst=True" }) {
					Assert.True(check.Contains(part, StringComparison.Ordinal), part + " missing:\n" + check + "\n" + output);
				}
				Assert.Contains("things.sav", check);
				Assert.Contains("Save timings: pause=", output); //the log line every save writes
				Assert.Contains("SAVE_TIMINGS_FAIL: formatted=True success=False setAside=True nextSaveOk=True end", output); //#1205

				//the autosave's timer (PeriodicSave.scp) saves in the background and is set again by @afterSave
				CommandPermissionTests.Ask(fixture, client, ".TestSaveAutosaveFire", "SAVE_AUTOSAVE_FIRED: timer=True");
				System.Threading.Thread.Sleep(3000);
				CommandPermissionTests.Ask(fixture, client, ".TestSaveAutosaveReport", "SAVE_AUTOSAVE:");
				Assert.Contains("SAVE_AUTOSAVE: background=True success=True rearmed=True end", fixture.GameOutputSnapshot());
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SaveTimingsCollection {
		public const string Name = "SaveTimings";
	}
}
