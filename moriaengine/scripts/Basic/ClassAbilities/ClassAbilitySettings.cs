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

		/// <summary>nastaveni_knockback_chance: out of 10000, per point.</summary>
		[SaveableData]
		public int knockbackChance = 1000;

		[SaveableData]
		public double knockbackDelaySeconds = 10;

		[SaveableData]
		public double knockbackEffectSeconds = 10;

		[SaveableData]
		public int knockbackRange = 4;

		[SaveableData]
		public int longRangeStamina = 50;

		[SaveableData]
		public int splitShotStamina = 75;

		[SaveableData]
		public double splitShotDelaySeconds = 45;

		[SaveableData]
		public double splitShotEffectSeconds = 10;

		[SaveableData]
		public int splitShotMaxRange = 4;

		[SaveableData]
		public int splitShotAoeDivisor = 260;

		[SaveableData]
		public int splitShotDamageVsM = 175;

		[SaveableData]
		public int splitShotDamageVsP = 125;

		[SaveableData]
		public int crushingArrowStamina = 25;

		[SaveableData]
		public double crushingArrowDelaySeconds = 1.5;

		[SaveableData]
		public double crushingArrowNextSeconds = 3;

		[SaveableData]
		public int crushingArrowReach = 12;

		/// <summary>nastaveni_crushingArrow_PvM_durationBase and _durationAbilityPoint, as tenths.</summary>
		[SaveableData]
		public double crushingArrowBaseSeconds = 2;

		[SaveableData]
		public double crushingArrowSecondsPerPoint = 0.2;

		/// <summary>nastaveni_ability_resurrectiondelay_fail.</summary>
		[SaveableData]
		public double selfResFailSeconds = 75;

		/// <summary>nastaveni_ability_resurrectiondelay_succes.</summary>
		[SaveableData]
		public double selfResSuccessSeconds = 3600;

		/// <summary>nastaveni_charge_limit: most charges of one school.</summary>
		[SaveableData]
		public int chargeLimit = 3;

		[SaveableData]
		public double chargeDurationSeconds = 15;

		[SaveableData]
		public double chargeDropSeconds = 5;

		/// <summary>nastaveni_charge_baseEff: percent of the bonus with next to no points.</summary>
		[SaveableData]
		public int chargeBaseEffect = 50;

		/// <summary>nastaveni_charge_abilityPoints: points for the whole bonus.</summary>
		[SaveableData]
		public int chargeAbilityPoints = 10;

		/// <summary>nastaveni_charge_spellPow_pvm_ per charge, the same for every spell on Erebor.</summary>
		[SaveableData]
		public int chargeSpellPowerVsM = 200;

		[SaveableData]
		public int chargeSpellPowerVsP = 100;

		/// <summary>nastaveni_charge_silenceBonus: permille of silence's chance per mind charge.</summary>
		[SaveableData]
		public int chargeSilenceBonus = 100;

		/// <summary>nastaveni_MindBlast_silence_chance: out of 1000, per point.</summary>
		[SaveableData]
		public int silenceChance = 60;

		/// <summary>nastaveni_spellcast_silence_mind_blast, in tenths of a second, plus a tenth per point.</summary>
		[SaveableData]
		public int silenceTenths = 15;

		/// <summary>nastaveni_ability_craft_vigour: out of 100000, per point, on top of the level's chance.</summary>
		[SaveableData]
		public int craftVigour = 200;

		/// <summary>nastaveni_thief_backstabP_chance and _backstabM_chance: out of 1000, per point.</summary>
		[SaveableData]
		public int backstabChanceVsP = 48;

		[SaveableData]
		public int backstabChanceVsM = 35;

		/// <summary>nastaveni_thief_backstabP_bonus and _backstabM_bonus: permille of a blow.</summary>
		[SaveableData]
		public int backstabBonusVsP = 1800;

		[SaveableData]
		public int backstabBonusVsM = 1500;

		/// <summary>nastaveni_ability_maskdelay_success and _fail, tenths there.</summary>
		[SaveableData]
		public double maskSuccessSeconds = 900;

		[SaveableData]
		public double maskFailSeconds = 120;

		/// <summary>nastaveni_ability_concentration: permille of a spell's mana a mage's concentration or a
		/// necromancer's darkpower gives back.</summary>
		[SaveableData]
		public int manaBackPermille = 850;

		/// <summary>nastaveni_throw_dist: a knife reaches less far than this, plus a long range's bonus.</summary>
		[SaveableData]
		public int throwDistance = 7;

		[SaveableData]
		public int throwStaminaPerTile = 1;

		[SaveableData]
		public double throwDelaySeconds = 4.5;

		[SaveableData]
		public double throwImmuneSeconds = 2;

		/// <summary>nastaveni_throw_staminaloss: percent of a player's max stamina, less a quarter of its armor.</summary>
		[SaveableData]
		public int throwStaminaLossPercent = 45;

		/// <summary>nastaveni_strikeCharge_roundStamina; _roundStaminaDrop isn't in the save, so it's 0 there.</summary>
		[SaveableData]
		public int strikeStamina = 40;

		/// <summary>nastaveni_strikeCharge_roundBase and _roundBonus: percent of a blow.</summary>
		[SaveableData]
		public int strikeBase = 20;

		[SaveableData]
		public int strikeBonus = 5;

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
