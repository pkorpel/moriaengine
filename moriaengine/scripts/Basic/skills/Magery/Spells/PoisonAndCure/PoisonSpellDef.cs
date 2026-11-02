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
	public class PoisonSpellDef : DurableCharEffectSpellDef {
		public PoisonSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>#962 - f_effect_a_poison: mind power by Poisoning and by Magery, a plain damage poison every 5 s.</summary>
		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			const int Seconds = 5, Sound = 517, Rounding = 5000000, Scale = 1000, CoefficientScale = 1000000;
			Character caster = spellEffectArgs.Caster;
			target.Trigger_HostileAction(caster);

			PoisonEffectSettings settings = PoisonEffectSettings.instance;
			long mind = target.IsPlayerForCombat ? caster.MindPowerVsP : caster.MindPowerVsM;
			long fromPoisoning = ((mind * caster.GetSkill(SkillName.Poisoning) * settings.spellPoisoningEffect) + Rounding) / Scale;
			long fromMagery = ((mind * caster.GetSkill(SkillName.Magery) * settings.spellMageryEffect) + Rounding) / Scale;
			int coefficient = (target is NPC) ? settings.percentOnMonsters : settings.percentOnPlayers;
			Poison.Apply(target, caster, (int) (((fromPoisoning + fromMagery) * coefficient) / CoefficientScale), Seconds, PoisonKind.AbsoluteDamage, false, Sound);
		}
	}
}