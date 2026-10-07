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
	public partial class ClearCastPlugin {

		private static ActivableAbilityDef a_clearcast;
		public static ActivableAbilityDef ClearCastDef {
			get {
				if (a_clearcast == null) {
					a_clearcast = (ActivableAbilityDef) AbilityDef.GetByDefname("a_clearcast");
				}
				return a_clearcast;
			}
		}

		public override void On_Assign() {
			base.On_Assign();
			this.Timer = ClearCastDef.EffectDuration;
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.SkillDef.Id == (int) SkillName.Magery) {
				Character self = (Character) this.Cont;
				SpellDef spell = (SpellDef) skillSeqArgs.Param1;

				self.Mana += (short) spell.GetManaUse(skillSeqArgs.Tool is SpellScroll);
			}
		}
	}

	[ViewableClass]
	public partial class ClearCastPluginDef {
	}
}