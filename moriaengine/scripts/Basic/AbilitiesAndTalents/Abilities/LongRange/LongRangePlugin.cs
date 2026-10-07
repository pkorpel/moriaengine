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

	[ViewableClass]
	public partial class LongRangePlugin {

		private static ActivableAbilityDef a_longrange;
		public static ActivableAbilityDef LongRangeDef {
			get {
				if (a_longrange == null) {
					a_longrange = (ActivableAbilityDef) AbilityDef.GetByDefname("a_longrange");
				}
				return a_longrange;
			}
		}

		public void On_Assign() {
			this.Timer = LongRangeDef.EffectDuration;

			this.EffectPower = LongRangeDef.EffectPower; //ignore points in ability

			Character self = (Character) this.Cont;
			self.WeaponRangeModifier += (int) this.EffectPower;
		}

		public override void On_UnAssign(Character cont) {
			base.On_UnAssign(cont);

			Character self = (Character) this.Cont;
			self.WeaponRangeModifier -= (int) this.EffectPower;
		}

		//with longrange active, every swing fizzles the target's spells
		public void On_AfterSwing(Character self, WeaponSwingArgs swingArgs) {
			if (swingArgs.FinalDamage > 0) { //do we need to have done damage?
				swingArgs.defender.Trigger_Disrupt();
			}
		}
	}

	[ViewableClass]
	public partial class LongRangePluginDef {
	}
}