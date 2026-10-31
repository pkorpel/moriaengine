using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#927 - the Potions page of .settings: Erebor's nastaveni_potions_*, in its own form:
	/// "seconds,min,max". Times are seconds.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class PotionSettings : SettingsMetaCategory {

		[SavedMember]
		public static PotionSettings instance = new PotionSettings();

		[LoadingInitializer]
		public PotionSettings() {
		}

		/// <summary>nastaveni_global_potiondelay: after a t_speedpotions.</summary>
		[SaveableData]
		public double delay = 5;

		/// <summary>nastaveni_global_potiondelay2: after a t_allpotions.</summary>
		[SaveableData]
		public double longDelay = 20;

		/// <summary>nastaveni_sance_rozbiti_lahve, percent.</summary>
		[SaveableData]
		public int bottleBreakChance = 10;

		[SaveableData]
		public double spamPenaltySeconds = 5;

		[SaveableData]
		public double maxSpamPenaltySeconds = 100;

		[SaveableData] public string agilityLess = "120,12,20";
		[SaveableData] public string agility = "300,14,20";
		[SaveableData] public string agilityGreat = "600,16,20";
		[SaveableData] public string agilityElixir = "900,30,30";
		[SaveableData] public string strengthLess = "120,12,20";
		[SaveableData] public string strength = "300,14,20";
		[SaveableData] public string strengthGreat = "600,16,20";
		[SaveableData] public string strengthElixir = "900,30,30";
		[SaveableData] public string cleverLess = "120,12,20";
		[SaveableData] public string clever = "300,14,20";
		[SaveableData] public string cleverGreat = "600,16,20";
		[SaveableData] public string cleverElixir = "900,30,30";
		[SaveableData] public string energyLess = "120,12,20";
		[SaveableData] public string energy = "300,14,20";
		[SaveableData] public string energyGreat = "600,16,20";
		[SaveableData] public string energyElixir = "900,30,30";
		[SaveableData] public string shield = "600,280,280";
		[SaveableData] public string shieldElixir = "900,420,420";
		[SaveableData] public string mobility = "30,1000,1000";
		[SaveableData] public string stoneskin = "600,5,5";
		[SaveableData] public string stoneskinElixir = "900,7,8";
		[SaveableData] public string dragonResist = "300,10,10";

		[SaveableData] public int healLess = 30;
		[SaveableData] public int heal = 50;
		[SaveableData] public int healGreat = 80;
		[SaveableData] public int healElixir = 1000;

		/// <summary>Permille of maximal stamina.</summary>
		[SaveableData] public int refreshLess = 200;
		[SaveableData] public int refresh = 400;
		[SaveableData] public int refreshGreater = 700;
		[SaveableData] public int refreshElixir = 1000;

		[SaveableData] public int cureLess = 50;
		[SaveableData] public int cure = 200;
		[SaveableData] public int cureGreat = 400;

		[SaveableData] public double nightsightSeconds = 1800;
		[SaveableData] public double invisibilitySeconds = 240;

		[SaveableData] public int explosionLess = 40;
		[SaveableData] public int explosion = 60;
		[SaveableData] public int explosionGreat = 80;
		[SaveableData] public int lava = 100;

		/// <summary>Not in the archive: mine.</summary>
		[SaveableData] public double hpSightLessSeconds = 300;
		[SaveableData] public double hpSightSeconds = 900;
		[SaveableData] public double hpSightGreatSeconds = 1800;
		[SaveableData] public double hpSightElixirSeconds = 3600;

		/// <summary>nastaveni_throw_dist_bomb, _delay.</summary>
		[SaveableData] public int throwRange = 6;
		[SaveableData] public double throwDelaySeconds = 4.5;
	}
}
