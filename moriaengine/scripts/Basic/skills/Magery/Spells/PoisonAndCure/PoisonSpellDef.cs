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

		/// <summary>#962 - Erebor's spell_poison_custom: spell power over 150, 140 for a monster, less up to 30 by the caster's level,
		/// and a point more on a monster or under a curse. A dose of the percent poison, and a point of damage now.</summary>
		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			const int OnPlayers = 150, OnMonsters = 140, LevelsForAPoint = 5, MostFromLevel = 30, Seconds = 5, Sound = 517;
			Character caster = spellEffectArgs.Caster;
			target.Trigger_HostileAction(caster);

			bool onPlayer = target.IsPlayer;
			int fromLevel = (!onPlayer && (caster is Player player)) ? System.Math.Min(player.Level / LevelsForAPoint, MostFromLevel) : 0;
			int power = spellEffectArgs.SpellPower / ((onPlayer ? OnPlayers : OnMonsters) - fromLevel);
			if (target.HasPlugin(((DurableCharEffectSpellDef) SpellDef.GetByDefname("s_curse")).EffectPluginKey_Spell)) {
				power++;
			}
			if (!onPlayer) {
				power++;
			}
			if ((power > 0) && Poison.Apply(target, caster, power, Seconds, PoisonKind.PercentOfHits, false, Sound)) {
				DamageManager.CauseDamage(caster, target, DamageType.MagicPoison, 1);
			}
		}
	}
}