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
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Zip;
using OrganicBit.Zip;
using SteamEngine.Common;
using ZipEntry = ICSharpCode.SharpZipLib.Zip.ZipEntry;

//using ICSharpCode.SharpZipLib.Zip;
//using OrganicBit.Zip;

namespace SteamEngine.CompiledScripts {
	public class E_BackupsManager_Global : CompiledTriggerGroup {

		/// <summary>
		/// The maximal size of the save folder on the disk (including the backups) in Megabytes.
		/// </summary>
		/// <remarks>
		/// A positive number means absolute size, negative number means how much space should stay 
		/// on the given disk. After each save, when the size oversteppes the number given here, 
		/// backups are deleted, according to an algorhitm that values more recent backups 
		/// as more important, but still keeps older backups too...
		/// </remarks>
		public const double SavesSize = 0;


		/// <summary>
		/// The maximal number of complete backups.
		/// </summary>
		/// <remarks>
		/// A zero value means any number of backups. 
		/// After each save, when the count oversteppes the number given here, 
		/// backups are deleted, according to an algorhitm that values more recent backups 
		/// as more important, but still keeps older backups too...
		/// </remarks>
		public const int MaxBackups = 50;

		private ISaveFileManager fileManager;
		private ISavePathManager pathManager;
		private int loadAttempts;
		private bool isLastLoadPossibility = true;


		public E_BackupsManager_Global() {
			//zip compressed saves. the 9 stands for compression level

			//either one of these zipping "managers" should work. it's yet needed to try them out in a real 
			//shard environment to find out if they're worth it ;)
			//fileManager = new SharpZipFileManager(9);
			//fileManager = new OrganicBitZipFileManager();

			this.fileManager = new NormalFileManager();
			this.pathManager = new SavePathManager(SavesSize, MaxBackups);
		}

		public void On_BeforeSave(Globals ignored, ScriptArgs sa) {
			string path = string.Concat(sa.Argv[0]);
			if (this.pathManager != null) {
				path = this.pathManager.GetSavingPath(path);
			}
			if (this.fileManager != null) {
				this.fileManager.StartSaving(path);
			}
			sa.Argv[0] = path;
		}


		public void On_OpenSaveStream(Globals ignored, ScriptArgs sa) {
			if (this.fileManager != null) {
				sa.Argv[1] = this.fileManager.GetSaveStream(string.Concat(sa.Argv[1]));
			}
		}

		public void On_AfterSave(Globals ignored, ScriptArgs sa) {
			bool success = Convert.ToBoolean(sa.Argv[1]);
			if (success) {
				if (this.fileManager != null) {
					this.fileManager.FinishSaving();
				}
				if (this.pathManager != null) {
					this.pathManager.SavingFinished(string.Concat(sa.Argv[0]));
				}
			}
		}

		//can be called more than once, when the particular load doesn't work
		public void On_BeforeLoad(Globals ignored, ScriptArgs sa) {
			string path = string.Concat(sa.Argv[0]);
			if (this.pathManager != null) {
				path = this.pathManager.GetLoadingPath(path, this.loadAttempts, out this.isLastLoadPossibility);
			}
			if (this.fileManager != null) {
				this.fileManager.StartLoading(path);
			}
			sa.Argv[0] = path;
			this.loadAttempts++;
		}

		public void On_OpenLoadStream(Globals ignored, ScriptArgs sa) {
			if (this.fileManager != null) {
				try {
					sa.Argv[1] = this.fileManager.GetLoadStream(string.Concat(sa.Argv[1]));
				} catch (FileNotFoundException e)
				{
					if (this.isLastLoadPossibility) {
						throw;//this finishes the loading
					}
					throw new SEException("Proper file missing in given folder. Let's try an older one...", e);//keeps on loading
				}
			}
		}

		public void On_AfterLoad(Globals ignored, ScriptArgs sa) {
			bool success = Convert.ToBoolean(sa.Argv[1]);
			if (success) {
				if (this.fileManager != null) {
					this.fileManager.FinishLoading();
				}
				if (this.pathManager != null) {
					this.pathManager.LoadingFinished(string.Concat(sa.Argv[0]));
				}
			}
		}

		///////////////////////////embedded classes////////////////////////////////////


		//does the work at file level
		private interface ISavePathManager {
			string GetSavingPath(string defaultPath);
			void SavingFinished(string path);
			string GetLoadingPath(string defaultPath, int attempt, out bool isLast);
			void LoadingFinished(string path);
		}

		//manages the directories
		private interface ISaveFileManager {
			void StartSaving(string path);
			TextWriter GetSaveStream(string name);
			void FinishSaving();

			void StartLoading(string path);
			StreamReader GetLoadStream(string name);
			void FinishLoading();
		}

		/////////////////////the actual implementing classes///////////////////////////

		//yeah what an original name
		private class SavePathManager : ISavePathManager {
			long maxSavesSize;
			int maxBackups;
			DateTime firstTime;
			DateTime lastTime;

			bool sizeValid;
			long size;

			private List<Backup> sortedByTime;
			private List<Backup> sortedBySpan;

			internal static DateTimeFormatInfo dtfi;
			internal const string TimeFormat = "HH_mm_ss"; //_fffffff";
			internal const string DateFormat = "yyyy-MM-dd";

			static SavePathManager() {
				dtfi = new DateTimeFormatInfo();
				dtfi.ShortDatePattern = DateFormat;
				dtfi.LongDatePattern = DateFormat;
				dtfi.ShortTimePattern = TimeFormat;
				dtfi.LongTimePattern = TimeFormat;

				dtfi.TimeSeparator = "_";
				dtfi.DateSeparator = "-";
			}

			internal SavePathManager(double maxSavesSize, int maxBackups) {
				this.maxBackups = maxBackups;
				this.maxSavesSize = (long) (maxSavesSize * 1024 * 1024);
			}

			public string GetSavingPath(string defaultPath) {
				DateTime now = DateTime.Now;
				string path = Tools.CombineMultiplePaths(
					defaultPath, now.ToString(DateFormat), now.ToString(TimeFormat));
				Tools.EnsureDirectory(path);

				return path;
			}

			public void SavingFinished(string path) {
				Backup nowList = new Backup(path, DateTime.Now);//a bit unexact, because the folder was founded before the save etc, but who cares now...
				if (this.sortedByTime.Count > 1) {
					int index = this.sortedByTime.Count - 1;
					Backup wasLast = this.sortedByTime[index];
					wasLast.SetSpan(this.sortedByTime[index - 1], nowList);
					this.sortedBySpan.Add(wasLast);
				}

				this.lastTime = nowList.time;
				this.sortedByTime.Add(nowList);
				this.sortedBySpan.Sort(Backup.spanComparerInstance);
				this.sortedByTime.Sort(Backup.timeComparerInstance);

				while (this.CountIsOverLimit() || this.SizeIsOverLimit(path)) {
					if (this.sortedByTime.Count < 2) {
						break;
					}
					this.ReduceByOne();
				}
			}

			private void ReduceByOne() {
				if (this.sortedByTime.Count == 2) {
					this.RemoveBackup(this.sortedByTime[0]);
					return;
				}

				double lowestPriority = double.MaxValue;
				Backup lowestPriorityBackup = null;

				TimeSpan firstToLast = this.lastTime - this.firstTime;
				for (int i = 0, n = this.sortedBySpan.Count; i < n; i++) {
					Backup b = this.sortedBySpan[i];
					TimeSpan fromFirst = b.time - this.firstTime;
					double priority;
					checked {
						priority = ((double) fromFirst.Ticks / firstToLast.Ticks) * b.span.Ticks;
					}
					if (priority < lowestPriority) {
						lowestPriorityBackup = b;
						lowestPriority = priority;
					}
				}
				this.RemoveBackup(lowestPriorityBackup);
			}

			private void RemoveBackup(Backup toRemove) {
				int index = this.sortedByTime.IndexOf(toRemove);
				this.sortedBySpan.Remove(toRemove);
				if ((index > 1) && (index < this.sortedBySpan.Count - 2)) {
					this.ResetSpanAtIndex(index - 1);
					this.ResetSpanAtIndex(index + 1);
				}

				this.sortedBySpan.Sort(Backup.spanComparerInstance);
				this.sortedByTime.Remove(toRemove);

				this.firstTime = this.sortedByTime[0].time;
				this.lastTime = this.sortedByTime[this.sortedByTime.Count - 1].time;
				toRemove.Delete();//remove from disk
				this.sizeValid = false;
			}

			private void ResetSpanAtIndex(int i) {
				Backup prev = this.sortedByTime[i - 1];
				Backup next = this.sortedByTime[i + 1];
				Backup cur = this.sortedByTime[i];
				cur.SetSpan(prev, next);
			}

			public string GetLoadingPath(string defaultPath, int attempt, out bool isLast) {
				Tools.EnsureDirectory(defaultPath, true);
				this.InitBackupsLists(defaultPath);
				if (this.sortedByTime.Count == 0) {
					throw new SEException("No previous backup folder found.");
				}
				int index = this.sortedByTime.Count - (1 + attempt);
				isLast = false;
				if (index == 0) {
					isLast = true;
				} else if (index < 0) {
					throw new FatalException("No older backup folders to look for.");
				}
				return this.sortedByTime[index].FullDirPath;
			}

			public void LoadingFinished(string path) {

			}

			private void RefreshSize() {
				if (!this.sizeValid) {
					this.size = 0;
					for (int i = 0, n = this.sortedByTime.Count; i < n; i++) {
						Backup b = this.sortedByTime[i];
						this.size += b.Size;
					}
					this.sizeValid = true;
				}
			}

			private bool SizeIsOverLimit(string path)
			{
				if (this.maxSavesSize < 0) {//disk free space limit
#if MSWIN
					string deviceName = Path.GetPathRoot(Path.GetFullPath(path));
					deviceName = deviceName.Substring(0, deviceName.Length - 1);
					//System.Management.
					ManagementObject disk = new ManagementObject(
						"win32_logicaldisk.deviceid=\"" + deviceName + "\"");
					disk.Get();
					long bytesFree = Convert.ToInt64(disk["FreeSpace"]);
					return (bytesFree < (-this.maxSavesSize));
#else

					DriveInfo[] drives = DriveInfo.GetDrives();
					foreach (DriveInfo drive in drives) {
						if (path.StartsWith(drive.RootDirectory.FullName, StringComparison.Ordinal)) {
							long bytesFree = drive.AvailableFreeSpace;
							return (bytesFree < (-maxSavesSize));
						}
					}
					throw new SEException("Can't decide about free space. Disable counting backups size, or something.");
#endif
				}
				if (this.maxSavesSize > 0) {
					this.RefreshSize();
					return (this.size > this.maxSavesSize);
				}
				return false;
			}

			private bool CountIsOverLimit() {
				if (this.maxBackups == 0) {
					return false;
				}
				return (this.sortedByTime.Count > this.maxBackups);
			}

			private void InitBackupsLists(string rootPath) {
				if (this.sortedByTime == null) {
					this.sortedByTime = new List<Backup>();
					this.sortedBySpan = new List<Backup>();
					DirectoryInfo rootDir = new DirectoryInfo(rootPath);
					foreach (DirectoryInfo subdir in rootDir.GetDirectories()) {
						try {
							DateTime date = DateTime.Parse(subdir.Name, dtfi);
							foreach (DirectoryInfo savedir in subdir.GetDirectories()) {
								try {
									TimeSpan time = DateTime.Parse(savedir.Name, dtfi) - DateTime.Today;
									DateTime dateTime = date + time;
									Backup b = new Backup(savedir, dateTime);
									this.sortedByTime.Add(b);
								} catch (Exception) { }//unkown dir
							}
						} catch (Exception) { }//unkown dir
					}

					this.sortedByTime.Sort(Backup.timeComparerInstance);
					for (int i = 1, n = this.sortedByTime.Count - 1; i < n; i++) {//without the first and last one
						Backup prev = this.sortedByTime[i - 1];
						Backup next = this.sortedByTime[i + 1];
						Backup cur = this.sortedByTime[i];
						cur.SetSpan(prev, next);
						this.sortedBySpan.Add(cur);
					}
					if (this.sortedByTime.Count > 0) {
						this.firstTime = this.sortedByTime[0].time;
						this.lastTime = this.sortedByTime[this.sortedByTime.Count - 1].time;
					}

					this.sortedBySpan.Sort(Backup.spanComparerInstance);
				}
			}

			private class Backup {
				internal DirectoryInfo pathInfo;
				private long size = -1;
				internal bool valid = true;//loading from us has failed in the past.
				internal DateTime time;
				internal TimeSpan span;

				internal static readonly IComparer<Backup> spanComparerInstance = new SpanComparer();
				internal static readonly IComparer<Backup> timeComparerInstance = new TimeComparer();

				internal Backup(string path, DateTime time) {
					this.pathInfo = new DirectoryInfo(path);
					this.time = time;
				}

				internal Backup(DirectoryInfo pathInfo, DateTime time) {
					this.pathInfo = pathInfo;
					this.time = time;
				}

				internal long Size {
					get {
						if (this.size == -1) {
							this.size = GetSizeOfDir(this.pathInfo);
						}
						return this.size;
					}
				}

				internal void SetSpan(Backup prev, Backup next) {
					this.span = next.time - prev.time;
				}

				private static long GetSizeOfDir(DirectoryInfo dir) {
					long sizeSoFar = 0;
					foreach (FileSystemInfo entry in dir.GetFileSystemInfos()) {
						if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
							sizeSoFar += GetSizeOfDir((DirectoryInfo) entry);
						} else {
							sizeSoFar += ((FileInfo) entry).Length;
						}
					}
					return sizeSoFar;
				}

				private class SpanComparer : IComparer<Backup> {
					public int Compare(Backup x, Backup y) {
						return TimeSpan.Compare(x.span, y.span);
					}
				}

				private class TimeComparer : IComparer<Backup> {
					public int Compare(Backup x, Backup y) {
						return DateTime.Compare(x.time, y.time);
					}
				}

				internal string FullDirPath {
					get {
						return this.pathInfo.FullName;
					}
				}

				internal void Delete() {
					string oneup = Path.Combine(this.pathInfo.FullName, "..");
					this.pathInfo.Delete(true);

					DirectoryInfo dayDir = new DirectoryInfo(oneup);
					if (dayDir.Exists) {
						if (dayDir.GetFileSystemInfos().Length == 0) {
							dayDir.Delete();
						}
					}
				}
			}
		}

		//no compression. actually does the same as the core would do, only it's done here so we can control it ;)
		private class NormalFileManager : ISaveFileManager {
			private string path;

			public void StartSaving(string path) {
				this.path = path;
			}

			public TextWriter GetSaveStream(string name) {
				string fileName = Path.Combine(this.path, name + ".sav");
				Console.WriteLine("Saving to " + LogStr.File(fileName));
				return new StreamWriter(File.Create(fileName));
			}

			public void FinishSaving() {
			}

			public void StartLoading(string path) {
				this.path = path;
			}

			public StreamReader GetLoadStream(string name) {
				string fileName = Path.Combine(this.path, name + ".sav");
				Console.WriteLine("Loading " + LogStr.File(fileName));
				//throw new Exception("test exc from GetLoadStream");

				return new StreamReader(File.OpenRead(fileName));
			}

			public void FinishLoading() {

			}
		}

		private class SharpZipFileManager : ISaveFileManager {
			private int compression;
			private ZipOutputStream zipWriter;
			private ZipInputStream zipReader;
			private bool openEntry;

			public SharpZipFileManager(int compression) {
				this.compression = compression;
			}

			public void StartSaving(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Saving to " + LogStr.File(zipFileName));
				this.zipWriter = new ZipOutputStream(File.Create(zipFileName));
				this.zipWriter.SetLevel(this.compression);
				this.openEntry = false;
			}

			public TextWriter GetSaveStream(string name) {
				if (this.openEntry) {
					this.zipWriter.CloseEntry();
				}

				ZipEntry entry = new ZipEntry(name + ".sav");
				entry.DateTime = DateTime.Now;
				this.zipWriter.PutNextEntry(entry);
				this.openEntry = true;

				StreamWriter sw = new StreamWriter(this.zipWriter);
				sw.AutoFlush = true;
				return sw;
			}

			public void FinishSaving() {
				this.zipWriter.CloseEntry();
				this.zipWriter.Finish();
				this.zipWriter.Close();
				this.zipWriter = null;
			}

			public void StartLoading(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Loading " + LogStr.File(zipFileName));
				this.zipReader = new ZipInputStream(File.OpenRead(zipFileName));
			}

			public StreamReader GetLoadStream(string name) {
				this.zipReader.GetNextEntry();
				return new StreamReader(this.zipReader);
			}

			public void FinishLoading() {
				try {
					this.zipReader.Close();
				} catch (Exception) { }
				this.zipReader = null;
			}
		}

		private class OrganicBitZipFileManager : ISaveFileManager {
			private ZipWriter zipWriter;
			private ZipReader zipReader;

			public void StartSaving(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Saving to " + LogStr.File(zipFileName));
				this.zipWriter = new ZipWriter(zipFileName);
			}

			public TextWriter GetSaveStream(string name) {
				OrganicBit.Zip.ZipEntry entry = new OrganicBit.Zip.ZipEntry(name + ".sav");
				entry.ModifiedTime = DateTime.Now;
				this.zipWriter.AddEntry(entry);

				StreamWriter sw = new StreamWriter(new OBStream(this.zipWriter));
				sw.AutoFlush = true;
				return sw;
			}

			public void FinishSaving() {
				this.zipWriter.Close();
				this.zipWriter = null;
			}

			public void StartLoading(string path) {
				string zipFileName = Path.Combine(path, "backup.zip");
				Console.WriteLine("Loading " + LogStr.File(zipFileName));
				try {
					this.zipReader = new ZipReader(zipFileName);
				} catch (Exception e) {
					throw new SEException(e.Message);
				}
			}

			public StreamReader GetLoadStream(string name) {
				this.zipReader.MoveNext();
				return new StreamReader(new OBStream(this.zipReader));
			}

			public void FinishLoading() {
				try {
					this.zipReader.Close();
				} catch (Exception) { }
				this.zipReader = null;
			}

			private class OBStream : Stream {
				internal ZipWriter zipWriter;
				internal ZipReader zipReader;

				protected internal OBStream(ZipWriter zipWriter) {
					this.zipWriter = zipWriter;
				}

				protected internal OBStream(ZipReader zipReader) {
					this.zipReader = zipReader;
				}

				public override void Close() {
					try {
						this.zipWriter.Close();
					} catch (Exception) { }
					try {
						this.zipReader.Close();
					} catch (Exception) { }
				}

				public override void Flush() {
				}

				public override int Read([In, Out] byte[] buffer, int offset, int count) {
					return this.zipReader.Read(buffer, offset, count);
				}
				public override long Seek(long offset, SeekOrigin origin) {
					throw new SEException("Can't seek.");
				}

				public override void SetLength(long length) {
					throw new SEException("Can't set length");
				}
				public override void Write(byte[] buffer, int offset, int count) {
					//Console.WriteLine("Write: "+Server.utf.GetString(buffer));
					this.zipWriter.Write(buffer, offset, count);
				}

				public override bool CanRead {
					get {
						return (this.zipReader != null);
					}
				}

				public override bool CanSeek {
					get {
						return false;
					}
				}

				public override bool CanWrite {
					get {
						return (this.zipWriter != null);
					}
				}

				public override long Length {
					get {
						throw new SEException("Can't get length");
					}
				}

				public override long Position {
					get {
						throw new SEException("Can't get position");
					}
					set {
						throw new SEException("Can't set position");
					}
				}
			}

		}
	}
}

//			private class NoCloseStream : Stream {
//				Stream underlying;
//
//				protected internal NoCloseStream(Stream underlying) {
//					this.underlying = underlying;
//				}
//				
//				public override void Close() {
//					//yeah, nothing! the underlyiping zip file needs not to be closed
//				}
//				
//				public override void Flush() { 
//					underlying.Flush();
//				}
//				public override int Read([In, Out] byte[] buffer, int offset, int count) {
//					return underlying.Read(buffer, offset, count);
//				}
//				public override long Seek(long offset, SeekOrigin origin) {
//					return underlying.Seek(offset, origin);
//				}
//				
//				public override void SetLength(long length) {
//					underlying.SetLength(length);
//				}
//				public override void Write(byte[] buffer, int offset, int count) {
//					//Console.WriteLine("Write: "+Server.utf.GetString(buffer));
//					underlying.Write(buffer, offset, count);
//				}
//				
//				public override bool CanRead { get {
//					return underlying.CanRead;
//				} }
//				
//				public override bool CanSeek { get {
//					return underlying.CanSeek;
//				} }
//				
//				public override bool CanWrite { get {
//					return underlying.CanWrite;
//				} }
//				
//				public override long Length { get {
//					return underlying.Length;
//				} }
//				
//				public override long Position { 
//					get {
//						return underlying.Position;
//					}
//					set {
//						underlying.Position = value;
//					}
//				}
//			}
