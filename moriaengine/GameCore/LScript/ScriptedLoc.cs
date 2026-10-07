using System.Collections.Generic;
using System.Linq;
using SteamEngine.Common;

namespace SteamEngine.LScript {

	public class ScriptedLocStringCollection : LocStringCollection, IUnloadable {
		public ScriptedLocStringCollection(string defname, string assemblyName, Language language, IEnumerable<KeyValuePair<string, string>> entriesFromCode)
			: base(defname, assemblyName, language, entriesFromCode) {
		}

		public void Unload() {
			LocManager.UnregisterLoc(this);
			this.IsUnloaded = true;
		}

		public bool IsUnloaded { get; private set; }

		internal static IUnloadable Load(PropsSection section) {
			var oldLoc = LocManager.GetLoc(section.HeaderName, Language.Default);
			if (oldLoc != null) {
				Logger.WriteError(section.Filename, section.HeaderLine, "ScriptedLoc " + LogStr.Ident(section.HeaderName) + " defined multiple times. Ignoring");
				return null;
			}

			int n = Tools.GetEnumLength<Language>();
			IUnloadable[] langs = new IUnloadable[n];

			for (int i = 0; i < n; i++) {
				var newLoc = new ScriptedLocStringCollection(defname: section.HeaderName, assemblyName: "LScript", language: (Language) i,
					entriesFromCode: section.PropsLines.Select(line => new KeyValuePair<string, string>(line.Name, line.Value)));
				langs[i] = newLoc;
			}

			if (section.TriggerCount > 1) {
				Logger.WriteWarning(section.Filename, section.HeaderLine, "Triggers in a ScriptLoc are nonsensual (and ignored).");
			}

			return new UnloadableGroup(langs);
		}
	}

	public class UnloadableGroup : IUnloadable {
		readonly IUnloadable[] array;

		public UnloadableGroup(params IUnloadable[] array) {
			this.array = array;
		}

		public void Unload() {
			foreach (IUnloadable member in this.array) {
				if (member != null) {
					member.Unload();
				}
			}
		}

		public bool IsUnloaded {
			get {
				foreach (IUnloadable member in this.array) {
					if (member != null) {
						if (member.IsUnloaded) {
							return true;
						}
					}
				}
				return false;
			}
		}
	}
}
