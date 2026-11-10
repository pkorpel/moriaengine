using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#892 - the numbers of Erebor's General and Nazguls, the "Realm Offices" category of .settings.
	/// Defaults are Erebor's (systems/generals.scp, nazguls.scp, sphereworld's nastaveni_*).</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class RealmSettings : SettingsMetaCategory {

		[SavedMember]
		public static RealmSettings instance = new RealmSettings();

		[LoadingInitializer]
		public RealmSettings() {
		}

		/// <summary>nastaveni_generals_honor_distance</summary>
		[SaveableData]
		public int honorDistance = 8;

		[SaveableData]
		public int honorBonus = 5;

		/// <summary>nastaveni_honor_effect_counter</summary>
		[SaveableData]
		public int honorEffectSeconds = 240;

		/// <summary>nastaveni_honor_delay_counter</summary>
		[SaveableData]
		public int honorDelaySeconds = 300;

		/// <summary>nastaveni_nazguls_fear_distance</summary>
		[SaveableData]
		public int fearDistance = 10;

		/// <summary>nastaveni_lordnazguls_fear_distance</summary>
		[SaveableData]
		public int firstFearDistance = 12;

		[SaveableData]
		public int fearPenalty = 3;

		[SaveableData]
		public int firstFearPenalty = 5;

		/// <summary>nastaveni_fear_effect_counter</summary>
		[SaveableData]
		public int fearEffectSeconds = 300;

		/// <summary>nastaveni_fear_delay_counter</summary>
		[SaveableData]
		public int fearDelaySeconds = 180;

		/// <summary>#909 - .nazghulfell: the First Nazgul needs more fame than this (Erebor's Lord/Lady, 9900).</summary>
		[SaveableData]
		public int fellFame = 9900;

		/// <summary>#909 - .nazghulfell: minutes before the First may switch again (Erebor's nextFellSwitch, 5).</summary>
		[SaveableData]
		public double fellCooldownMinutes = 5;

		/// <summary>A ring's wearer needs this level, and so do the victims of the quest.</summary>
		[SaveableData]
		public int ringMinLevel = 30;

		/// <summary>Kills of Gondor players a ring asks for inside the last ringQuestDays, rolling. 0 takes the quest away.</summary>
		[SaveableData]
		public int ringQuestKills = 5;

		[SaveableData]
		public int ringQuestDays = 3;

		/// <summary>How long a new wearer can't lose the ring.</summary>
		[SaveableData]
		public int ringSafeDays = 1;

		/// <summary>The ring leaves someone offline this long.</summary>
		[SaveableData]
		public int ringMaxAwayDays = 7;

		/// <summary>The ring leaves someone offline this many times longer than online.</summary>
		[SaveableData]
		public int ringOfflineRatio = 11;

		/// <summary>The ring leaves if deaths x ringDeathWeight reach kills x the kill weight.</summary>
		[SaveableData]
		public int ringDeathWeight = 7;

		[SaveableData]
		public int ringKillWeight = 5;

		[SaveableData]
		public int firstRingKillWeight = 4;

		/// <summary>Kills a shrine takes off, every ringDecayDays (firstRingDecayDays for the First).</summary>
		[SaveableData]
		public int ringKillDecay = 7;

		[SaveableData]
		public int ringDecayDays = 7;

		[SaveableData]
		public int firstRingDecayDays = 5;

		/// <summary>How many of Mordor the gate to Fela Nazgul lets through.</summary>
		[SaveableData]
		public int trialPasses = 3;

		[SaveableData]
		public int trialMinutes = 30;

		public override string ToString() {
			return "Realm Offices";
		}
	}
}
