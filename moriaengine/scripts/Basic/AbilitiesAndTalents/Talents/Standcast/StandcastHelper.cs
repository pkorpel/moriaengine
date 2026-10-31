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

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#491 - shared Standcast read/consume logic, called from SpellDef's
	/// GetSpellPowerAgainstChar (every target of the current cast - must NOT consume there, or a
	/// multi-target spell would only boost its first target) and Magery.On_Success (consume
	/// once per successful cast).</summary>
	public static class StandcastHelper {
		private static ActivableAbilityDef standcastDef;
		private static ActivableAbilityDef StandcastDef {
			get {
				if (standcastDef == null) {
					standcastDef = (ActivableAbilityDef) AbilityDef.GetByDefname("a_standcast");
				}
				return standcastDef;
			}
		}

		private const int spellExplosion = 43, spellChainLightning = 49, spellMeteorSwarm = 55;

		/// <summary>#860 - dospellcustom_donesrc: what the toggle adds to the multiplier of a spell
		/// on a monster. By level, up to 200, and half for the spells that hit several targets.</summary>
		public static double CalculateLevelBonus(int level, int spellId, double perLevel) {
			int usableLevel = System.Math.Min(level, 200);
			if ((spellId == spellExplosion) || (spellId == spellChainLightning) || (spellId == spellMeteorSwarm)) {
				usableLevel /= 2;
			}
			return usableLevel * perLevel;
		}

		internal static double GetLevelBonus(Character caster, int spellId) {
			ActivableAbilityDef def = StandcastDef;
			Player player = caster as Player;
			if ((def == null) || (player == null) || !def.IsActive(caster)) {
				return 0;
			}
			return CalculateLevelBonus(player.Level, spellId, MagerySettings.instance.fieldSpellPowerPerLevel);
		}

		internal static void ConsumeIfActive(Character caster) {
			ActivableAbilityDef def = StandcastDef;
			if ((def != null) && def.IsActive(caster)) {
				def.Deactivate(caster);
			}
		}
	}
}
