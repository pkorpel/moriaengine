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
using System.Text;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.Persistence {
	public delegate IUnloadable LoadSection(PropsSection data);
	public delegate bool CanStartAsScript(string data);

	public static class WorldSaver {
		/// <summary>#1203 - where the last finished save's time went; null before the first save.</summary>
		public static SaveTimings LastTimings { get; private set; }

		/// <summary>#1209 - for tests only: the save file of this name fails as it's written, as a full disk would. Null = off.</summary>
		public static string TestWriteFailureFile { get; set; }

		/// <summary>#1209 - a background save whose files are still being written (or whose end hasn't run yet), else null.
		/// Only touched under globalLock.</summary>
		private static PendingSave pending;

		/// <summary>Saves the world and returns once the files are written and @afterSave has run. Called with globalLock held.</summary>
		internal static bool Save() {
			return Save(false);
		}

		/// <summary>#1209 - the world is formatted into memory while the server is paused (the caller's pause), then written on a
		/// worker thread as the game goes on; @afterSave runs once the files are written, with whether they were. Called with
		/// globalLock held. False only when the formatting failed.</summary>
		internal static bool SaveInBackground() {
			return Save(true);
		}

		private static bool Save(bool background) {
			FinishPendingWrite(); //one save at a time: the previous one's files written and its end run first

			using (StopWatch.StartAndDisplay(background ? "Saving world data (written in the background)..." : "Saving world data...")) {
				PacketSequences.BroadCast("Server is pausing for worldsave...");

				long start = Stopwatch.GetTimestamp();
				SaveTimings timings = new SaveTimings { Background = background };
				WeakRefDictionaryUtils.PurgeAll();
				timings.PurgeMs = MsSince(start);

				PendingSave save = Format(timings, start);
				if (save.FormatFailed) {
					save.Finish();
					return false;
				}
				if (background) {
					pending = save;
					System.Threading.Thread writer = new System.Threading.Thread(save.WriteThenFinish);
					writer.IsBackground = true;
					writer.Name = "World save writer";
					writer.Start();
					return true;
				}
				save.Write();
				save.Finish();
				return save.Success;
			}
		}

		/// <summary>#1209 - waits until a background save's files are written, then runs its end (@afterSave) here if its writer
		/// hasn't yet. Takes globalLock for that (it may be held already). The next save, a recompile and the shutdown call it.</summary>
		public static void FinishPendingWrite() {
			PendingSave save;
			lock (MainClass.globalLock) {
				save = pending;
			}
			if (save == null) {
				return;
			}
			save.WaitWritten();
			lock (MainClass.globalLock) {
				save.TryFinish();
				if (pending == save) {
					pending = null;
				}
			}
		}

		private static double MsSince(long timestamp) {
			return (Stopwatch.GetTimestamp() - timestamp) * 1000.0 / Stopwatch.Frequency;
		}

		/// <summary>@beforeSave, then every file formatted into memory: all of the world's pause but the purge.</summary>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static PendingSave Format(SaveTimings timings, long start) {
			string path = Globals.SavePath;

			ScriptArgs sa = new ScriptArgs(path);
			long beforeStart = Stopwatch.GetTimestamp();
			Globals.Instance.TryTrigger(TriggerKey.beforeSave, sa);
			timings.BeforeSaveMs = MsSince(beforeStart);
			path = string.Concat(sa.Argv[0]);
			if (path.Length < 1) {//scripts error or something...
				path = Globals.SavePath;
			}
			timings.Folder = path;

			ObjectSaver.StartingSaving();

			PendingSave save = new PendingSave(path, timings, start);
			try {
				Tools.EnsureDirectory(path, true); //#1173 - in here: a folder that can't be made fails the save the usual way, @afterSave included (the autosave re-arms there)

				save.Add(FormatFile(path, "globals", timings, Globals.SaveGlobals));
				foreach (IBaseClassSaveCoordinator coordinator in ObjectSaver.AllCoordinators) {
					save.Add(FormatFile(path, coordinator.FileNameToSave, timings, coordinator.SaveAll));
				}
			} catch (FatalException) {
				save.Abandon();
				throw;
			} catch (Exception e) {
				Logger.WriteCritical(e);
				save.Abandon();
			}

			ObjectSaver.SavingFinished();
			timings.PauseMs = MsSince(start);
			return save;
		}

		/// <summary>One file: the openSaveStream trigger names where it goes (scripts' BackupsManager hands its own writer), and
		/// the text is formatted and encoded into a SaveBuffer.</summary>
		private static SaveFile FormatFile(string path, string name, SaveTimings timings, Action<SaveStream> content) {
			SaveTimings.FileTiming timing = timings.AddFile(name);
			long start = Stopwatch.GetTimestamp();
			ScriptArgs sa = new ScriptArgs(path, name);
			Globals.Instance.TryTrigger(TriggerKey.openSaveStream, sa);
			SaveFile file = new SaveFile(path, name, sa.Argv[1], timing);
			timing.OpenMs = MsSince(start);

			start = Stopwatch.GetTimestamp();
			StreamWriter encoder = new StreamWriter(file.Bytes, SaveStream.SaveEncoding, SaveStream.WriterBufferChars, true);
			SaveStream saveStream = new SaveStream(encoder);
			saveStream.WriteComment("Textual SteamEngine save");
			content(saveStream);
			saveStream.WriteLine("[EOF]");
			encoder.Flush();
			encoder.Dispose();
			timing.FormatMs = MsSince(start);
			timing.Bytes = file.Bytes.Length;
			return file;
		}

		/// <summary>#1209 - one formatted file and where it goes.</summary>
		private sealed class SaveFile {
			internal readonly string folder;
			internal readonly string name;
			internal readonly object target; //what openSaveStream left: a TextWriter, a Stream, or a file name
			internal readonly SaveTimings.FileTiming timing;
			internal readonly SaveBuffer Bytes = new SaveBuffer();

			internal SaveFile(string folder, string name, object target, SaveTimings.FileTiming timing) {
				this.folder = folder;
				this.name = name;
				this.target = target;
				this.timing = timing;
			}

			/// <summary>Writes the bytes to the target and closes it. Any failure throws (#1205: a failed write is a failed save).</summary>
			internal void Write() {
				if (TestWriteFailureFile == this.name) {
					throw new IOException("Simulated write failure of " + this.name + ".sav (WorldSaver.TestWriteFailureFile)");
				}
				TextWriter textWriter = this.target as TextWriter;
				if (textWriter != null) {
					StreamWriter streamWriter = textWriter as StreamWriter;
					if ((streamWriter != null) && IsPlainUtf8(streamWriter.Encoding)) {
						streamWriter.Flush();
						this.Bytes.CopyBytesTo(streamWriter.BaseStream); //the same bytes the writer would have made
					} else {
						textWriter.Write(this.Bytes.GetText(SaveStream.SaveEncoding));
					}
					textWriter.Dispose();
					return;
				}
				Stream stream = this.target as Stream;
				if (stream != null) {
					this.Bytes.CopyBytesTo(stream);
					stream.Dispose();
					return;
				}
				string filepath = Path.Combine(this.folder, string.Concat(this.target) + ".sav");
				string tempPath = filepath + ".tmp";
				using (FileStream file = SaveStream.CreateFile(tempPath)) {
					this.Bytes.CopyBytesTo(file);
				}
				File.Move(tempPath, filepath, true); //only once the whole file is written
			}

			[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			internal void CloseTarget() {
				try {
					IDisposable disposable = this.target as IDisposable;
					if (disposable != null) {
						disposable.Dispose();
					}
				} catch (Exception e) {
					Logger.WriteWarning("Error closing the save file " + this.name + ": " + e.Message);
				}
			}

			private static bool IsPlainUtf8(Encoding encoding) {
				return (encoding.CodePage == 65001) && (encoding.GetPreamble().Length == 0);
			}
		}

		/// <summary>#1209 - a save between formatting and its end: written (on this thread or a worker), then @afterSave runs
		/// once, under globalLock, on whichever side gets there first.</summary>
		private sealed class PendingSave {
			private readonly string folder;
			private readonly SaveTimings timings;
			private readonly long start;
			private readonly List<SaveFile> files = new List<SaveFile>();
			private readonly System.Threading.ManualResetEventSlim written = new System.Threading.ManualResetEventSlim(false);
			private int finished;

			internal bool FormatFailed { get; private set; }
			internal bool Success { get; private set; }

			internal PendingSave(string folder, SaveTimings timings, long start) {
				this.folder = folder;
				this.timings = timings;
				this.start = start;
			}

			internal void Add(SaveFile file) {
				this.files.Add(file);
			}

			/// <summary>Formatting failed: nothing gets written, what's open is closed.</summary>
			internal void Abandon() {
				this.FormatFailed = true;
				foreach (SaveFile file in this.files) {
					file.CloseTarget();
					file.Bytes.Release();
				}
				this.written.Set();
			}

			[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			internal void Write() {
				bool ok = true;
				foreach (SaveFile file in this.files) {
					long fileStart = Stopwatch.GetTimestamp();
					if (ok) {
						try {
							file.Write();
						} catch (Exception e) {
							Logger.WriteCritical("Writing the save file " + file.name + " failed, the save failed", e);
							ok = false;
							file.CloseTarget();
						}
					} else {
						file.CloseTarget(); //after a failure the rest isn't written, only closed
					}
					file.timing.WriteMs = MsSince(fileStart);
					file.Bytes.Release();
				}
				this.Success = ok;
				this.written.Set();
			}

			/// <summary>The worker: writes, then takes globalLock to run the end, unless someone ran it meanwhile.</summary>
			[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			internal void WriteThenFinish() {
				try {
					this.Write();
					lock (MainClass.globalLock) {
						this.TryFinish();
						if (pending == this) {
							pending = null;
						}
					}
				} catch (Exception e) {
					Logger.WriteCritical("The background save's writer failed", e);
					this.written.Set();
				}
			}

			internal void WaitWritten() {
				this.written.Wait();
			}

			/// <summary>Runs Finish unless it has run. Under globalLock.</summary>
			internal void TryFinish() {
				if (System.Threading.Interlocked.Exchange(ref this.finished, 1) == 0) {
					this.Finish();
				}
			}

			/// <summary>@afterSave with whether the files are on disk, then the timings and the broadcast. Under globalLock.</summary>
			internal void Finish() {
				this.finished = 1;
				bool success = !this.FormatFailed && this.Success;
				long afterStart = Stopwatch.GetTimestamp();
				Globals.Instance.TryTrigger(TriggerKey.afterSave, new ScriptArgs(this.folder, success));
				this.timings.AfterSaveMs = MsSince(afterStart);
				this.timings.Success = success;
				this.timings.TotalMs = MsSince(this.start);
				LastTimings = this.timings;
				Logger.WriteInfo(true, "Save timings: " + this.timings);
				if (success) {
					PacketSequences.BroadCast("Saving finished.");
				} else {
					PacketSequences.BroadCast("Saving failed!");//we do not throw an exception to kill the server,
					//the admin should do that, after he tries to fix the problem somehow...
				}
			}
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