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
	public partial class WhirlWindPlugin {
		private static ActivableAbilityDef a_whirlwind;
		public static ActivableAbilityDef WhirlWindDef {
			get {
				if (a_whirlwind == null) {
					a_whirlwind = (ActivableAbilityDef) AbilityDef.GetByDefname("a_whirlwind");
				}
				return a_whirlwind;
			}
		}

		bool isProcessingOthers;

		public void On_BeforeSwing(WeaponSwingArgs swingArgs) {
			swingArgs.DamageAfterAC *= WhirlWindDef.EffectPower;

			if (!this.isProcessingOthers) { //we don't want recursion
				this.isProcessingOthers = true;

				var self = (Character) this.Cont;
				var origRelation = Notoriety.GetCharRelation(self, swingArgs.defender);

				foreach (Character ch in self.GetMap().GetCharsInRange(self.X, self.Y, 1)) {
					if ((ch != swingArgs.defender) && (ch != self)) {
						var relation = Notoriety.GetCharRelation(self, ch);
						if (relation <= origRelation) { //same or worse relation than with the original target = we want to strike them
							DamageManager.ProcessSwing(self, ch);
						}
					}
				}

				this.Delete(); //WhirlWindDef.Deactivate(self);
			}
		}
	}

	[ViewableClass]
	public partial class WhirlWindPluginDef {
	}
}