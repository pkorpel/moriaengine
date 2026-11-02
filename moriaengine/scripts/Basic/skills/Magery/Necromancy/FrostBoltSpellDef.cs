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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - f_effect_a_frost_bolt: the blow, then a second's freeze for a target near enough and weak enough.</summary>
	[ViewableClass]
	public class FrostBoltSpellDef : DamageSpellDef {
		private const int MostDistance = 7, DefenseFactor = 14, LeastRoll = 2, MostRoll = 11, FreezeSeconds = 1;
		private static readonly PluginKey freezeKey = PluginKey.Acquire("_frostBolt_");

		public FrostBoltSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);
			Character caster = spellEffectArgs.Caster;
			if (target.IsDeleted || target.Flag_Dead || (Point2D.GetSimpleDistance(target, caster) > MostDistance)) {
				return;
			}
			int mindPower = target.IsPlayerForCombat ? caster.MindPowerVsP : caster.MindPowerVsM;
			if ((CombatCalculator.GetSpellDefense(caster, target) / 10) * DefenseFactor > mindPower * Globals.dice.Next(LeastRoll, MostRoll + 1)) {
				return;
			}
			if (target.ResistParalyse > Globals.dice.Next(1000)) {
				return;
			}
			ParalyzeEffectPluginDef.Paralyze(target, freezeKey, TimeSpan.FromSeconds(FreezeSeconds));
			caster.RedMessage(Loc<FrostBoltLoc>.Get(caster.Language).Frozen);
		}
	}

	internal class FrostBoltLoc : CompiledLocStringCollection<FrostBoltLoc> {
		public string Frozen = "Cil zmrazen!";
	}
}
