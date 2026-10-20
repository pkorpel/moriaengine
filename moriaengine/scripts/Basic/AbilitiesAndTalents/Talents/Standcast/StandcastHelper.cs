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

	/// <summary>#483/#491 - shared Standcast read/consume logic, called from both SpellDef's
	/// GetSpellPowerAgainstChar/AgainstNonChar (read the multiplier, every target of the current
	/// cast - must NOT consume here, or a multi-target area spell would only boost its first
	/// target) and Magery.On_Success (consume exactly once per successful cast, after
	/// Trigger_Success has already read the boosted spellPower for every target). Non-players
	/// (no Level) and casters without the toggle active both get a no-op 1x multiplier - this is
	/// the critical regression-safety property: every pre-existing Phase 0-6 test, none of which
	/// ever activates Standcast, must see byte-identical spellPower to before this phase.
	/// <para>"Fields get a smaller flat per-level bonus instead" (per #491's own source note)
	/// doesn't currently have a concrete application point: Phase 4/5 separately confirmed no
	/// field spell's effect is scaled by spellPower at all (WallSpellDef's fields are placement-
	/// only, plus Paralyze Field's own per-tile paralyze chance isn't spellPower-scaled either) -
	/// so the full multiplier is applied uniformly below rather than inventing a field-specific
	/// path with nothing real to attach it to. Revisit if a future phase gives a field spell
	/// spellPower-scaled behavior.</para></summary>
	internal static class StandcastHelper {
		private static ActivableAbilityDef standcastDef;
		private static ActivableAbilityDef StandcastDef {
			get {
				if (standcastDef == null) {
					standcastDef = (ActivableAbilityDef) AbilityDef.GetByDefname("a_standcast");
				}
				return standcastDef;
			}
		}

		internal static int ApplyMultiplier(Character caster, int spellPower) {
			ActivableAbilityDef def = StandcastDef;
			if ((def == null) || !def.IsActive(caster)) {
				return spellPower;
			}
			Player player = caster as Player;
			if (player == null) {
				return spellPower;
			}
			int points = caster.GetAbility(def);
			double bonus = points * def.EffectPower * player.Level;
			if (bonus <= 0) {
				return spellPower;
			}
			return (int) System.Math.Round(spellPower * (1.0 + bonus));
		}

		internal static void ConsumeIfActive(Character caster) {
			ActivableAbilityDef def = StandcastDef;
			if ((def != null) && def.IsActive(caster)) {
				def.Deactivate(caster);
			}
		}
	}
}
