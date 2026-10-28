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
	/// Real-user-reported follow-up to #653's teventStubs batch - e_looter. User's explicit
	/// choice: ported faithfully rather than simplified - reacts to stepping onto ANY lootable
	/// corpse (not just ones this NPC personally killed), same as the real archive's own
	/// `on=@ItemStep` trigger. No chance roll or per-species tag exists in the archive for this
	/// one - it's unconditional. Consumed via Corpse.On_Step (see Corpse.cs), not from this
	/// file's own Try* methods - a step-on-item trigger, not a swing/tick one.</summary>
	[ViewableClass]
	public class LooterAbilityDef : PassiveAbilityDef {
		public LooterAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static LooterAbilityDef looterAbilityDef;
		private static LooterAbilityDef LooterDef {
			get {
				if (looterAbilityDef == null) {
					looterAbilityDef = AbstractScript.GetByDefname("a_tevent_e_looter") as LooterAbilityDef;
				}
				return looterAbilityDef;
			}
		}

		/// <summary>#looter (user-approved: ported faithfully) - an NPC with this ability that
		/// steps onto any lootable corpse moves one item from it into its own backpack,
		/// unconditionally (no chance roll, no per-species tag exists in the archive for this
		/// one). Called from Corpse.On_Step.</summary>
		public static void TryApplyLoot(NPC looter, Corpse corpse) {
			if ((looter == null) || (corpse == null) || corpse.IsDeleted) {
				return;
			}
			LooterAbilityDef def = LooterDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!looter.HasAbility(def, out ability)) {
				return;
			}

			Item backpack = looter.Backpack;
			if (backpack == null) {
				return;
			}
			foreach (Item loot in corpse.EnumShallow()) {
				loot.Cont = backpack;
				return; //one item per step, matching the real archive's own single-grab shape
			}
		}
	}
}
