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
	/// Real-user-reported follow-up ("what are some other monster abilities we can
	/// implement?") - e_summoneater. Unconditional in the real archive source (no chance tag
	/// at all) - on a landed swing, if the target is a summoned creature (SummonSpellDef's own
	/// IsSummonedCreature tag), it's deleted outright instead of taking normal damage. No
	/// NPCDef override, no converter work - nothing to recover.</summary>
	[ViewableClass]
	public class SummonEaterAbilityDef : PassiveAbilityDef {
		public SummonEaterAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static SummonEaterAbilityDef summonEaterAbilityDef;
		private static SummonEaterAbilityDef SummonEaterDef {
			get {
				if (summonEaterAbilityDef == null) {
					summonEaterAbilityDef = AbstractScript.GetByDefname("a_tevent_e_summoneater") as SummonEaterAbilityDef;
				}
				return summonEaterAbilityDef;
			}
		}

		/// <summary>#summoneater - unconditional in the real archive: if the attacker lands a
		/// swing on a summoned creature, that creature is deleted outright instead of taking
		/// normal damage. Must be called right after Trigger_BeforeSwing succeeds, before the
		/// normal critic/damage/stun/bleed/knockback chain - returns true when the defender was
		/// consumed, telling the caller to skip the rest of the swing entirely (there's nothing
		/// left to apply any of that to).</summary>
		public static bool TryApplySummonEater(Character attacker, Character defender) {
			SummonEaterAbilityDef def = SummonEaterDef;
			if (def == null) {
				return false;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return false;
			}
			if (!SummonSpellDef.IsSummonedCreature(defender)) {
				return false;
			}
			defender.Delete();
			return true;
		}
	}
}
