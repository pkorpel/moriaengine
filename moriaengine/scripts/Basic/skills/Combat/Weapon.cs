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

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class WeaponDef {
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
	public partial class Weapon : IPoisonableItem {

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