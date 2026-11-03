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

using SteamEngine.Persistence;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_bleed. Default
	/// {1000,1000} (100%) deliberately matches the real archive's own default: tag.npc_bleed
	/// is unset on most monsters, and the source script's own chance check only runs "if
	/// (<eval tag.npc_bleed>)" at all - unset means no chance roll, i.e. always bleeds; the tag
	/// only ever LOWERS the chance on specific species. NPCDef.BleedChance (same two-layer
	/// override precedent as the other 3 abilities) is how that per-species lowering is
	/// recovered from the archive.</summary>
	[ViewableClass]
	public class BleedAbilityDef : PassiveAbilityDef {
		private FieldValue chance;

		public BleedAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("bleedChance", new double[] { 1000, 1000 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to bleed on a landed melee swing -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static BleedAbilityDef bleedAbilityDef;
		internal static BleedAbilityDef BleedDef {
			get {
				if (bleedAbilityDef == null) {
					bleedAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_bleed") as BleedAbilityDef;
				}
				return bleedAbilityDef;
			}
		}

		/// <summary>#bleed - on a landed melee swing, a chance to add to the target's bleed
		/// (#745 - BleedingEffectPluginDef.Bite, the one bleed a bandage stops, as Erebor's
		/// i_krvaceni) scaled off the damage that swing actually dealt. Must be called AFTER
		/// DamageManager.CauseDamage, since it needs the real landed-damage amount, not the
		/// pre-armor swing value. A miss or a fully-blocked swing (actualDamage &lt;= 0) has
		/// nothing to scale off of - same as the real archive's own `wasHarmed` guard.</summary>
		public static void TryApplyBleed(Character attacker, Character defender, int actualDamage) {
			BleedAbilityDef def = BleedDef;
			if (def == null) {
				return;
			}
			if (actualDamage <= 0) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.BleedChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			SingletonScript<BleedingEffectPluginDef>.Instance.Bite(attacker, defender, actualDamage);
		}
	}
}
