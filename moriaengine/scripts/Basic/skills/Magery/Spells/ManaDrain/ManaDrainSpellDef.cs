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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#488 - Mana Drain: removes mana from the target, unlike Circle VII's Mana
	/// Vampire (not yet built) which transfers the drained mana to the caster. Mirrors
	/// DamageSpellDef's own On_EffectChar/EFFECT shape, except it reduces Character.Mana
	/// directly instead of routing through DamageManager.CauseDamage - mana isn't a damage type.
	/// Deliberately NOT using Character.AddMana - that helper's Min/Max clamp order is inverted
	/// (always collapses to 0 regardless of input), a pre-existing bug reported separately.</summary>
	[ViewableClass]
	public class ManaDrainSpellDef : SpellDef {
		public ManaDrainSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			double drain = this.GetEffectForValue(spellEffectArgs.SpellPower);
			target.Mana = (short) Math.Max(0, target.Mana - drain);
		}
	}
}
