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
	public partial class ManaSavingPlugin {

		private static ActivableAbilityDef a_mana_saving;
		public static ActivableAbilityDef ManaSavingDef {
			get {
				if (a_mana_saving == null) {
					a_mana_saving = (ActivableAbilityDef) AbilityDef.GetByDefname("a_mana_saving");
				}
				return a_mana_saving;
			}
		}

		private static ActivableAbilityDef a_mana_saving_bonus;
		public static ActivableAbilityDef ManaSavingBonusDef {
			get {
				if (a_mana_saving_bonus == null) {
					a_mana_saving_bonus = (ActivableAbilityDef) AbilityDef.GetByDefname("a_mana_saving_bonus");
				}
				return a_mana_saving_bonus;
			}
		}

		public override void On_Assign() {
			base.On_Assign();
			this.EffectPower += ((Character) this.Cont).GetAbility(ManaSavingBonusDef) * ManaSavingBonusDef.EffectPower;
		}

		public void On_SkillSuccess(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.SkillDef.Id == (int) SkillName.Magery) {
				Character self = (Character) this.Cont;
				ActivableAbilityDef abilityDef = ManaSavingDef;

				if (self.Mana < self.MaxMana) {
					SpellDef spell = (SpellDef) skillSeqArgs.Param1;
					double manause = spell.GetManaUse(skillSeqArgs.Tool is SpellScroll);
					double giveback = manause * this.EffectPower;
				
					self.Mana = (short) Math.Min(
						Math.Ceiling(self.Mana + giveback), //round up
						self.MaxMana);
				}
			}
		}
	}

	[ViewableClass]
	public partial class ManaSavingPluginDef {
	}
}