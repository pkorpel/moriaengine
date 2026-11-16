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

using System;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Generators.Abstractions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	[ThingDefTemplate]
	public partial class WeaponDef : DestroyableDef {
		[DefField(typeof(double), DefaultExpression = "50")]
		private FieldValue attackVsM;

		[DefField(typeof(double), DefaultExpression = "50")]
		private FieldValue attackVsP;

		[DefField(typeof(double), DefaultExpression = "0")]
		private FieldValue piercing;

		[DefField(typeof(double), DefaultExpression = "100")]
		private FieldValue speed;

		[DefField(typeof(WeaponType), DefaultExpression = "WeaponType.BareHands")]
		private FieldValue weaponType;

		[DefField(typeof(WeaponAnimType), DefaultExpression = "WeaponAnimType.Undefined")]
		private FieldValue weaponAnimType;

		[DefField(typeof(ProjectileType), DefaultExpression = "ProjectileType.None")]
		private FieldValue projectileType;

		[DefField(typeof(int), DefaultExpression = "-1")]
		private FieldValue projectileAnim;

		[DefField(typeof(string))]
		private FieldValue ammoType;

		[DefField(typeof(int), DefaultExpression = "1")]
		private FieldValue rangeVsM;

		[DefField(typeof(int), DefaultExpression = "1")]
		private FieldValue rangeVsP;

		[DefField(typeof(int), DefaultExpression = "5")]
		private FieldValue strikeStartRange;

		[DefField(typeof(int), DefaultExpression = "10")]
		private FieldValue strikeStopRange;

		[DefField(typeof(double), DefaultExpression = "-1")]
		private FieldValue mindPowerVsM;

		[DefField(typeof(double), DefaultExpression = "-1")]
		private FieldValue mindPowerVsP;

		[DefField(typeof(int), DefaultExpression = "0")]
		private FieldValue poisoningDifficulty;

		[DefField(typeof(double), DefaultExpression = "1.0")]
		private FieldValue poisoningEfficiency;

		[DefField(typeof(int), DefaultExpression = "5")]
		private FieldValue poisonCapacity;

		[DefField(typeof(double), DefaultExpression = "1.0")]
		private FieldValue bleedingEfficiency;

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if (param.Equals("range", StringComparison.OrdinalIgnoreCase)) {
				base.LoadScriptLine(filename, line, "rangevsm", args);
				base.LoadScriptLine(filename, line, "rangevsp", args);
			} else {
				base.LoadScriptLine(filename, line, param, args);
			}
		}
	}

	[ViewableClass]
	[ThingTemplate]
	public partial class Weapon : Destroyable, IPoisonableItem {
		//#962 - Erebor's t_cm_blacksmithing_target, given to a one-handed mace at @create
		public override void OnUse(AbstractCharacter from) {
			Player player = from as Player;
			if ((player != null) && CraftingSkillDef.IsOfType(this, "t_weapon_mace_crook")) { //#962 - Sphere's Herding
				player.Target(SingletonScript<Targ_Herd>.Instance);
				return;
			}
			if ((player != null) && (this.Layer == (int) LayerNames.Hand1)) {
				if (CraftingSkillDef.IsOfType(this, "t_weapon_mace_smith")) {
					player.Target(SingletonScript<Targ_SmithHammer>.Instance);
					return;
				}
				if (CraftingSkillDef.IsOfType(this, "t_weapon_fence")) { //and t_cm_bowcrafting_target
					player.Target(SingletonScript<Targ_BowcraftKnife>.Instance, this);
					return;
				}
			}
			if ((player != null) && (this.TypeDef.UseSkill == SkillName.None) &&
					CraftingSkillDef.IsOfType(this, "t_weapon_sword", "t_weapon_axe", "t_weapon_mace_sharp")) { //#1070 - f_weapon_sharp_targon_item
				player.Target(SingletonScript<Targ_Blade>.Instance, this);
				return;
			}
			base.OnUse(from);
		}


		//at durability 0, attacknumber is halved
		public double AttackVsM {
			get {
				double halved = this.TypeDef.AttackVsM / 2;
				return (halved + (((halved * this.Durability) / this.MaxDurability)));
			}
		}

		public double AttackVsP {
			get {
				double halved = this.TypeDef.AttackVsP / 2;
				return (halved + (((halved * this.Durability) / this.MaxDurability)));
			}
		}

		public double MindPowerVsM {
			get {
				double defValue = this.TypeDef.MindPowerVsM;
				double bareHands = MagerySettings.instance.bareHandsMindPowerVsM;
				return this.CalculateMPDurabilty(defValue, bareHands);
			}
		}

		public double MindPowerVsP {
			get {
				double defValue = this.TypeDef.MindPowerVsP;
				double bareHands = MagerySettings.instance.bareHandsMindPowerVsP;
				return this.CalculateMPDurabilty(defValue, bareHands);
			}
		}

		private double CalculateMPDurabilty(double defValue, double bareHands)
		{
			if (defValue >= 0) {
				double halved = (defValue - bareHands) / 2;
				return bareHands + halved + (((halved * this.Durability) / this.MaxDurability));
			}
			return bareHands;
		}

		public double Piercing {
			get {
				return this.TypeDef.Piercing;
			}
		}

		public WeaponType WeaponType {
			get {
				return this.TypeDef.WeaponType;
			}
		}

		public int RangeVsM {
			get {
				return this.TypeDef.RangeVsM;
			}
		}

		public int RangeVsP {
			get {
				return this.TypeDef.RangeVsP;
			}
		}

		public int StrikeStartRange {
			get {
				return this.TypeDef.StrikeStartRange;
			}
		}

		public int StrikeStopRange {
			get {
				return this.TypeDef.StrikeStopRange;
			}
		}

		public double Speed {
			get {
				return this.TypeDef.Speed;
			}
		}

		public WeaponAnimType WeaponAnimType {
			get {
				return this.TypeDef.WeaponAnimType;
			}
		}

		public ProjectileType ProjectileType {
			get {
				return this.TypeDef.ProjectileType;
			}
		}

		public int ProjectileAnim {
			get {
				return this.TypeDef.ProjectileAnim;
			}
		}

		public int PoisoningDifficulty {
			get {
				return this.TypeDef.PoisoningDifficulty;
			}
		}

		public double PoisoningEfficiency {
			get {
				return this.TypeDef.PoisoningEfficiency;
			}
		}

		public int PoisonCapacity {
			get {
				return this.TypeDef.PoisonCapacity;
			}
		}

		public double BleedingEfficiency {
			get {
				return this.TypeDef.BleedingEfficiency;
			}
		}
	}
}