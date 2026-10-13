using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {

	public abstract class LocStringCollection : IEnumerable<KeyValuePair<string, string>> {
		internal const string defaultText = "<LocalisationEntryNotAvailable>";

		//Not used by this class anymore (#104, Part B removed the txt-overlay parsing this
		//was originally for) - kept because GameCore/PropsFileParser.cs reuses it as the
		//shared "name = value //comment" regex for general script-file parsing.
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly Regex valueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private readonly Dictionary<string, string> entriesByName;

		protected LocStringCollection(string defname, string assemblyName, Language language, IEnumerable<KeyValuePair<string, string>> entriesFromCode) {
			this.Language = language;
			this.Defname = defname;
			this.AssemblyName = assemblyName;

			this.entriesByName = entriesFromCode.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

			LocManager.RegisterLoc(this);
		}

		protected LocStringCollection(Language language, string assemblyName, string defname) {
			this.Language = language;
			this.Defname = defname;
			this.AssemblyName = assemblyName;

			this.entriesByName = this.GetEntriesFromCode().ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

			LocManager.RegisterLoc(this);
		}

		public string Defname { get; }

		public Language Language { get; }

		public string AssemblyName { get; }

		protected virtual IEnumerable<KeyValuePair<string, string>> GetEntriesFromCode() {
			throw new NotImplementedException();
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
