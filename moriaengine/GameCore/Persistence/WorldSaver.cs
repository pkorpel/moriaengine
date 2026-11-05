/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.Persistence {
	public delegate IUnloadable LoadSection(PropsSection data);
	public delegate bool CanStartAsScript(string data);

	public static class WorldSaver {
		/// <summary>#1203 - where the last save's time went; null before the first save.</summary>
		public static SaveTimings LastTimings { get; private set; }

		/// <summary>#1203 - a measurement mode, off by default: each file is formatted into memory, then written, so the
		/// timings split formatting from disk writing. Costs the memory of the largest file's text while saving.</summary>
		public static bool FormatInMemory { get; set; }

		internal static bool Save() {

			using (StopWatch.StartAndDisplay("Saving world data...")) {
				PacketSequences.BroadCast("Server is pausing for worldsave...");

				SaveTimings timings = new SaveTimings { FormatInMemory = FormatInMemory };
				long start = Stopwatch.GetTimestamp();
				WeakRefDictionaryUtils.PurgeAll();
				timings.PurgeMs = MsSince(start);

				//-1 because we will save new files now.
				bool success = TrySave(timings);
				timings.Success = success;
				timings.TotalMs = MsSince(start);
				LastTimings = timings;
				Logger.WriteInfo(true, "Save timings: " + timings);
				if (!success) {
					PacketSequences.BroadCast("Saving failed!");//we do not throw an exception to kill the server, 
					//the admin should do that, after he tries to fix the problem somehow...
					return false;
				}
				PacketSequences.BroadCast("Saving finished.");
				return true;
			}
		}

		private static double MsSince(long timestamp) {
			return (Stopwatch.GetTimestamp() - timestamp) * 1000.0 / Stopwatch.Frequency;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		static bool TrySave(SaveTimings timings) {
			string path = Globals.SavePath;

			ScriptArgs sa = new ScriptArgs(path);
			long start = Stopwatch.GetTimestamp();
			Globals.Instance.TryTrigger(TriggerKey.beforeSave, sa);
			timings.BeforeSaveMs = MsSince(start);
			path = string.Concat(sa.Argv[0]);
			if (path.Length < 1) {//scripts error or something...
				path = Globals.SavePath;
			}
			timings.Folder = path;

			ObjectSaver.StartingSaving();

			bool success = false;
			try {
				Tools.EnsureDirectory(path, true); //#1173 - in here: a folder that can't be made fails the save the usual way, @afterSave included (the autosave re-arms there)

				SaveFile(path, "globals", timings, Globals.SaveGlobals);
				foreach (IBaseClassSaveCoordinator coordinator in ObjectSaver.AllCoordinators) {
					SaveFile(path, coordinator.FileNameToSave, timings, coordinator.SaveAll);
				}
				success = true;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical(e);
			} finally {
				long afterStart = Stopwatch.GetTimestamp();
				Globals.Instance.TryTrigger(TriggerKey.afterSave, new ScriptArgs(path, success));
				timings.AfterSaveMs = MsSince(afterStart);
			}

			ObjectSaver.SavingFinished();
			return success;
		}

		/// <summary>One save file, to what the openSaveStream trigger hands (scripts' BackupsManager gives its own writer), else
		/// path/name.sav through a .tmp renamed at the end. In FormatInMemory mode the text is made in memory first, then
		/// written to the same target, so the write (encoding included) is timed apart. SaveStream.Close() handles its own
		/// flush/close/rename error logging.</summary>
		private static void SaveFile(string path, string name, SaveTimings timings, Action<SaveStream> content) {
			SaveTimings.FileTiming timing = timings.AddFile(name);
			ScriptArgs sa = new ScriptArgs(path, name);
			Globals.Instance.TryTrigger(TriggerKey.openSaveStream, sa);
			object file = sa.Argv[1];
			long start = Stopwatch.GetTimestamp();
			SaveStream saveStream;
			if (timings.FormatInMemory) {
				StringWriter text = new StringWriter(CultureInfo.InvariantCulture);
				WriteContent(new SaveStream(text), content);
				timing.FormatMs = MsSince(start);
				start = Stopwatch.GetTimestamp();
				saveStream = GetSaveStream(path, file);
				saveStream.WriteText(text.GetStringBuilder());
			} else {
				saveStream = GetSaveStream(path, file);
				WriteContent(saveStream, content);
				timing.FormatMs = MsSince(start);
				start = Stopwatch.GetTimestamp();
			}
			timing.Bytes = saveStream.BytesWritten;
			saveStream.Close();
			timing.WriteMs = MsSince(start);
		}

		private static void WriteContent(SaveStream saveStream, Action<SaveStream> content) {
			saveStream.WriteComment("Textual SteamEngine save");
			content(saveStream);
			saveStream.WriteLine("[EOF]");
		}

		static SaveStream GetSaveStream(string path, object file) {
			//object file is either string or Stream or TextWriter
			TextWriter tw = file as TextWriter;
			if (tw != null) {
				//WriteLine("GetSaveStream got TextWriter: "+tw);
				return new SaveStream(tw);
			}
			Stream s = file as Stream;
			if (s != null) {
				//Console.WriteLine("GetSaveStream got Stream: "+s);
				return new SaveStream(new StreamWriter(s));
			}
			string filename = string.Concat(file);
			string filepath = Path.Combine(path, filename + ".sav");
			string tempPath = filepath + ".tmp";
			return new SaveStream(new StreamWriter(File.Create(tempPath)), tempPath, filepath);
		}
		////////////////////////////////////////////////////////////////////////////////

		// A persistent I/O/parse error (locked file, corrupt save) would otherwise hang
		// startup forever here with no way for an admin to notice short of killing the
		// process. Bounded retries give transient issues (e.g. a brief file lock) a chance
		// to clear, then fail loudly instead of spinning silently.
		private const int MaxLoadAttempts = 3;

		public static void Load() {
			using (StopWatch.StartAndDisplay("Loading world data...")) {
				for (int attempt = 1; attempt <= MaxLoadAttempts; attempt++) {
					if (TryLoad()) {
						return;
					}
					if (attempt < MaxLoadAttempts) {
						Logger.WriteWarning("World load attempt " + attempt + " of " + MaxLoadAttempts + " failed, retrying...");
						System.Threading.Thread.Sleep(500);
					}
				}
				throw new FatalException("World data failed to load after " + MaxLoadAttempts +
					" attempts. See prior log entries for the underlying error(s).");
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static bool TryLoad() {
			Timer.StartingLoading();
			ObjectSaver.StartingLoading();

			string path = "";

			try {
				path = Globals.SavePath;
				ScriptArgs sa = new ScriptArgs(path);
				Globals.Instance.Trigger(TriggerKey.beforeLoad, sa);
				path = string.Concat(sa.Argv[0]);
				if (path.Length < 1) {//scripts error or something...
					path = Globals.SavePath;
				}
				Tools.EnsureDirectory(path, true);

				sa = new ScriptArgs(path, "globals");
				Globals.Instance.Trigger(TriggerKey.openLoadStream, sa);
				StreamReader loadStream = GetLoadStream(path, sa.Argv[1]);
				InvokeLoad(loadStream, Path.Combine(path, "globals.sav"));
				CloseLoadStream(loadStream);

				HashSet<string> nameSet = new HashSet<string>();
				foreach (IBaseClassSaveCoordinator coordinator in ObjectSaver.AllCoordinators) {
					string name = coordinator.FileNameToSave;
					if (nameSet.Contains(name.ToLowerInvariant())) {
						//we already loaded this file.
						continue;
					}
					nameSet.Add(name.ToLowerInvariant());
					sa = new ScriptArgs(path, name);
					Globals.Instance.Trigger(TriggerKey.openLoadStream, sa);
					loadStream = GetLoadStream(path, sa.Argv[1]);
					InvokeLoad(loadStream, Path.Combine(path, name + ".sav"));
					CloseLoadStream(loadStream);
				}

				Globals.Instance.TryTrigger(TriggerKey.afterLoad, new ScriptArgs(path, true));
				Console.WriteLine("Loading successful");
			} catch (FileNotFoundException e) {
				//this is not critical, just a warning.
				Logger.WriteWarning(e.Message);
				Logger.WriteWarning("No proper save files or backups found; Starting with a blank worldfile, no accounts, etc. The first account to log in will be set as admin.");
				MainClass.ClearWorld();
				StaticRegion.ReloadFromScripts(); //#1190 - the clear took the regions the scripts made
				TriggerGroup.ReAddGlobals();
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteCritical(e);
				Logger.WriteCritical("Loading failed.");
				MainClass.ClearWorld();
				StaticRegion.ReloadFromScripts(); //#1190 - the clear took the regions the scripts made, the next save tried needs them
				TriggerGroup.ReAddGlobals();
				Globals.Instance.TryTrigger(TriggerKey.afterLoad, new ScriptArgs(path, false));
				return false;
			}

			ObjectSaver.LoadingFinished();
			//GameAccount.LoadingFinished();
			Timer.LoadingFinished();
			//Region.LoadingFinished();
			//Thing.LoadingFinished();//Things must come after regions
			return true;
		}

		private static void InvokeLoad(StreamReader stream, string filename) {
			EOFMarked = false;
			foreach (PropsSection section in PropsFileParser.Load(
					filename, stream, StartsAsScript, true)) {

				string type = section.HeaderType.ToLowerInvariant();
				string name = section.HeaderName;
				if (EOFMarked) {
					Logger.WriteWarning(section.Filename, section.HeaderLine, "[EOF] reached. Skipping " + section);
					continue;
				}
				if (string.IsNullOrEmpty(name)) {
					if (type == "eof") {
						EOFMarked = true;
						continue;
						//} else {
						//    GameAccount.Load(input);
						//    return null;
					}
				}
				if (type == "globals") {
					Globals.LoadGlobals(section);
					continue;
				}
				if (ObjectSaver.IsKnownSectionName(type)) {
					ObjectSaver.LoadSection(section);
					continue;
				}
				if (AbstractDef.ExistsDefType(type)) {
					AbstractDef.LoadSectionFromSaves(section);
					continue;
				}
				Logger.WriteError(section.Filename, section.HeaderLine, "Unknown section " + LogStr.Ident(section));
			}
			if (!EOFMarked) {
				throw new SEException("EOF Marker not reached!");
			}
		}

		public static bool StartsAsScript(string headerType) {
			return false;
		}

		private static bool EOFMarked;

		static StreamReader GetLoadStream(string path, object file) {
			//object file is either string or Stream or TextWriter
			StreamReader sr = file as StreamReader;
			if (sr != null) {
				return sr;
			}
			Stream s = file as Stream;
			if (s != null) {
				return new StreamReader(s);
			}
			string filename = string.Concat(file);
			string filepath = Path.Combine(path, filename + ".sav");
			return new StreamReader(File.OpenRead(filepath));
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void CloseLoadStream(StreamReader loadStream) {
			try {
				loadStream.Close();
			} catch (Exception e) {
				Logger.WriteWarning("Error closing load stream: " + e.Message);
			}
		}
	}
}