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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class BloodritualPlugin {

		private static ActivableAbilityDef a_bloodritual;
		public static ActivableAbilityDef BloodritualDef {
			get {
				if (a_bloodritual == null) {
					a_bloodritual = (ActivableAbilityDef) AbilityDef.GetByDefname("a_bloodritual");
				}
				return a_bloodritual;
			}
		}

		public void On_Assign() {
			Player self = (Player) this.Cont;
			short statDifference;

			self.Vit = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Vit, (short) this.EffectPower, out statDifference);
			self.Int = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Int, (short) this.EffectPower, out statDifference);
		}

		public override void On_UnAssign(Character cont) {
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class BloodritualPluginDef {
	}

	public class BloodritualLoc : CompiledLocStringCollection<BloodritualLoc> {
		// TODO doplnit hlasky
		//public string BloodritualActivated = "";
		//public string BloodritualDeactivated = "";
		//public string StatsTooLowToActivate = "";
}
}
