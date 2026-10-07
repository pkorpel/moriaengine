using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
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

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class CombatSettings : SettingsMetaCategory {

		[SavedMember]
		public static CombatSettings instance = new CombatSettings();

		[LoadingInitializer]
		public CombatSettings() {
		}

		/// <summary>How long should a character remember it's combat targets?</summary>
		[SaveableData]
		public double secondsToRememberTargets = 300;

		[SaveableData]
		public double bareHandsAttackVsP = 10;

		[SaveableData]
		public double bareHandsAttackVsM = 10;

		[SaveableData]
		public double bareHandsPiercing = 100;

		[SaveableData]
		public double bareHandsSpeed = 100;

		[SaveableData]
		public int bareHandsRange = 1;

		[SaveableData]
		public int bareHandsStrikeStartRange = 5;

		[SaveableData]
		public int bareHandsStrikeStopRange = 10;

		[SaveableData]
		public double weaponSpeedGlobal = 1.000;

		[SaveableData]
		public double weaponSpeedNPC = 0.150;

		[SaveableData]
		public double attackStrModifier = 3.170;

		[SaveableData]
		public double weapAttack = 1.100; //nastaveni_global_UC

		[SaveableData]
		public double weapAttackM = 1.000; //nastaveni_global_MUC

		[SaveableData]
		public double weapAttackVsM = 1.600; //nastaveni_global_vMweapattack

		[SaveableData]
		public double weapAttackPvP = 0.900; //nastaveni_global_PvPweapattack

		[SaveableData]
		public double armorClassP = 0.700; //nastaveni_global_Parmor

		[SaveableData]
		public double armorClassM = 1.000; //nastaveni_global_Marmor

		[SaveableData]
		public double armorClassMvP = 200; //nastaveni_global_MvP_armorfactor

		/// <summary>Every case other than (M is attacker, P is defender)</summary>
		[SaveableData]
		public double armorRandEffectMin = 0.945; //skill_parrying.effect

		/// <summary>Every case other than (M is attacker, P is defender)</summary>
		[SaveableData]
		public double armorRandEffectMax = 1.050; //skill_parrying.effect

		[SaveableData]
		public double swingDamagePvP = 0.900; //nastaveni_global_PvPweapdam

		[SaveableData]
		public double swingDamageVsM = 1.350; //nastaveni_global_vMweapdam

		[SaveableData]
		public double swingDamageM = 1.0; //nastaveni_global_Mvweapdam

		[SaveableData]
		public double swingDamageRandMvPMin = 0.850; //nastaveni_global_MvP_randomfactor

		[SaveableData]
		public double swingDamageRandMvPMax = 1.000; //nastaveni_global_MvP_randomfactor		

		public WeaponTypeMassSetting weaponTypes = new WeaponTypeMassSetting();

		public WeaponSpeedMassSetting weaponSpeeds = new WeaponSpeedMassSetting();

		public WeaponRangeVsMMassSetting weaponRangesVsM = new WeaponRangeVsMMassSetting();
		public WeaponRangeVsPMassSetting weaponRangesVsP = new WeaponRangeVsPMassSetting();

		public WeaponStrikeStartRangeMassSetting weaponStrikeStartRanges = new WeaponStrikeStartRangeMassSetting();
		public WeaponStrikeStopRangeMassSetting weaponStrikeStopRanges = new WeaponStrikeStopRangeMassSetting();

		public WeaponAnimTypeSetting weaponAnims = new WeaponAnimTypeSetting();

		public WeaponMaterialTypeMassSetting weaponMaterialTypes = new WeaponMaterialTypeMassSetting();

		public MetaMassSetting<WeaponAttackVsMMassSetting, ColoredWeaponDef, double> weaponsAttackVsM = new MetaMassSetting<WeaponAttackVsMMassSetting, ColoredWeaponDef, double>();

		public MetaMassSetting<WeaponAttackVsPMassSetting, ColoredWeaponDef, double> weaponsAttackVsP = new MetaMassSetting<WeaponAttackVsPMassSetting, ColoredWeaponDef, double>();

		public WearableLayerMassSetting wearableLayers = new WearableLayerMassSetting();

		public ArmorVsPMassSetting armorVsP = new ArmorVsPMassSetting();
		public ArmorVsMMassSetting armorVsM = new ArmorVsMMassSetting();

		public MindDefenseVsPMassSetting mindDefVsP = new MindDefenseVsPMassSetting();
		public MindDefenseVsMMassSetting mindDefVsM = new MindDefenseVsMMassSetting();

		public WeaponProjectileAnimMassSetting weaponProjectileAnimations = new WeaponProjectileAnimMassSetting();
		public WeaponProjectileType weaponProjectileTypes = new WeaponProjectileType();
		public ProjectilePiercingMassSetting projectilePiercings = new ProjectilePiercingMassSetting();
		public ProjectileTypeMassSetting projectileTypes = new ProjectileTypeMassSetting();
	}

	public class WeaponTypeMassSetting : MassSettings_ByModel<WeaponDef, WeaponType> {
		public override string Name {
			get {
				return "Typy zbraní";
			}
		}

		protected class WeaponTypeFieldView : FieldView_ByModel {
			internal WeaponTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, WeaponType value) {
				def.WeaponType = value;
				switch (value) {
					case WeaponType.TwoHandBlunt:
					case WeaponType.TwoHandSpike:
					case WeaponType.TwoHandSword:
					case WeaponType.TwoHandAxe:
					case WeaponType.XBow:
					case WeaponType.Bow:
						def.Layer = 2;
						def.TwoHanded = true;
						break;
					case WeaponType.OneHandBlunt:
					case WeaponType.OneHandSpike:
					case WeaponType.OneHandSword:
					case WeaponType.OneHandAxe:
						def.Layer = 1;
						def.TwoHanded = false;
						break;
				}
			}

			internal override WeaponType GetValue(WeaponDef def) {
				return def.WeaponType;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponTypeFieldView(index);
		}
	}


	public class WeaponRangeVsMMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Dostøely/dosahy zbraní vs M";
			}
		}

		protected class WeaponRangeVsMFieldView : FieldView_ByModel {
			internal WeaponRangeVsMFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.RangeVsM = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.RangeVsM;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponRangeVsMFieldView(index);
		}
	}

	public class WeaponRangeVsPMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Dostøely/dosahy zbraní vs P";
			}
		}

		protected class WeaponRangeVsPFieldView : FieldView_ByModel {
			internal WeaponRangeVsPFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.RangeVsP = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.RangeVsP;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponRangeVsPFieldView(index);
		}
	}

	public class WeaponStrikeStartRangeMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Dostøely/dosahy zbraní - minimum pro zaèátek nápøahu";
			}
		}

		protected class WeaponStrikeStartRangeFieldView : FieldView_ByModel {
			internal WeaponStrikeStartRangeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.StrikeStartRange = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.StrikeStartRange;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponStrikeStartRangeFieldView(index);
		}
	}

	public class WeaponStrikeStopRangeMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Dostøely/dosahy zbraní - maximum pro trvání nápøahu";
			}
		}

		protected class WeaponStrikeStopRangeFieldView : FieldView_ByModel {
			internal WeaponStrikeStopRangeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.StrikeStopRange = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.StrikeStopRange;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponStrikeStopRangeFieldView(index);
		}
	}

	public class WeaponSpeedMassSetting : MassSettings_ByModel<WeaponDef, double> {
		public override string Name {
			get {
				return "Rychlosti zbraní";
			}
		}

		protected class WeaponSpeedFieldView : FieldView_ByModel {
			internal WeaponSpeedFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, double value) {
				def.Speed = value;
			}

			internal override double GetValue(WeaponDef def) {
				return def.Speed;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponSpeedFieldView(index);
		}
	}

	public class WeaponAnimTypeSetting : MassSettings_ByModel<WeaponDef, WeaponAnimType> {
		public override string Name {
			get {
				return "Typy animace zbraní";
			}
		}

		protected class WeaponAnimTypeFieldView : FieldView_ByModel {
			internal WeaponAnimTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, WeaponAnimType value) {
				def.WeaponAnimType = value;
			}

			internal override WeaponAnimType GetValue(WeaponDef def) {
				WeaponAnimType anim = def.WeaponAnimType;
				if (anim == WeaponAnimType.Undefined) {
					anim = TranslateAnimType(def.WeaponType);
				}
				return anim;
			}
		}

		public static WeaponAnimType TranslateAnimType(WeaponType type) {
			WeaponAnimType anim = WeaponAnimType.Undefined;
			switch (type) {
				case WeaponType.BareHands:
					anim = WeaponAnimType.BareHands;
					break;
				case WeaponType.XBow:
					anim = WeaponAnimType.XBow;
					break;
				case WeaponType.Bow:
					anim = WeaponAnimType.Bow;
					break;
				case WeaponType.OneHandAxe:
				case WeaponType.OneHandSpike:
				case WeaponType.OneHandSword:
				case WeaponType.OneHandBlunt:
					anim = WeaponAnimType.HeldInRightHand;
					break;
				case WeaponType.TwoHandSword:
				case WeaponType.TwoHandBlunt:
				case WeaponType.TwoHandSpike:
				case WeaponType.TwoHandAxe:
					anim = WeaponAnimType.HeldInLeftHand;
					break;
			}
			return anim;
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponAnimTypeFieldView(index);
		}
	}

	public class WeaponMaterialTypeMassSetting : MassSettings_ByModel<ColoredWeaponDef, MaterialType> {
		public override string Name {
			get {
				return "Typ materiálu zbraní";
			}
		}

		protected class WeaponMaterialTypeFieldView : FieldView_ByModel {
			internal WeaponMaterialTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, MaterialType value) {
				def.MaterialType = value;
			}

			internal override MaterialType GetValue(ColoredWeaponDef def) {
				return def.MaterialType;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponMaterialTypeFieldView(index);
		}
	}

	public class WeaponAttackVsPMassSetting : MassSettingByMaterial<ColoredWeaponDef, double> {

		public override string Name {
			get { return "Útok proti hráèùm"; }
		}

		protected class WeaponAttackVsPFieldView : FieldView_ByModel {
			internal WeaponAttackVsPFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, double value) {
				def.AttackVsP = value;
			}

			internal override double GetValue(ColoredWeaponDef def) {
				return def.AttackVsP;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponAttackVsPFieldView(index);
		}
	}

	public class WeaponAttackVsMMassSetting : MassSettingByMaterial<ColoredWeaponDef, double> {

		public override string Name {
			get { return "Útok proti monstrùm"; }
		}

		protected class WeaponAttackVsMFieldView : FieldView_ByModel {
			internal WeaponAttackVsMFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredWeaponDef def, double value) {
				def.AttackVsM = value;
			}

			internal override double GetValue(ColoredWeaponDef def) {
				return def.AttackVsM;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponAttackVsMFieldView(index);
		}
	}

	public class WearableLayerMassSetting : MassSettings_ByModel<WearableDef, LayerNames> {
		public override string Name {
			get {
				return "Layery brnìní/obleèení";
			}
		}

		protected class WearableLayerFieldView : FieldView_ByModel {
			internal WearableLayerFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WearableDef def, LayerNames value) {
				def.Layer = (byte) value;
			}

			internal override LayerNames GetValue(WearableDef def) {
				return (LayerNames) def.Layer;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WearableLayerFieldView(index);
		}
	}

	public class ArmorVsPMassSetting : MassSettings_ByWearableTypeAndMaterial<ColoredArmorDef, int> {

		public override string Name {
			get { return "Armor proti hráèùm"; }
		}

		protected class ArmorVsPFieldView : FieldView_ByWearableTypeAndMaterial {
			internal ArmorVsPFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredArmorDef def, int value) {
				def.ArmorVsP = value;
			}

			internal override int GetValue(ColoredArmorDef def) {
				return def.ArmorVsP;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ArmorVsPFieldView(index);
		}
	}

	public class ArmorVsMMassSetting : MassSettings_ByWearableTypeAndMaterial<ColoredArmorDef, int> {

		public override string Name {
			get { return "Armor proti monstrùm"; }
		}

		protected class ArmorVsMFieldView : FieldView_ByWearableTypeAndMaterial {
			internal ArmorVsMFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredArmorDef def, int value) {
				def.ArmorVsM = value;
			}

			internal override int GetValue(ColoredArmorDef def) {
				return def.ArmorVsM;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ArmorVsMFieldView(index);
		}
	}

	public class MindDefenseVsPMassSetting : MassSettings_ByWearableTypeAndMaterial<ColoredArmorDef, int> {

		public override string Name {
			get { return "Obrana mysli hráèùm"; }
		}

		protected class MindDefenseVsPFieldView : FieldView_ByWearableTypeAndMaterial {
			internal MindDefenseVsPFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredArmorDef def, int value) {
				def.MindDefenseVsP = value;
			}

			internal override int GetValue(ColoredArmorDef def) {
				return def.MindDefenseVsP;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new MindDefenseVsPFieldView(index);
		}
	}

	public class MindDefenseVsMMassSetting : MassSettings_ByWearableTypeAndMaterial<ColoredArmorDef, int> {

		public override string Name {
			get { return "Obrana mysli monstrùm"; }
		}

		protected class MindDefenseVsMFieldView : FieldView_ByWearableTypeAndMaterial {
			internal MindDefenseVsMFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ColoredArmorDef def, int value) {
				def.MindDefenseVsM = value;
			}

			internal override int GetValue(ColoredArmorDef def) {
				return def.MindDefenseVsM;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new MindDefenseVsMFieldView(index);
		}
	}

	public class WeaponProjectileType : MassSettings_ByModel<WeaponDef, ProjectileType> {
		public override string Name {
			get {
				return "Typ projektilù pro zbranì";
			}
		}

		protected class ProjectileTypeFieldView : FieldView_ByModel {
			internal ProjectileTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, ProjectileType value) {
				def.ProjectileType = value;
			}

			internal override ProjectileType GetValue(WeaponDef def) {
				return def.ProjectileType;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectileTypeFieldView(index);
		}
	}

	public class WeaponProjectileAnimMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Animace projektilù pro zbranì";
			}
		}

		protected class WeaponProjectileAnimFieldView : FieldView_ByModel {
			internal WeaponProjectileAnimFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.ProjectileAnim = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.ProjectileAnim;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponProjectileAnimFieldView(index);
		}
	}

	public class ProjectileTypeMassSetting : MassSettings_ByModel<ProjectileDef, ProjectileType> {
		public override string Name {
			get {
				return "Typ projektilù";
			}
		}

		protected class ProjectileTypeFieldView : FieldView_ByModel {
			internal ProjectileTypeFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ProjectileDef def, ProjectileType value) {
				def.ProjectileType = value;
			}

			internal override ProjectileType GetValue(ProjectileDef def) {
				return def.ProjectileType;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectileTypeFieldView(index);
		}
	}

	public class ProjectilePiercingMassSetting : MassSettings_ByClass_ThingDef<ProjectileDef, double> {
		public override string Name {
			get {
				return "Piercing projektilù";
			}
		}

		protected class ProjectilePiercingFieldView : FieldView_ByClass_ThingDef {
			internal ProjectilePiercingFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ProjectileDef def, double value) {
				def.Piercing = value;
			}

			internal override double GetValue(ProjectileDef def) {
				return def.Piercing;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectilePiercingFieldView(index);
		}
	}
}

