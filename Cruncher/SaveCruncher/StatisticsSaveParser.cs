//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using CrunchyUtils;

//namespace SaveCruncher {
//	class SaveParser {

//		static ConcurrentDictionary<string, ConcurrentDictionary<string, int>> statistics = new
//			ConcurrentDictionary<string, ConcurrentDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

//		public static async Task Parse(string saveFilePath) {
//			Logger.Write("Starting loading '" + saveFilePath + "'");



//			await Task.Run(() => {

//				EnumerateSections(saveFilePath).AsParallel().ForAll(ParseSection);
//			});

//			var text = await Task.Run(() => {
//				ConcurrentQueue<string> q = new ConcurrentQueue<string>();
//				foreach (var sectionType in statistics.AsParallel()) {
//					var sb = new StringBuilder();
//					if (sectionType.Key.Equals("VarNames")) {
//						continue;
//					}

//					sb.Append("[").Append(sectionType.Key).AppendLine("]");
//					foreach (var lineStats in sectionType.Value.OrderBy(kvp => -kvp.Value)) {
//						if (lineStats.Value > 1) {
//							sb.Append("	").Append(lineStats.Key).Append(":").AppendLine(lineStats.Value.ToString());
//						}
//					}
//					q.Enqueue(sb.ToString());
//				}
//				return string.Join(Environment.NewLine, q.ToArray());
//			});

//			Logger.Write(text);

//			Logger.Write("Finished loading '" + saveFilePath + "'");
//		}

//		private static IEnumerable<List<string>> EnumerateSections(string saveFilePath) {
//			int c = 0;
//			List<string> sectionLines = null;

//			foreach (var line in File.ReadLines(saveFilePath)) {
//				if (line.StartsWith("[", StringComparison.Ordinal)) {
//					if (sectionLines != null) {
//						yield return sectionLines;
//						sectionLines = null;
//					} else {
//						sectionLines = new List<string>();
//						sectionLines.Add(line);
//					}
//				} else if (sectionLines != null) {
//					sectionLines.Add(line);
//				} else {
//					continue;
//				}
//				c++;
//			}
//			//Logger.Write("Lines: " + c);
//		}

//		public static void ParseSection(List<string> sectionLines) {
//			var headerLine = sectionLines[0];

//			string headerType;

//			var firstSpaceAt = headerLine.IndexOf(' ');
//			var endBracketAt = headerLine.IndexOf(']');

//			if (firstSpaceAt > -1) {
//				headerType = headerLine.Substring(1, firstSpaceAt - 1);
//			} else {
//				headerType = headerLine.Substring(1, endBracketAt - 1);
//			}

//			Action<ConcurrentDictionary<string, int>> singleStatDictUpdate = stat => {
//				for (int i = 1, n = sectionLines.Count; i < n; i++) {
//					var line = sectionLines[i];
//					if (string.IsNullOrWhiteSpace(line)) {
//						continue;
//					}
//					var equalsSignAt = line.IndexOf('=');
//					string valueName;
//					if (equalsSignAt > -1) {
//						valueName = line.Substring(0, equalsSignAt).Trim();
//					} else {
//						valueName = line.Trim();
//					}
//					stat.AddOrUpdate(valueName,
//						1, (k, prev) => prev + 1);
//					stat.AddOrUpdate("__any__",
//						1, (k, prev) => prev + 1);
//				}
//				stat.AddOrUpdate("__n__",
//					1, (k, prev) => prev + 1);
//			};

//			var typestat = statistics.AddOrUpdate(headerType,
//				k => {
//					var stat = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
//					singleStatDictUpdate(stat);
//					return stat;
//				},
//				(k, stat) => {
//					singleStatDictUpdate(stat);
//					return stat;
//				}
//			);
//		}
//	}
//}
