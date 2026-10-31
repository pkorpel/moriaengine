/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class MagerySettings : SettingsMetaCategory {

		[SavedMember]
		public static MagerySettings instance = new MagerySettings();

		[LoadingInitializer]
		public MagerySettings() {
		}

		[SaveableData]
		public double bareHandsMindPowerVsP = 700;

		[SaveableData]
		public double bareHandsMindPowerVsM = 700;

		[SaveableData]
		public double mindPowerIntModifier = 0.700;

		[SaveableData]
		public int astralPowerStaminaCost = 3;

		[SaveableData]
		public int astralPowerStaminaPerSecond = 16; //nastaveni_ability_staminalowering

		/// <summary>#868 - a special cold strike does this much at power 1000, not mysticStrikeEffect.</summary>
		[SaveableData]
		public double mysticSpecialStrikeEffect = 13; //nastaveni_ability_mystikUC

		[SaveableData]
		public double mysticLightningStrikeEffect = 15; //nastaveni_ability_mystikUClightning

		[SaveableData]
		public double mysticLightningPower = 2000; //nastaveni_ability_mystikBleskDmg

		[SaveableData]
		public int mysticFreezeChanceVsP = 300; //nastaveni_mystik_abilitychance_vP

		[SaveableData]
		public int mysticFreezeChanceVsM = 1000; //nastaveni_mystik_abilitychance_vM

		[SaveableData]
		public int mysticSwitchStamina = 20;

		[SaveableData]
		public int mysticCrushingBlowStamina = 30; //nastaveni_mystik_doubleswing_stam

		[SaveableData]
		public double mysticCrushingBlowStrength = 0.750; //nastaveni_mystik_doubleswing_strength

		[SaveableData]
		public int reactiveArmorAbsorbPvP = 10; //nastaveni_reactivea_pvp_absorb

		[SaveableData]
		public int reactiveArmorReturnPvP = 10; //nastaveni_reactivea_pvp_return

		[SaveableData]
		public int reactiveArmorAbsorbPvM = 20; //nastaveni_reactivea_pvm_absorb

		[SaveableData]
		public int reactiveArmorReturnPvM = 10; //nastaveni_reactivea_pvm_return

		[SaveableData]
		public double spellPowerVsM = 2.000; //nastaveni_global_vMsm

		[SaveableData]
		public double spellPowerPvP = 1.000; //nastaveni_global_pvpSM

		[SaveableData]
		public double spellPowerMonster = 1.000; //nastaveni_global_MvSM

		/// <summary>#860 - added to spellPowerVsM per level (up to 200) under Standcast, and per
		/// fourth of the level (at most 50) for field spells.</summary>
		[SaveableData]
		public double fieldSpellPowerPerLevel = 0.020; //nastaveni_standcast_sm

		[SaveableData]
		public double armorMindDefense = 0.800; //nastaveni_global_armorOM

		[SaveableData]
		public double mindDefenseGlobal = 0.900; //nastaveni_global_om

		[SaveableData]
		public double mysticPowerVsM = 1.100; //nastaveni_global_mystikvM / 10

		[SaveableData]
		public double mysticPowerPerLevel = 0.010; //nastaveni_mysticLvL_sm

		/// <summary>#860 - a Mystic strike does this much at power 1000.</summary>
		[SaveableData]
		public double mysticStrikeEffect = 16; //nastaveni_ability_mystikUCbezpary

		/// <summary>#450 - Mystic's swing speed is mana-driven (sqrt(mana/2) * weapon speed *
		/// this), not the normal sqrt(Dex)-based formula every other profession uses. Placeholder
		/// default - Erebor's real nastaveni_weapspeed_mystik value is a live-shard GM config
		/// constant, not present in the public script archive (same situation #447's own research
		/// flagged for every other Mystic tuning number).</summary>
		[SaveableData]
		public double mysticWeaponSpeedModifier = 60; //nastaveni_weapspeed_mystik

		/// <summary>#450 - fraction of current mana retained after each Mystic swing (970 =
		/// retain 97%, i.e. lose 3% per swing). Placeholder default, same reason as
		/// mysticWeaponSpeedModifier above.</summary>
		[SaveableData]
		public double mysticManaLossPermille = 960; //nastaveni_mystik_manaloss

		/// <summary>#483/#484 - multiplier applied to a spell's own Magery skill-requirement
		/// threshold when it's used as a fallback fizzle difficulty (see SpellDef.Difficulty's
		/// own doc comment) for the many spells that never set an explicit difficulty=. Default
		/// 1.0 means "fizzle chance is ~50% exactly at the spell's stated skill requirement" -
		/// tune globally here rather than editing every spell's difficulty individually.</summary>
		[SaveableData]
		public double spellDifficultyFallbackMultiplier = 1.0;

		/// <summary>#483/#489 - Paralyze Field: how often (in seconds) each field tile re-checks
		/// for standing characters and (re-)applies the paralyze effect. No real Erebor value is
		/// recoverable from the public archive (same situation as every other GM-tunable
		/// placeholder this initiative has left as-is) - 1.0s is a simple, testable default.</summary>
		[SaveableData]
		public double paralyzeFieldTickIntervalSeconds = 1.0;

		/// <summary>#483/#489 - Paralyze Field: chance (0.0-1.0) that a given tick actually
		/// paralyzes a character standing in the field. Default 1.0 (always triggers) for
		/// deterministic testing; tune down for difficulty balancing.</summary>
		[SaveableData]
		public double paralyzeFieldParalyzeChance = 1.0;

		/// <summary>#483/#489 - Paralyze Field: duration (in seconds) of the paralyze effect each
		/// tick applies.</summary>
		[SaveableData]
		public double paralyzeFieldParalyzeDurationSeconds = 4.0;

		/// <summary>#483/#491 - Concentration: fraction of casting mana cost discounted per
		/// ability point (real formula confirmed from source - mananeed = mananeed *
		/// nastaveni_ability_concentration / 1000 - but the live-shard per-point value itself
		/// isn't recoverable from the public archive). Default 0.02 (2% per point, 20% at this
		/// ability's own 10-point cap) is a placeholder, same situation as every other
		/// unconfirmable number this initiative has left GM-tunable.</summary>
		[SaveableData]
		public double concentrationDiscountPerPoint = 0.02;

		/// <summary>#483/#491 - Magic Charge: spell-power bonus fraction per stacked charge point
		/// (max 10) in the charge's own school, applied to that school's other spells. Live-shard
		/// value unconfirmable from the public archive - placeholder, same as above.</summary>
		[SaveableData]
		public double magicChargeBonusPerPoint = 0.02;

		/// <summary>#483/#491 - Magic Charge: seconds between each -1 decay tick once a school has
		/// at least 1 charge. Placeholder default.</summary>
		[SaveableData]
		public double magicChargeDecayIntervalSeconds = 10.0;

		public MetaMassSetting<WeaponMindPowerVsMMassSetting, ColoredWeaponDef, double> mindPowerVsM = new MetaMassSetting<WeaponMindPowerVsMMassSetting, ColoredWeaponDef, double>();

		public MetaMassSetting<WeaponMindPowerVsPMassSetting, ColoredWeaponDef, double> mindPowerVsP = new MetaMassSetting<WeaponMindPowerVsPMassSetting, ColoredWeaponDef, double>();

		public SpellsSettings spells = new SpellsSettings();

		[InfoField("Maxmana hol�")]
		public StaffMaxManaMassSetting allSpells = new StaffMaxManaMassSetting();
	}

	[ViewableClass]
	public class SpellsSettings : SettingsMetaCategory {
		[InfoField("damage kouzel")]
		public SpellDamageMassSetting spellDamage = new SpellDamageMassSetting();

		[InfoField("typ damage kouzel")]
		public SpellDamageTypeMassSetting damageType = new SpellDamageTypeMassSetting();

		[InfoField("v�echny kouzla")]
		public AllSpellsMassSetting allSpells = new AllSpellsMassSetting();
	}

	public class WeaponMindPowerVsPMassSetting : MassSettingByMaterial<ColoredWeaponDef, double> {

		public override string Name {
			get { return "S�la mysli proti hr���m"; }
		}

		protected class WeaponvVsPFieldView : FieldView_ByModel {
			internal WeaponvVsPFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, double value) {
				def.MindPowerVsP = value;
			}

			internal override double GetValue(ColoredWeaponDef def) {
				return def.MindPowerVsP;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponvVsPFieldView(index);
		}
	}

	public class WeaponMindPowerVsMMassSetting : MassSettingByMaterial<ColoredWeaponDef, double> {

		public override string Name {
			get { return "S�la mysli proti monstr�m"; }
		}

		protected class WeaponMindPowerVsMFieldView : FieldView_ByModel {
			internal WeaponMindPowerVsMFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, double value) {
				def.MindPowerVsM = value;
			}

			internal override double GetValue(ColoredWeaponDef def) {
				return def.MindPowerVsM;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponMindPowerVsMFieldView(index);
		}
	}

	public abstract class SpellDefEffectMassSetting<DefType> : MassSettings_ByClass_SingleField<DefType, double[]> where DefType : SpellDef {
		protected class SpellDefEffectFieldView : FieldView_ByClass_SingleField {
			internal SpellDefEffectFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(DefType def, double[] value) {
				def.Effect = value;
			}

			internal override double[] GetValue(DefType def) {
				return def.Effect;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new SpellDefEffectFieldView(index);
		}
	}

	public class SpellDamageMassSetting : SpellDefEffectMassSetting<DamageSpellDef> {

		public override string Name {
			get { return "Damage kouzel (pole effect)"; }
		}
	}

	public class SpellDamageTypeMassSetting : MassSettings_ByClass_SingleField<DamageSpellDef, DamageType> {

		public override string Name {
			get { return "Typ Damage kouzel"; }
		}

		protected class SpellDamageTypeFieldView : FieldView_ByClass_SingleField {
			internal SpellDamageTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(DamageSpellDef def, DamageType value) {
				def.DamageType = value;
			}

			internal override DamageType GetValue(DamageSpellDef def) {
				return def.DamageType;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new SpellDamageTypeFieldView(index);
		}
	}
	
	public class AllSpellsMassSetting : MassSettings_ByClass_List<SpellDef> {
		
		public override string Name {
			get { return "Seznam v�ech kouzel"; }
		}
	}

	public class StaffMaxManaMassSetting : MassSettings_ByClass_SingleField<ColoredStaffDef, int> {

		public override string Name {
			get { return "Maxmana hol�"; }
		}

		protected class StaffMaxManaFieldView : FieldView_ByClass_SingleField {
			internal StaffMaxManaFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredStaffDef def, int value) {
				def.MaxMana = value;
			}

			internal override int GetValue(ColoredStaffDef def) {
				return def.MaxMana;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new StaffMaxManaFieldView(index);
		}
	}
}

