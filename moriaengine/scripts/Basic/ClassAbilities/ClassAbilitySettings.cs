using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - Erebor's nastaveni_ numbers for class abilities, as "Class Abilities" in .settings.
	/// Seconds here are Erebor's tenths divided by ten.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class ClassAbilitySettings : SettingsMetaCategory {

		[SavedMember]
		public static ClassAbilitySettings instance = new ClassAbilitySettings();

		[LoadingInitializer]
		public ClassAbilitySettings() {
		}

		/// <summary>nastaveni_ability_berssniper: permille of a blow when berserker or sniper lands.</summary>
		[SaveableData]
		public int criticalPermille = 1800;

		/// <summary>nastaveni_roundattack_chance: out of 10000, per point.</summary>
		[SaveableData]
		public int roundAttackChance = 1000;

		[SaveableData]
		public double roundAttackDelaySeconds = 60;

		/// <summary>nastaveni_roundattack_postih: percent of a blow each one around takes.</summary>
		[SaveableData]
		public int roundAttackDamagePercent = 80;

		/// <summary>nastaveni_roundattack_staminacost isn't in the archive's save, so it's 0 there.</summary>
		[SaveableData]
		public int roundAttackStamina = 0;

		/// <summary>nastaveni_ability_leapchange: out of 10000, per point.</summary>
		[SaveableData]
		public int leapChance = 950;

		[SaveableData]
		public double leapDelaySeconds = 3.5;

		[SaveableData]
		public int leapRange = 5;

		[SaveableData]
		public int leapStaminaPerTile = 0;

		/// <summary>nastaveni_warcry_chance: out of 10000, per point.</summary>
		[SaveableData]
		public int warcryChance = 1000;

		[SaveableData]
		public double warcryDelaySeconds = 60;

		[SaveableData]
		public int warcryRange = 15;

		/// <summary>nastaveni_doubleswing: permille of a blow.</summary>
		[SaveableData]
		public int doubleSwingPermille = 1500;

		[SaveableData]
		public int doubleSwingStamina = 20;

		[SaveableData]
		public int doubleSwingMinLevel = 30;

		/// <summary>nastaveni_defstance_dmgplayer and _dmgnpc: percent off per ten points.</summary>
		[SaveableData]
		public int defStanceDamagePercent = 25;

		[SaveableData]
		public int singleTauntStamina = 20;

		[SaveableData]
		public int massTauntStamina = 50;

		[SaveableData]
		public double singleTauntDelaySeconds = 60;

		[SaveableData]
		public double massTauntDelaySeconds = 180;

		[SaveableData]
		public double tauntDelayPenaltySeconds = 5;

		[SaveableData]
		public int tauntRange = 10;
	}
}
