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
	public class MindBlastSpellDef : DamageSpellDef {
		public MindBlastSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		//#962 - f_effect_a_mind_blast: rounded to whole points, nothing for a mind as strong as the caster's
		protected override double CalculateDamage(Character caster, Character target, int spellPower) {
			double damage = base.CalculateDamage(caster, target, spellPower) * (caster.Int - target.Int);
			return System.Math.Max(0, System.Math.Truncate((damage + 500) / 1000));
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			if (target is NPC npc) {
				MageAbilities.TrySilence(spellEffectArgs.Caster, npc); //#1017 - silence on NPCs only
			}
			base.On_EffectChar(target, spellEffectArgs);
			ParalyzeEffectPluginDef.Unparalyze(target); //paralyze_remove
		}
	}
}