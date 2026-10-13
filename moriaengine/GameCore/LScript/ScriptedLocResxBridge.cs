using SteamEngine.Common;

namespace SteamEngine.LScript {

	/// <summary>
	/// Registers the handful of ScriptedLoc-family defnames (#104, Part B) that used to be
	/// defined inline via a [Loc ...]/[ScriptedLoc ...] block in their own .scp/.def file -
	/// those inline blocks are gone now (their content lives in GameText.resx/.Czech.resx
	/// instead, same as the compiled *Loc classes), so nothing triggers
	/// ScriptedLocStringCollection.Load() for them via script loading anymore. This is a
	/// small, closed, hardcoded set (found by direct reference search across the whole
	/// repo) - not tied to script hot-reload, since their content now lives in the resx
	/// files rather than the reloadable script files.
	/// </summary>
	public static class ScriptedLocResxBridge {
		private static readonly (string Defname, string[] EntryNames)[] KnownEntries = {
			("loc_ability_functions", new[] { "NoSuchAbility" }),
			("loc_kick", new[] { "Kicks" }),
			("loc_longrange", new[] { "LongrangeFailed" }),
			("loc_silence", new[] { "YouHaveBeenSilenced", "YourTargetIsSilenced", "YoureSilenced" }),
			("loc_mage", new[] { "ResurrectionTalentMissing" }),
			("loc_playervendor", new[] { "OneMomentPlease", "TargetThingToStock", "NothingToSell" }),
		};

		public static readonly string[] KnownDefnames = System.Array.ConvertAll(KnownEntries, e => e.Defname);

		public static void RegisterAll() {
			int n = Tools.GetEnumLength<Language>();
			foreach (var (defname, entryNames) in KnownEntries) {
				for (int i = 0; i < n; i++) {
					Language lang = (Language) i;
					if (LocManager.GetLoc(defname, lang) != null) {
						continue; // already registered (e.g. a hot-reload re-running this)
					}
					var entries = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();
					foreach (string entryName in entryNames) {
						entries.Add(new System.Collections.Generic.KeyValuePair<string, string>(
							entryName, GameTextResources.GetEntry(defname + "_" + entryName, lang)));
					}
					new ScriptedLocStringCollection(defname, "LScript", lang, entries);
				}
			}
		}
	}
}
