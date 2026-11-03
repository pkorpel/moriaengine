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

	/// <summary>#483/#491 - reads (never consumes, charges decay on their own timer rather than
	/// being spent per-cast) the caster's Magic Charge bonus for whichever school a given spell
	/// belongs to: Fire school boosts spells whose DamageType has the Fire bit set, Energy school
	/// boosts the Electric bit, Mind school boosts any spell flagged UseMindPower (Harm's own
	/// casting already uses mind power, which is exactly why Harm is this session's mind-charge
	/// trigger - not an arbitrary choice). A caster with no MagicChargePlugin (the overwhelming
	/// majority - every pre-existing Phase 0-6 test included) is a guaranteed no-op, same
	/// regression-safety shape as StandcastHelper.</summary>
	internal static class MagicChargeHelper {
		/// <summary>#1017 - f_magicCharge_formula. No target, as for a spell on the ground, counts as a monster.</summary>
		internal static int ApplyBonus(Character caster, SpellDef spell, int spellPower, Character target = null) {
			MagicChargePlugin plugin = MagicChargePlugin.Get(caster);
			MagicChargeSchool? school = MagicChargePlugin.SchoolOf(spell);
			if ((plugin == null) || (school == null)) {
				return spellPower;
			}
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			int perCharge = ((target != null) && target.IsPlayer) ? settings.chargeSpellPowerVsP : settings.chargeSpellPowerVsM;
			int effect = plugin.GetCharge(school.Value) * perCharge;
			int ability = ClassAbilities.Points(caster, MagicChargePlugin.AbilityOf(school.Value));
			if (ability < settings.chargeAbilityPoints) {
				effect = (effect * ((settings.chargeBaseEffect * settings.chargeAbilityPoints) + (ability * (100 - settings.chargeBaseEffect)))) / (100 * settings.chargeAbilityPoints);
			}
			return (spellPower * (1000 + effect)) / 1000;
		}
	}
}
