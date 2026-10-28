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

	/// <summary>
	/// Real-user-reported follow-up - e_npc_infiniteMana. Unconditional in the real archive
	/// source (no chance tag, no per-species tuning): this NPC's mana is simply always kept
	/// at max. No NPCDef override, no converter work.</summary>
	[ViewableClass]
	public class InfiniteManaAbilityDef : PassiveAbilityDef {
		public InfiniteManaAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static InfiniteManaAbilityDef infiniteManaAbilityDef;
		private static InfiniteManaAbilityDef InfiniteManaDef {
			get {
				if (infiniteManaAbilityDef == null) {
					infiniteManaAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_infinitemana") as InfiniteManaAbilityDef;
				}
				return infiniteManaAbilityDef;
			}
		}

		/// <summary>#npc_infiniteMana - unconditional in the real archive (no chance roll, no
		/// per-species tuning): this NPC's mana is simply always kept topped up. Checked once
		/// per NPCAiTimer.Fight think-tick, same cheap pattern as the other periodic checks here -
		/// a caster granted this ability never actually runs out of mana between casts.</summary>
		public static void TryApplyInfiniteMana(NPC self) {
			InfiniteManaAbilityDef def = InfiniteManaDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			self.Mana = self.MaxMana;
		}
	}
}
