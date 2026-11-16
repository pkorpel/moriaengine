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
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	[PluginTemplate]
	public partial class SpellManaDiscountPlugin : EffectDurationPlugin {
		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.SkillDef.Id == (int) SkillName.Magery) {
				Character self = (Character) this.Cont;
				SpellDef spell = (SpellDef) skillSeqArgs.Param1;
				if (spell == this.TypeDef.Spell) {
					self.Mana += (short) (spell.GetManaUse(skillSeqArgs.Tool is SpellScroll) * this.EffectPower);
				}
			}
		}
	}

	[ViewableClass]
	[PluginDefTemplate]
	public partial class SpellManaDiscountPluginDef : EffectDurationPluginDef {
		[DefField(typeof(SpellDef))]
		private FieldValue spell;
	}
}