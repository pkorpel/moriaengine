using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1203 - a world save reports where its time went, file by file, and the measurement mode (each file formatted into
	/// memory, then written) writes the same files as a normal save: all there, all complete, no .tmp left.
	/// </summary>
	public static class SaveTimingsTests {
		[SteamFunction]
		public static void TestSaveTimings(Character self) {
			SaveTimings normal = SaveOnce(false);
			SaveTimings split = SaveOnce(true);
			Console.WriteLine("SAVE_TIMINGS: normal " + normal);
			Console.WriteLine("SAVE_TIMINGS: split " + split);

			string[] normalFiles = FileNames(normal.Folder);
			string[] splitFiles = FileNames(split.Folder);
			bool sameFiles = normalFiles.SequenceEqual(splitFiles);
			bool noTmp = !Directory.GetFiles(normal.Folder, "*.tmp").Any() && !Directory.GetFiles(split.Folder, "*.tmp").Any();
			bool allComplete = Directory.GetFiles(normal.Folder, "*.sav").Concat(Directory.GetFiles(split.Folder, "*.sav")).All(EndsWithEof);
			bool everyFileTimed = (normal.Files.Count == normalFiles.Length) && (split.Files.Count == splitFiles.Length) &&
				normal.Files.All(f => normalFiles.Contains(f.Name + ".sav")) && split.Files.All(f => splitFiles.Contains(f.Name + ".sav"));
			bool bytesMatch = normal.Files.Concat(split.Files).All(f => f.Bytes > 0) &&
				normal.Files.All(f => f.Bytes == new FileInfo(Path.Combine(normal.Folder, f.Name + ".sav")).Length) &&
				split.Files.All(f => f.Bytes == new FileInfo(Path.Combine(split.Folder, f.Name + ".sav")).Length);
			bool sumsFit = (normal.OpenMs + normal.FormatMs + normal.WriteMs <= normal.TotalMs) && (split.OpenMs + split.FormatMs + split.WriteMs <= split.TotalMs) &&
				normal.Files.All(f => f.OpenMs > 0) && split.Files.All(f => f.OpenMs > 0);
			//accounts and regions don't change between two saves a moment apart: the same text both ways
			bool sameContent = new[] { "accounts.sav", "regions.sav" }.All(f =>
				File.ReadAllBytes(Path.Combine(normal.Folder, f)).SequenceEqual(File.ReadAllBytes(Path.Combine(split.Folder, f))));
			Console.WriteLine("SAVE_TIMINGS_CHECK: success=" + (normal.Success && split.Success) + " modes=" + (!normal.FormatInMemory && split.FormatInMemory) +
				" sameFiles=" + sameFiles + " noTmp=" + noTmp + " allComplete=" + allComplete + " everyFileTimed=" + everyFileTimed +
				" bytesMatch=" + bytesMatch + " sumsFit=" + sumsFit + " sameContent=" + sameContent + " files=" + string.Join(",", normalFiles));
		}

		private static SaveTimings SaveOnce(bool inMemory) {
			WorldSaver.FormatInMemory = inMemory;
			try {
				Globals.Save();
			} finally {
				WorldSaver.FormatInMemory = false;
			}
			return WorldSaver.LastTimings;
		}

		private static string[] FileNames(string folder) {
			return Directory.GetFiles(folder, "*.sav").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
		}

		private static bool EndsWithEof(string file) {
			string text = File.ReadAllText(file);
			return text.TrimEnd().EndsWith("[EOF]", StringComparison.Ordinal);
		}
	}
}
