using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1173 - server side of Tests/Protocol/ShutdownSaveTests.cs: a mark on the character, set before a shutdown and read
	/// after the next boot, shows whether the shutdown saved.</summary>
	public static class ShutdownSaveTests {
		private static readonly TagKey markTK = TagKey.Acquire("_shutdownSaveMark_");
		private static readonly SteamEngine.Timers.TimerKey autosaveTK = SteamEngine.Timers.TimerKey.Acquire("PeriodicSave"); //scripts/Basic/PeriodicSave.scp

		[SteamFunction]
		public static void TestShutdownMark(Character self, ScriptArgs sa) {
			self.SetTag(markTK, sa.Args.Trim().Trim('"'));
			Console.WriteLine("SHUTDOWN_SAVE: marked " + self.GetTag(markTK) + " |");
		}

		[SteamFunction]
		public static void TestShutdownReport(Character self) {
			object mark = self.GetTag(markTK);
			Console.WriteLine("SHUTDOWN_SAVE: mark=" + (mark ?? "none") + " autosaveMinutes=" + Globals.AutosaveMinutes +
				" autosaveTimer=" + (Globals.Instance.GetTimer(autosaveTK) != null) + " |");
		}

		/// <summary>#1190 - how many static regions the world has (the archive map's, from scripts) and which one holds self.</summary>
		[SteamFunction]
		public static void TestShutdownRegionReport(Character self) {
			Console.WriteLine("SHUTDOWN_REGIONS: count=" + SteamEngine.Regions.StaticRegion.AllRegions.Count + " world=" +
				SteamEngine.Regions.StaticRegion.WorldRegion.Defname + " here=" + self.Region.Defname + " |");
		}

		/// <summary>Sets the account's max plevel, to try the shutdown commands as a plain GM (4).</summary>
		[SteamFunction]
		public static void TestShutdownSetMaxPlevel(Character self, ScriptArgs sa) {
			int level = int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			typeof(AbstractAccount).GetField("maxPlevel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self.Account, (byte) level);
			self.Account.PLevel = (byte) level;
			Console.WriteLine("SHUTDOWN_SAVE: maxPlevel=" + self.Account.MaxPLevel + " |");
		}

		/// <summary>The next save fails after its folder is made: a folder stands where each of its files should go.</summary>
		[SteamFunction]
		public static void TestShutdownBreakSaveWrites(Character self) {
			E_ShutdownSaveBreaker_Global.breakWrites = true;
			Console.WriteLine("SHUTDOWN_SAVE: next save's writes broken |");
		}

		/// <summary>Says whether the save broken by TestShutdownBreakSaveWrites had its folder set aside (renamed to *.failed), so
		/// no boot tries it.</summary>
		[SteamFunction]
		public static void TestShutdownFailedSaveReport(Character self) {
			string folder = E_ShutdownSaveBreaker_Global.brokenFolder;
			bool setAside = (folder != null) && !Directory.Exists(folder) && Directory.Exists(folder.TrimEnd(Path.DirectorySeparatorChar) + ".failed");
			Console.WriteLine("SHUTDOWN_SAVE: failed save " + folder + " setAside=" + setAside + " |");
		}

		/// <summary>Points this server's save folder under a regular file, so no save folder can be made and every save from now on
		/// fails. Only for the one boot: the next one reads steamengine.ini again.</summary>
		[SteamFunction]
		public static void TestShutdownBreakSaves(Character self) {
			string file = Path.GetTempFileName(); //a file, not a folder: nothing can be made below it
			typeof(Globals).GetField("savePath", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, Path.Combine(file, "save"));
			Console.WriteLine("SHUTDOWN_SAVE: saves broken file=" + file + " |");
		}
	}

	/// <summary>Breaks the next save's writes for TestShutdownBreakSaveWrites. As the save opens its first file, a folder is
	/// made where each file is to go (both the .sav BackupsManager writes and WorldSaver's own .sav.tmp), so each File.Create
	/// fails, whichever of this and BackupsManager runs first.</summary>
	public class E_ShutdownSaveBreaker_Global : CompiledTriggerGroup {
		internal static bool breakWrites;
		internal static string brokenFolder;

		public void On_OpenSaveStream(Globals ignored, ScriptArgs sa) {
			if (breakWrites) {
				breakWrites = false;
				brokenFolder = string.Concat(sa.Argv[0]);
				List<string> names = new List<string>();
				if (sa.Argv[1] is string) {
					names.Add((string) sa.Argv[1]); //this one isn't open yet
				}
				foreach (SteamEngine.Persistence.IBaseClassSaveCoordinator coordinator in SteamEngine.Persistence.ObjectSaver.AllCoordinators) {
					names.Add(coordinator.FileNameToSave);
				}
				foreach (string name in names) {
					Directory.CreateDirectory(Path.Combine(brokenFolder, name + ".sav"));
					Directory.CreateDirectory(Path.Combine(brokenFolder, name + ".sav.tmp"));
				}
				Console.WriteLine("SHUTDOWN_SAVE: save files blocked in " + brokenFolder + " |");
			}
		}
	}
}
