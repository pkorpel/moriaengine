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

		/// <summary>Opt-in diagnostic combat messages (too far, lost LOS/interaction, hit/miss,
		/// damage + who caused it) - surfaces combat-flow events that otherwise have zero
		/// player-facing feedback today, for live-testing new combat mechanics. Off by default -
		/// this is a debug aid, not a shipped UX feature.</summary>
		[SaveableData]
		public bool showCombatMessages = false;

		[SaveableData]
		public double bareHandsAttackVsP = 5; //nastaveni_weapdam_barehands

		[SaveableData]
		public double bareHandsAttackVsM = 5; //nastaveni_weapdam_barehands

		[SaveableData]
		public double bareHandsPiercing = 100;

		[SaveableData]
		public double bareHandsSpeed = 200; //nastaveni_weapspeed_barehands

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

		/// <summary>#860 - permille of monsters' physical blows dodged per point of a_weapon_dodge.</summary>
		[SaveableData]
		public int weaponDodgePerPoint = 10; //nastaveni_ability_pvMdodge

		/// <summary>#860 - added to weapAttackVsM per player level, combat_main_nonmystik_countDmg.</summary>
		[SaveableData]
		public double weapAttackVsMPerLevel = 0.010;

		[SaveableData]
		public int weaponWearPerHit = 60; //nastaveni_global_physIdam

		[SaveableData]
		public int staffWearPerSpell = 50; //nastaveni_global_magicIdam

		[SaveableData]
		public int armorWearPerDamage = 180; //nastaveni_global_armorIdam

		/// <summary>#866 - e_vigilance: the share of a thief's blow on a monster left in the stance.</summary>
		[SaveableData]
		public double vigilanceOwnDamage = 0.700;

		/// <summary>#872 - a thief's bleed cap, plus their level up to 200.</summary>
		[SaveableData]
		public int thiefBleedCap = 300; //nastaveni_thief_bleed_cap

		/// <summary>#872 - permille chance of a bleed's tick interrupting casting (chance_to_fizz).</summary>
		[SaveableData]
		public int bleedFizzleChance = 668;

		[SaveableData]
		public int weapAttackVsMMaxLevel = 200;

		/// <summary>#860 - Erebor ran on Windows, where the world ticked about 9.14 times a
		/// second, not 10 (EreborNext's clock probe). Hitspeed is in those ticks.</summary>
		[SaveableData]
		public double swingTicksPerSecond = 9.14;

		[SaveableData]
		public double weapAttackPvP = 0.900; //nastaveni_global_PvPweapattack

		[SaveableData]
		public double armorClassP = 0.700; //nastaveni_global_Parmor

		[SaveableData]
		public double armorClassM = 1.000; //nastaveni_global_Marmor

		[SaveableData]
		public double armorClassMvP = 200; //nastaveni_global_MvP_armorfactor

		/// <summary>#96 fix - dead, intentionally unused (not a mistake - see
		/// CombatCalculator.CalculateArmorClassEffect's own comment). Used to unconditionally
		/// discard the real armor value for every case except monster-attacks-player; the fix
		/// is to stop discarding it, not to invent a new formula using these. Left declared
		/// (not removed) purely because this engine's reflection-based ViewableClass/
		/// SaveableClass code generator writes a position-indexed Generated/*.Generated.cs file
		/// that would need hand-renumbering across potentially thousands of entries if a field
		/// were deleted outright - same reasoning armorClassMvP above is already left unused.</summary>
		[SaveableData]
		public double armorRandEffectMin = 0.945;

		/// <summary>#96 fix - see armorRandEffectMin just above.</summary>
		[SaveableData]
		public double armorRandEffectMax = 1.050;

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

		/// <summary>Attack ranges that are a property of the attacker (profession/combat style),
		/// not of whatever weapon happens to be equipped - the existing weaponRangesVsM/VsP mass
		/// settings above are by weapon MODEL, which doesn't fit Mystic's mind-power attack:
		/// its real reach is the attacker's own mind power, confirmed from the Erebor source as
		/// a profession-level constant (mystikrangepvp/pvm), not a per-staff-model stat. Mystic
		/// is the only entry for now (#447 follow-up) - more profession-specific ranges can be
		/// added here later as they come up, same shape, rather than inventing a new page.</summary>
		[InfoField("Ranges")]
		public RangesSettings Ranges {
			get {
				return RangesSettings.instance;
			}
		}
	}

	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class RangesSettings : SettingsMetaCategory {

		[SavedMember]
		public static RangesSettings instance = new RangesSettings();

		[LoadingInitializer]
		public RangesSettings() {
		}

		/// <summary>#447 follow-up - placeholder default (Erebor's real mystikrangepvp/pvm
		/// values are live-shard GM config, not in the public script archive, same situation
		/// every other Mystic tuning number hit). Used for both RangeVsP and RangeVsM - Erebor's
		/// source actually has separate pvp/pvm range constants, but a single shared value is
		/// enough for now; split into two fields here if that distinction ever matters.</summary>
		[SaveableData]
		public int mystic = 8; //nastaveni_global_mystikrangepvm

		[SaveableData]
		public int mysticVsPlayers = 7; //nastaveni_global_mystikrangepvp

		/// <summary>#454 follow-up - placeholder default (same situation as mystic above -
		/// Erebor's real range constants are live-shard GM config). Applies to any weapon with
		/// WeaponType.Bow, regardless of profession.</summary>
		[SaveableData]
		public int bow = 8; //nastaveni_global_runbowrangepvm

		[SaveableData]
		public int bowVsPlayers = 7; //nastaveni_global_Runbowrangepvp

		/// <summary>#454 follow-up - placeholder default. Applies to WeaponType.XBow weapons
		/// whose defname does NOT contain "heavy" (see CombatCalculator.CalculateCombatWeaponValues
		/// for why defname, not a dedicated field, distinguishes light from heavy crossbow).</summary>
		[SaveableData]
		public int crossbow = 8;

		/// <summary>#454 follow-up - placeholder default. Applies to WeaponType.XBow weapons
		/// whose defname contains "heavy" (i_crossbow_heavy_* across all 7 material tiers).</summary>
		[SaveableData]
		public int heavyCrossbow = 8;
	}

	public class WeaponTypeMassSetting : MassSettings_ByModel<WeaponDef, WeaponType> {
		public override string Name {
			get {
				return "Typy zbrani";
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
				return "Dostrely/dosahy zbrani vs M";
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
				return "Dostrely/dosahy zbrani vs P";
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
				return "Dostrely/dosahy zbrani - minimum pro zacatek naprahu";
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
				return "Dostrely/dosahy zbrani - maximum pro trvani naprahu";
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
				return "Rychlosti zbrani";
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
				return "Typy animace zbrani";
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
				return "Typ materialu zbrani";
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
			get { return "Utok proti hracum"; }
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
			get { return "Utok proti monstrum"; }
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
				return "Layery brneni/obleceni";
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
			get { return "Armor proti hracum"; }
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
			get { return "Armor proti monstrum"; }
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
			get { return "Obrana mysli hracum"; }
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
			get { return "Obrana mysli monstrum"; }
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
				return "Typ projektilu pro zbrane";
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
				return "Animace projektilu pro zbrane";
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
				return "Typ projektilu";
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
				return "Piercing projektilu";
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

