using System;
using System.IO;
using System.Linq;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1203, #1209 - a world save reports where its time went, file by file. A background save (the autosave) writes the same
	/// files as a normal one, holding the world only while formatting; two in a row don't overlap; and a failed write fails
	/// the save (#1205), so BackupsManager sets its folder aside.
	/// </summary>
	public static class SaveTimingsTests {
		[SteamFunction]
		public static void TestSaveTimings(Character self) {
			Globals.Save();
			SaveTimings normal = WorldSaver.LastTimings;
			Globals.SaveInBackground();
			Globals.SaveInBackground(); //waits for the first one's write and runs its end before it starts
			SaveTimings first = WorldSaver.LastTimings; //the first one: the second is still being written
			WorldSaver.FinishPendingWrite();
			SaveTimings second = WorldSaver.LastTimings;
			Console.WriteLine("SAVE_TIMINGS: normal " + normal);
			Console.WriteLine("SAVE_TIMINGS: background " + second);

			string[] normalFiles = FileNames(normal.Folder);
			bool sameFiles = normalFiles.SequenceEqual(FileNames(second.Folder)) && ((first == null) || normalFiles.SequenceEqual(FileNames(first.Folder)));
			bool noTmp = new[] { normal.Folder, second.Folder }.All(f => !Directory.GetFiles(f, "*.tmp").Any());
			bool allComplete = new[] { normal.Folder, second.Folder }.All(f => Directory.GetFiles(f, "*.sav").All(EndsWithEof)) &&
				((first == null) || Directory.GetFiles(first.Folder, "*.sav").All(EndsWithEof));
			bool everyFileTimed = (normal.Files.Count == normalFiles.Length) && (second.Files.Count == normalFiles.Length);
			bool bytesMatch = normal.Files.Concat(second.Files).All(f => f.Bytes > 0) &&
				normal.Files.All(f => f.Bytes == new FileInfo(Path.Combine(normal.Folder, f.Name + ".sav")).Length) &&
				second.Files.All(f => f.Bytes == new FileInfo(Path.Combine(second.Folder, f.Name + ".sav")).Length);
			bool sumsFit = (second.OpenMs + second.FormatMs <= second.PauseMs) && (second.PauseMs <= second.TotalMs) &&
				(normal.OpenMs + normal.FormatMs + normal.WriteMs <= normal.TotalMs) && second.Files.All(f => f.WriteMs > 0);
			//accounts and regions don't change between two saves a moment apart: the same bytes both ways
			bool sameContent = new[] { "accounts.sav", "regions.sav" }.All(f =>
				File.ReadAllBytes(Path.Combine(normal.Folder, f)).SequenceEqual(File.ReadAllBytes(Path.Combine(second.Folder, f))));
			//the second background save waited for the first one's write and ran its end before it formatted
			bool firstEndedFirst = (first != null) && (first != second) && first.Background && (first.Folder != second.Folder) && first.Success;
			Console.WriteLine("SAVE_TIMINGS_CHECK: success=" + (normal.Success && second.Success) + " modes=" + (!normal.Background && second.Background) +
				" sameFiles=" + sameFiles + " noTmp=" + noTmp + " allComplete=" + allComplete + " everyFileTimed=" + everyFileTimed +
				" bytesMatch=" + bytesMatch + " sumsFit=" + sumsFit + " sameContent=" + sameContent + " firstEndedFirst=" + firstEndedFirst +
				" files=" + string.Join(",", normalFiles));

			//#1205 - a write that fails fails the save, and BackupsManager sets the folder aside
			WorldSaver.TestWriteFailureFile = "regions";
			bool formatted;
			try {
				formatted = Globals.SaveInBackground();
				WorldSaver.FinishPendingWrite();
			} finally {
				WorldSaver.TestWriteFailureFile = null;
			}
			SaveTimings failed = WorldSaver.LastTimings;
			string folder = failed.Folder.TrimEnd(Path.DirectorySeparatorChar);
			bool setAside = !Directory.Exists(folder) && Directory.Exists(folder + ".failed");
			Globals.Save();
			Console.WriteLine("SAVE_TIMINGS_FAIL: formatted=" + formatted + " success=" + failed.Success + " setAside=" + setAside +
				" nextSaveOk=" + WorldSaver.LastTimings.Success + " end");
		}

		/// <summary>The autosave's own timer (PeriodicSave.scp) comes due now: it saves in the background.</summary>
		[SteamFunction]
		public static void TestSaveAutosaveFire(Character self) {
			SteamEngine.Timers.Timer timer = Globals.Instance.GetTimer(SteamEngine.Timers.TimerKey.Acquire("PeriodicSave"));
			if (timer != null) {
				timer.DueInSeconds = 0;
			}
			Console.WriteLine("SAVE_AUTOSAVE_FIRED: timer=" + (timer != null));
		}

		[SteamFunction]
		public static void TestSaveAutosaveReport(Character self) {
			WorldSaver.FinishPendingWrite();
			SaveTimings last = WorldSaver.LastTimings;
			SteamEngine.Timers.Timer timer = Globals.Instance.GetTimer(SteamEngine.Timers.TimerKey.Acquire("PeriodicSave"));
			bool rearmed = (timer != null) && (timer.DueInSeconds > (Globals.AutosaveMinutes * 60) - 120);
			Console.WriteLine("SAVE_AUTOSAVE: background=" + last.Background + " success=" + last.Success + " rearmed=" + rearmed + " end");
		}

		private static string[] FileNames(string folder) {
			return Directory.GetFiles(folder, "*.sav").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
		}

		private static bool EndsWithEof(string file) {
			return File.ReadAllText(file).TrimEnd().EndsWith("[EOF]", StringComparison.Ordinal);
		}
	}
}
