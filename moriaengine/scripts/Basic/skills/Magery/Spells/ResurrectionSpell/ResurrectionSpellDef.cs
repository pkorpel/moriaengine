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
	public class ResurrectionSpellDef : SpellDef {

		public ResurrectionSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);

			if (target.Flag_Dead) {
				target.Resurrect();
			} else {
				spellEffectArgs.Caster.ClilocSysMessage(501041); // Target is not dead.
			}

		}
		protected override void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectItem(target, spellEffectArgs);



			int range = this.EffectRange;
			Character owner;
			Character caster = spellEffectArgs.Caster;
			Corpse corp = target as Corpse;

			if (corp != null) {
				if (corp.Owner != null) {
					owner = corp.Owner;
					if (owner.Flag_Dead) {
						if (owner.CanReach(corp).Allow) {
							owner.Resurrect(corp);
						} else {
							caster.WriteLine(Loc<ResurrectionLoc>.Get(caster.Language).GhostCantReachTheBody);
						}
					} else {
						caster.ClilocSysMessage(501041); // Target is not dead.
					}
				} else {
					caster.WriteLine(Loc<ResurrectionLoc>.Get(caster.Language).ThisIsntPlayersBody);
				}
			} else {
				caster.WriteLine(Loc<ResurrectionLoc>.Get(caster.Language).ThisIsntBody);

			}
		}
	}
	public class ResurrectionLoc : CompiledLocStringCollection<ResurrectionLoc> {
		public string ThisIsntBody = "Toto není tìlo!";
		public string ThisIsntPlayersBody = "Toto není tìlo hráèské postavy.";
		//internal readonly string ResurrectionPlayerIsntDead = "Hráè toho tìla není mrtvý.";
		public string GhostCantReachTheBody = "Duch nedosáhne na své tìlo.";
	}
}