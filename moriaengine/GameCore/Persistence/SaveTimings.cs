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

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SteamEngine.Persistence {
	/// <summary>
	/// #1203, #1209 - where a world save's time went (WorldSaver.LastTimings). The world is held for PauseMs: the weak
	/// reference purge, the scripts' @beforeSave, each file's openSaveStream trigger (OpenMs) and its formatting into memory
	/// (FormatMs, encoding included). Each file's WriteMs is putting those bytes on disk: before Save returns, or on a worker
	/// thread after the pause for a background save (the autosave). @afterSave runs once the files are written. TotalMs runs
	/// from the start to the end of @afterSave. Not timed: the broadcasts and the sync queues flushed before the save starts.
	/// </summary>
	public sealed class SaveTimings {
		public sealed class FileTiming {
			public string Name { get; internal set; }
			/// <summary>The openSaveStream trigger, which opens the file (scripts' BackupsManager); the engine's own .tmp is opened in WriteMs.</summary>
			public double OpenMs { get; internal set; }
			public double FormatMs { get; internal set; }
			public double WriteMs { get; internal set; }
			public long Bytes { get; internal set; }
		}

		private readonly List<FileTiming> files = new List<FileTiming>();

		/// <summary>#1209 - written on a worker thread after the pause (the autosave), not before the save returned.</summary>
		public bool Background { get; internal set; }
		/// <summary>#1209 - how long the world was held: the purge, @beforeSave and the formatting.</summary>
		public double PauseMs { get; internal set; }
		/// <summary>The folder the files went to (the scripts' beforeSave picks it).</summary>
		public string Folder { get; internal set; }
		public bool Success { get; internal set; }
		public double PurgeMs { get; internal set; }
		public double BeforeSaveMs { get; internal set; }
		public double AfterSaveMs { get; internal set; }
		public double TotalMs { get; internal set; }

		public IList<FileTiming> Files {
			get {
				return this.files;
			}
		}

		internal FileTiming AddFile(string name) {
			FileTiming file = new FileTiming { Name = name };
			this.files.Add(file);
			return file;
		}

		public double OpenMs {
			get {
				double sum = 0;
				foreach (FileTiming file in this.files) {
					sum += file.OpenMs;
				}
				return sum;
			}
		}

		public double FormatMs {
			get {
				double sum = 0;
				foreach (FileTiming file in this.files) {
					sum += file.FormatMs;
				}
				return sum;
			}
		}

		public double WriteMs {
			get {
				double sum = 0;
				foreach (FileTiming file in this.files) {
					sum += file.WriteMs;
				}
				return sum;
			}
		}

		public long Bytes {
			get {
				long sum = 0;
				foreach (FileTiming file in this.files) {
					sum += file.Bytes;
				}
				return sum;
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(CultureInfo.InvariantCulture, "pause={0:F0}ms total={1:F0}ms format={2:F0}ms write={3:F0}ms open={4:F0}ms purge={5:F0}ms beforeSave={6:F0}ms afterSave={7:F0}ms {8:F1}MB background={9} success={10}",
				this.PauseMs, this.TotalMs, this.FormatMs, this.WriteMs, this.OpenMs, this.PurgeMs, this.BeforeSaveMs, this.AfterSaveMs, this.Bytes / 1048576.0, this.Background, this.Success);
			foreach (FileTiming file in this.files) {
				sb.AppendFormat(CultureInfo.InvariantCulture, " | {0} open {1:F0} format {2:F0} write {3:F0}ms {4:F1}MB", file.Name, file.OpenMs, file.FormatMs, file.WriteMs, file.Bytes / 1048576.0);
			}
			return sb.ToString();
		}
	}
}
