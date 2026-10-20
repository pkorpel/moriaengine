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

	/// <summary>#483/#489 - Reveal: area spell (dispatch via the generic IsAreaSpell/EffectRange
	/// mechanism already in SpellDef.cs, same path Explosion/Mass Dispel use) that unconditionally
	/// clears Flag_InvisByMagic (classic Reveal strips magical invisibility outright - there's no
	/// corresponding "resist" skill to contest against) and, for Flag_Hidden targets, rolls a
	/// Magery-vs-Hiding skill contest (mirroring DetectHiddenSkillDef.On_Stroke's own skill-vs-
	/// Hiding contest shape) before calling HidingSkillDef.UnHide - per #489's own recommendation
	/// of a contest over a flat/unconditional reveal, for fidelity with the existing Hiding-
	/// defense pattern.</summary>
	[ViewableClass]
	public class RevealSpellDef : SpellDef {
		private static SkillDef magery;
		private static SkillDef Magery {
			get {
				if (magery == null) {
					magery = SkillDef.GetBySkillName(SkillName.Magery);
				}
				return magery;
			}
		}

		public RevealSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);

			if (target.Flag_InvisByMagic) {
				target.Flag_InvisByMagic = false;
				target.SysMessage("You feel a surge of magic dispel your invisibility!");
			}

			if (target.Flag_Hidden) {
				Character caster = spellEffectArgs.Caster;
				if (Magery.CheckSuccess(caster, target.GetSkill(SkillName.Hiding))) {
					HidingSkillDef.UnHide(target);
				}
			}
		}
	}
}
