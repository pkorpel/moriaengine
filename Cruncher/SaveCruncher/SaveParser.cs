using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchyUtils;

namespace SaveCruncher {
	class SaveParser {

		const int sessionChunkSize = 10000;

		public static async Task Parse(string saveFilePath, Action<IEnumerable<SaveEntry>> processParsedEntry) {
			Logger.Write("Starting loading '" + saveFilePath + "'");

			var queue = new BlockingCollection<SaveEntry>();

			var consumer = Task.Run(() => {

				var tasksList = new List<Task>();
				var tempList = new List<SaveEntry>(sessionChunkSize);

				foreach (var t in queue.GetConsumingEnumerable()) {
					tempList.Add(t);

					if (tempList.Count >= sessionChunkSize) {
						tasksList.Add(Task.Run(() => processParsedEntry(tempList)));

						tempList = new List<SaveEntry>(sessionChunkSize);
					}
				}

				tasksList.Add(Task.Run(() => processParsedEntry(tempList)));

				Task.WaitAll(tasksList.ToArray());
			});

			EnumerateSections(saveFilePath).AsParallel().Select(ParseSection).ForAll(e => queue.Add(e));
			queue.CompleteAdding();

			await consumer;

			Logger.Write("Finished loading '" + saveFilePath + "'");
		}

		private static IEnumerable<List<string>> EnumerateSections(string saveFilePath) {
			int c = 0;
			List<string> sectionLines = null;

			foreach (var line in File.ReadLines(saveFilePath)) {
				if (line.StartsWith("[", StringComparison.Ordinal)) {
					if (sectionLines != null) {
						yield return sectionLines;
						sectionLines = null;
					} else {
						sectionLines = new List<string>();
						sectionLines.Add(line);
					}
				} else if (sectionLines != null) {
					sectionLines.Add(line);
				} else {
					continue;
				}
				c++;
			}
			if (sectionLines != null) {
				yield return sectionLines;
			}
			//Logger.Write("Lines: " + c);
		}

		public static SaveEntry ParseSection(List<string> sectionLines) {
			var headerLine = sectionLines[0];

			var entry = new SaveEntry();

			var firstSpaceAt = headerLine.IndexOf(' ');
			var endBracketAt = headerLine.IndexOf(']');

			if (firstSpaceAt > -1) {
				entry.SectionType = headerLine.Substring(1, firstSpaceAt - 1);
				entry.SectionName = headerLine.Substring(firstSpaceAt + 1, endBracketAt - firstSpaceAt - 1);
			} else {
				entry.SectionType = headerLine.Substring(1, endBracketAt - 1);
				entry.SectionName = "";
			}


			for (int i = 1, n = sectionLines.Count; i < n; i++) {
				var line = sectionLines[i];
				if (string.IsNullOrWhiteSpace(line)) {
					continue;
				}
				var equalsSignAt = line.IndexOf('=');

				string key;
				string value;

				if (equalsSignAt > -1) {
					key = line.Substring(0, equalsSignAt).Trim().Replace(".", "_");
					value = line.Substring(equalsSignAt + 1).Trim();
				} else {
					key = line.Trim();
					value = "";
				}

				entry.Data[key] = value;
			}

			return entry;
		}
	}
}
