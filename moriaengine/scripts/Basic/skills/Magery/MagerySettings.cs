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

		/// <summary>#450 - Mystic's swing speed is mana-driven (sqrt(mana/2) * weapon speed *
		/// this), not the normal sqrt(Dex)-based formula every other profession uses. Placeholder
		/// default - Erebor's real nastaveni_weapspeed_mystik value is a live-shard GM config
		/// constant, not present in the public script archive (same situation #447's own research
		/// flagged for every other Mystic tuning number).</summary>
		[SaveableData]
		public double mysticWeaponSpeedModifier = 1.000; //nastaveni_weapspeed_mystik

		/// <summary>#450 - fraction of current mana retained after each Mystic swing (970 =
		/// retain 97%, i.e. lose 3% per swing). Placeholder default, same reason as
		/// mysticWeaponSpeedModifier above.</summary>
		[SaveableData]
		public double mysticManaLossPermille = 970; //nastaveni_mystik_manaloss

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

