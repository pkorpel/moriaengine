using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace SteamEngine.Common {

	public enum Language {
		Default, English = Default,
		Czech
	}

	public static class LocManager {
		private static readonly ConcurrentDictionary<string, LocStringCollection>[] loadedLanguages = InitDictArrays();

		private static ConcurrentDictionary<string, LocStringCollection>[] InitDictArrays() {
			int n = Tools.GetEnumLength<Language>();
			var langs = new ConcurrentDictionary<string, LocStringCollection>[n];

			for (int i = 0; i < n; i++) {
				langs[i] = new ConcurrentDictionary<string, LocStringCollection>(StringComparer.InvariantCultureIgnoreCase);
			}

			return langs;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void RegisterLoc(LocStringCollection newLoc) {
			string className = newLoc.Defname;
			Language lan = newLoc.Language;

			var previous = loadedLanguages[(int) lan].GetOrAdd(className, newLoc);
			if (previous != newLoc) {
				throw new SEException("Loc instance '" + className + "' already exists");
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void UnregisterLoc(LocStringCollection newLoc) {
			string className = newLoc.Defname;
			Language lan = newLoc.Language;

			LocStringCollection previous;
			if (loadedLanguages[(int) lan].TryRemove(className, out previous)) {
				if (previous != newLoc) {
					if (!loadedLanguages[(int) lan].TryAdd(className, previous)) {
						throw new FatalException("Parallel loading fucked up.");
					}
					throw new SEException("Loc instance '" + className + "' was not registered.");
				}
			} else {
				throw new FatalException("Parallel loading fucked up.");
			}
		}

		public static LocStringCollection GetLoc(string defname, Language language) {
			LocStringCollection retVal;
			loadedLanguages[(int) language].TryGetValue(defname, out retVal);
			return retVal;
		}

		public static string GetEntry(string defname, string entryName) {
			return GetEntry(defname, entryName, Language.Default);
		}

		public static string GetEntry(string defname, string entryName, Language language) {
			LocStringCollection retVal;
			if (loadedLanguages[(int) language].TryGetValue(defname, out retVal)) {
				return retVal.GetEntry(entryName);
			}
			return LocStringCollection.defaultText;
		}

		public static Language TranslateLanguageCode(string languageCode) {
			languageCode = languageCode.ToLowerInvariant();
			switch (languageCode) {
				case "cz":
				case "cze":
				case "czech":
				case "cs":
				case "cesky":
				case "èesky":
				case "èeština":
				case "cs-cz":
					return Language.Czech;
			}
			return Language.English;
		}

		public static void ForgetInstancesFromAssembly(Assembly assemblyBeingUnloaded) {
			//we copy the list first, because we're going to remove some entries in a foreach loop
			var allLocs = loadedLanguages[(int) Language.Default].Values.ToList();

			foreach (LocStringCollection loc in allLocs) {
				if (loc.GetType().Assembly == assemblyBeingUnloaded) {
					foreach (var langDict in loadedLanguages) {
						LocStringCollection ignored;
						langDict.TryRemove(loc.Defname, out ignored);
					}
				}
			}
		}

		public static void ForgetInstancesOfType(Type type) {
			//we copy the list first, because we're going to remove some entries in a foreach loop
			var allLocs = loadedLanguages[(int) Language.Default].Values.ToList();

			foreach (LocStringCollection loc in allLocs) {
				if (loc.GetType() == type) {
					foreach (var langDict in loadedLanguages) {
						LocStringCollection ignored;
						langDict.TryRemove(loc.Defname, out ignored);
					}
				}
			}
		}
	}
}
