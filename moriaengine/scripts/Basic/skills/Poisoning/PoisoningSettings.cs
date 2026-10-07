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

namespace SteamEngine.CompiledScripts {
	//[SaveableClass]
	//[HasSavedMembers]
	[ViewableClass]
	public class PoisoningSettings : SettingsMetaCategory {

		//[SavedMember]
		public static PoisoningSettings instance = new PoisoningSettings();

		//[LoadingInitializer]

		public AllFadingEffectsMassSetting poisonTypes = new AllFadingEffectsMassSetting();

		public FadingEffectTickIntervalMassSetting tickIntervalsPerType = new FadingEffectTickIntervalMassSetting();
		public FadingEffectMaxTicksMassSetting maxTicksPerType = new FadingEffectMaxTicksMassSetting();
		public FadingEffectMaxPowerMassSetting maxPowerPerType = new FadingEffectMaxPowerMassSetting();


		public AllPoisonPotionsMassSetting poisonPotions = new AllPoisonPotionsMassSetting();

		public PoisonPotionPowerMassSetting powerPerPotion = new PoisonPotionPowerMassSetting();
		public PoisonPotionTickCountMassSetting ticksPerPotion = new PoisonPotionTickCountMassSetting();


		public ProjectilePoisoningDifficultyMassSetting difficultyPerProjectile = new ProjectilePoisoningDifficultyMassSetting();
		public ProjectilePoisoningEfficiencyMassSetting efficiencyPerProjectile = new ProjectilePoisoningEfficiencyMassSetting();

		public WeaponPoisoningDifficultyMassSetting difficultyPerWeaponModel = new WeaponPoisoningDifficultyMassSetting();
		public WeaponPoisoningEfficiencyMassSetting efficiencyPerWeaponModel = new WeaponPoisoningEfficiencyMassSetting();
		public WeaponPoisoningCapacityMassSetting capacityPerWeaponModel = new WeaponPoisoningCapacityMassSetting();

	}


	public class AllFadingEffectsMassSetting : MassSettings_ByClass_List<FadingEffectDurationPluginDef> {

		public override string Name {
			get { return "Všechny 'fading' efekty (bleeding, jedy, apod.)"; }
		}
	}

	public class FadingEffectTickIntervalMassSetting :
		MassSettings_ByClass_SingleField<FadingEffectDurationPluginDef, double> {

		public override string Name { get { return "Tick intervaly podle typu efektu v sekundách"; } }

		protected class PoisonTypeTickIntervalFieldView : FieldView_ByClass_SingleField {
			internal PoisonTypeTickIntervalFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(FadingEffectDurationPluginDef def, double value) {
				def.TickInterval = value;
			}

			internal override double GetValue(FadingEffectDurationPluginDef def) {
				return def.TickInterval;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new PoisonTypeTickIntervalFieldView(index);
		}
	}

	public class FadingEffectMaxTicksMassSetting :
		MassSettings_ByClass_SingleField<FadingEffectDurationPluginDef, int> {

		public override string Name { get { return "Max poèet tickù podle typu efektu, tj. sèítací strop trvání"; } }

		protected class MaxTicksFieldView : FieldView_ByClass_SingleField {
			internal MaxTicksFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(FadingEffectDurationPluginDef def, int value) {
				def.MaxTicks = value;
			}

			internal override int GetValue(FadingEffectDurationPluginDef def) {
				return def.MaxTicks;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new MaxTicksFieldView(index);
		}
	}

	public class FadingEffectMaxPowerMassSetting :
		MassSettings_ByClass_SingleField<FadingEffectDurationPluginDef, double> {

		public override string Name { get { return "Max power podle typu efektu, tj. sèítací strop. Použit pøi opakované aplikaci stejneho efektu na jednoho chudáka."; } }

		protected class MaxPowerFieldView : FieldView_ByClass_SingleField {
			internal MaxPowerFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(FadingEffectDurationPluginDef def, double value) {
				def.MaxPower = value;
			}

			internal override double GetValue(FadingEffectDurationPluginDef def) {
				return def.MaxPower;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new MaxPowerFieldView(index);
		}
	}

	public class AllPoisonPotionsMassSetting : MassSettings_ByClass_List<PoisonPotionDef> {

		public override string Name {
			get { return "Všechny poison potiony"; }
		}
	}

	public class PoisonPotionPowerMassSetting :
		MassSettings_ByClass_SingleField<PoisonPotionDef, int[]> {

		public override string Name { get { return "Power jedovatých potionù - dvojèíslí, pøi použití se bere náhodná hodnota v rozmezí, vèetnì."; } }

		protected class PoisonPotionPowerFieldView : FieldView_ByClass_SingleField {
			internal PoisonPotionPowerFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonPotionDef def, int[] value) {
				def.PoisonPower = value;
			}

			internal override int[] GetValue(PoisonPotionDef def) {
				return def.PoisonPower;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new PoisonPotionPowerFieldView(index);
		}
	}

	public class PoisonPotionTickCountMassSetting :
		MassSettings_ByClass_SingleField<PoisonPotionDef, int> {

		public override string Name { get { return "Poèet tikù jedovatých potionù"; } }

		protected class PoisonPotionTickCountFieldView : FieldView_ByClass_SingleField {
			internal PoisonPotionTickCountFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(PoisonPotionDef def, int value) {
				def.PoisonTickCount = value;
			}

			internal override int GetValue(PoisonPotionDef def) {
				return def.PoisonTickCount;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new PoisonPotionTickCountFieldView(index);
		}
	}

	public class ProjectilePoisoningEfficiencyMassSetting :
		MassSettings_ByClass_SingleField<ProjectileDef, double> {

		public override string Name {
			get {
				return "Efektivita poisoningu pro jednotlivé druhy projektilù. 1.0 = 100% ";
			}
		}

		protected class ProjectilePoisoningEfficiencyFieldView : FieldView_ByClass_SingleField {
			internal ProjectilePoisoningEfficiencyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ProjectileDef def, double value) {
				def.PoisoningEfficiency = value;
			}

			internal override double GetValue(ProjectileDef def) {
				return def.PoisoningEfficiency;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectilePoisoningEfficiencyFieldView(index);
		}
	}

	public class ProjectilePoisoningDifficultyMassSetting :
		MassSettings_ByClass_SingleField<ProjectileDef, int> {

		public override string Name {
			get {
				return "Obtížnost poisoningu pro jednotlivé druhy projektilù. 0 = nelze otrávit, 1000 = 100% ";
			}
		}

		protected class ProjectilePoisoningDifficultyFieldView : FieldView_ByClass_SingleField {
			internal ProjectilePoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(ProjectileDef def, int value) {
				def.PoisoningDifficulty = value;
			}

			internal override int GetValue(ProjectileDef def) {
				return def.PoisoningDifficulty;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new ProjectilePoisoningDifficultyFieldView(index);
		}
	}

	public class WeaponPoisoningDifficultyMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Obtížnost poisoningu pro zbranì dle modelu. 0 = nelze otrávit, 1000 = 100% ";
			}
		}

		protected class WeaponPoisoningDifficultyFieldView : FieldView_ByModel {
			internal WeaponPoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.PoisoningDifficulty = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.PoisoningDifficulty;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponPoisoningDifficultyFieldView(index);
		}
	}

	public class WeaponPoisoningEfficiencyMassSetting : MassSettings_ByModel<WeaponDef, double> {
		public override string Name {
			get {
				return "Efektivita poisoningu pro zbranì dle modelu. 1.0 = 100% ";
			}
		}

		protected class WeaponPoisoningDifficultyFieldView : FieldView_ByModel {
			internal WeaponPoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, double value) {
				def.PoisoningEfficiency = value;
			}

			internal override double GetValue(WeaponDef def) {
				return def.PoisoningEfficiency;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponPoisoningDifficultyFieldView(index);
		}
	}

	public class WeaponPoisoningCapacityMassSetting : MassSettings_ByModel<WeaponDef, int> {
		public override string Name {
			get {
				return "Poisoning kapacita zbraní - Kolikrát lze udeøit než se jed na zbrani vyèerpá ";
			}
		}

		protected class WeaponPoisoningDifficultyFieldView : FieldView_ByModel {
			internal WeaponPoisoningDifficultyFieldView(int index)
				: base(index) {
			}

			internal override void SetValue(WeaponDef def, int value) {
				def.PoisonCapacity = value;
			}

			internal override int GetValue(WeaponDef def) {
				return def.PoisonCapacity;
			}
		}

		public override IDataFieldView GetFieldView(int index) {
			return new WeaponPoisoningDifficultyFieldView(index);
		}
	}
}

