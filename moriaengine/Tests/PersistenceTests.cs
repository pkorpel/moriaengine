using System;
using System.IO;
using SteamEngine.Persistence;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Regression coverage for the save-format versioning and atomic-write fixes (enhancements
	/// Phase 13 / issue #28) - a crash mid-write used to truncate the previous save, and there
	/// was no way to detect a save file written by an incompatible future version.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class PersistenceTests {
		[Fact]
		public void SaveGlobalsWritesCurrentSaveFormatVersion() {
			var writer = new StringWriter();
			var stream = new SaveStream(writer);
			Globals.SaveGlobals(stream);

			Assert.Contains("saveFormatVersion=" + Globals.CurrentSaveFormatVersion, writer.ToString());
		}

		[Fact]
		public void LoadGlobalsAcceptsItsOwnWrittenSaveFormatVersionWithoutThrowing() {
			string dir = Path.Combine(Path.GetTempPath(), "steamengine-version-roundtrip-" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			try {
				string filePath = Path.Combine(dir, "globals.sav");
				using (StreamWriter fileWriter = new StreamWriter(File.Create(filePath))) {
					var stream = new SaveStream(fileWriter);
					stream.WriteComment("Textual SteamEngine save");
					Globals.SaveGlobals(stream);
					stream.WriteLine("[EOF]");
				}

				bool foundGlobalsSection = false;
				using (StreamReader reader = new StreamReader(File.OpenRead(filePath))) {
					foreach (PropsSection section in PropsFileParser.Load(filePath, reader, _ => false, false)) {
						if (string.Equals(section.HeaderType, "globals", StringComparison.OrdinalIgnoreCase)) {
							Globals.LoadGlobals(section); // must not throw for a version this same build wrote
							foundGlobalsSection = true;
							break;
						}
					}
				}
				Assert.True(foundGlobalsSection, "No Globals section found in the written save file.");
			} finally {
				Directory.Delete(dir, true);
			}
		}

		[Fact]
		public void SaveStreamAtomicWrite_OnlyReplacesFinalFileOnSuccessfulClose() {
			string dir = Path.Combine(Path.GetTempPath(), "steamengine-atomic-save-" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			try {
				string finalPath = Path.Combine(dir, "test.sav");
				string tempPath = finalPath + ".tmp";
				File.WriteAllText(finalPath, "old content");

				var stream = new SaveStream(new StreamWriter(File.Create(tempPath)), tempPath, finalPath);
				stream.WriteLine("new content");
				stream.Close();

				Assert.False(File.Exists(tempPath), "Temp file should be gone after a successful Close().");
				Assert.Equal("new content" + Environment.NewLine, File.ReadAllText(finalPath));
			} finally {
				Directory.Delete(dir, true);
			}
		}
	}
}
