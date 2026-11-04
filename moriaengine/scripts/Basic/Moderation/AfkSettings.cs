using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1107 - the AFK check's numbers, Erebor's live values (save/sphereworld.scp nastaveni_afk_* and
	/// systems/administrace/afk_defs.scp). On .settings as "AFK".</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class AfkSettings : SettingsMetaCategory {

		[SavedMember]
		public static AfkSettings instance = new AfkSettings();

		[LoadingInitializer]
		public AfkSettings() {
		}

		/// <summary>nastaveni_afk_toggleonoff: the automatic check of gathering and crafting players. Off on Erebor: GMs
		/// ran the checks.</summary>
		[SaveableData]
		public bool automaticCheck = false;

		/// <summary>d_afk_minTime, d_afk_maxTime: seconds between automatic rounds.</summary>
		[SaveableData]
		public int minSecondsBetween = 500;

		[SaveableData]
		public int maxSecondsBetween = 3300;

		/// <summary>One round in this many comes soon after the last, d_afk_minTimeCritical to d_afk_maxTimeCritical,
		/// so a player can't relax right after answering.</summary>
		[SaveableData]
		public int soonAfterOneIn = 15;

		[SaveableData]
		public int soonMinSeconds = 60;

		[SaveableData]
		public int soonMaxSeconds = 130;

		/// <summary>nastaveni_afk_countdown: the warning before a check.</summary>
		[SaveableData]
		public int countdownSeconds = 15;

		/// <summary>nastaveni_afk_tolerance: time to type the letters.</summary>
		[SaveableData]
		public int answerSeconds = 15;

		/// <summary>nastaveni_povoleno_AFKfails: failures let go in a period; the next one sends to the camp.</summary>
		[SaveableData]
		public int allowedFailures = 2;

		/// <summary>nastaveni_AFK_dniKOdmazani: how long a period of failures lasts, in days.</summary>
		[SaveableData]
		public int periodDays = 15;

		/// <summary>nastaveni_uspesnych_AFK_k_odmazani: correct answers that wipe one failure.</summary>
		[SaveableData]
		public int correctToWipeFailure = 60;
	}
}
