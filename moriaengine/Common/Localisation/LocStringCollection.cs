using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {

	public abstract class LocStringCollection : IEnumerable<KeyValuePair<string, string>> {
		const string servLocDir = "Languages";
		internal const string defaultText = "<LocalisationEntryNotAvailable>";

		//name=value //comment
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex valueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private readonly Dictionary<string, string> entriesByName;

		protected LocStringCollection(string defname, string assemblyName, Language language, IEnumerable<KeyValuePair<string, string>> entriesFromCode) {
			this.Language = language;
			this.Defname = defname;
			this.AssemblyName = assemblyName;

			this.entriesByName = this.LoadEntriesFromLanguageFile(entriesFromCode);

			LocManager.RegisterLoc(this);
		}

		protected LocStringCollection(Language language, string assemblyName, string defname) {
			this.Language = language;
			this.Defname = defname;
			this.AssemblyName = assemblyName;

			this.entriesByName = this.LoadEntriesFromLanguageFile(this.GetEntriesFromCode());

			LocManager.RegisterLoc(this);
		}

		public string Defname { get; }

		public Language Language { get; }

		public string AssemblyName { get; }

		protected virtual IEnumerable<KeyValuePair<string, string>> GetEntriesFromCode() {
			throw new NotImplementedException();
		}

		public Dictionary<string, string> LoadEntriesFromLanguageFile(IEnumerable<KeyValuePair<string, string>> entriesFromCode) {
			var result = entriesFromCode.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
			var helperList = new Dictionary<string,string>(result, StringComparer.OrdinalIgnoreCase);

			string path = Tools.CombineMultiplePaths(".", servLocDir,
				Enum.GetName(typeof(Language), this.Language),
				this.AssemblyName,
				string.Concat(this.Defname, ".txt"));

			FileInfo file = new FileInfo(path);
			if (file.Exists) {
				using (StreamReader reader = file.OpenText()) {
					string line;
					int lineNum = 0;
					while ((line = reader.ReadLine()) != null) {
						lineNum++;
						line = line.Trim();
						if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#")) {
							continue;
						}

						Match m = valueRE.Match(line);
						if (m.Success) {
							GroupCollection gc = m.Groups;
							string name = gc["name"].Value;
							if (result.ContainsKey(name)) {
								if (helperList.ContainsKey(name)) {
									result[name] = gc["value"].Value;
									helperList.Remove(name);
								} else {
									Logger.WriteWarning(path, lineNum, "Duplicate value name '" + name + "'. Ignoring.");
								}
							} else {
								Logger.WriteWarning(path, lineNum, "Unknown value name '" + name + "'. Ignoring.");
							}
						} else {
							Logger.WriteWarning(path, lineNum, "Unknown format of line");
						}
					}
				}
			}

			//lines that are on the class but aren't in the text file, we append them to the file
			if (helperList.Count > 0) {
				Tools.EnsureDirectory(Path.GetDirectoryName(path));
				using (StreamWriter writer = new StreamWriter(file.Open(FileMode.Append, FileAccess.Write))) {
					foreach (KeyValuePair<string, string> pair in helperList) {
						writer.WriteLine(pair.Key.PadRight(32) + "= " + pair.Value);
					}
				}
			}

			return result;
		}

		public string GetEntry(string entryName) {
			string value;
			if (this.entriesByName.TryGetValue(entryName, out value)) {
				return value;
			}
			return defaultText;
		}

		public bool HasEntry(string entryName) {
			return this.entriesByName.ContainsKey(entryName);
		}

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
			return this.entriesByName.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.entriesByName.GetEnumerator();
		}

		public override string ToString() {
			return string.Concat(this.Defname,
				" (", Enum.GetName(typeof(Language), this.Language), ")");
		}
	}
}
