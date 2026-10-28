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

	[ViewableClass]
	public class CriticAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;

		public CriticAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("criticChance", new double[] { 250, 250 }, typeof(double[]));
			this.damage = this.InitTypedField("criticDamage", new double[] { 50, 60 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to land a critical hit, melee vs.
		/// players only (same real archive restriction) - deliberately shadows AbilityDef.Chance
		/// (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Bonus damage range added to the landed swing on a critical.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static CriticAbilityDef criticAbilityDef;
		internal static CriticAbilityDef CriticDef {
			get {
				if (criticAbilityDef == null) {
					criticAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_critic") as CriticAbilityDef;
				}
				return criticAbilityDef;
			}
		}

		/// <summary>#critic - on a landed melee swing against a player specifically (same real
		/// archive restriction), a chance to add bonus damage to the swing that's already about
		/// to land. Must be called BEFORE DamageManager.CauseDamage, while swingArgs.DamageAfterAC
		/// is still mutable.</summary>
		public static void TryApplyCritic(Character attacker, WeaponSwingArgs swingArgs) {
			CriticAbilityDef def = CriticDef;
			if (def == null) {
				return;
			}
			if (!swingArgs.defender.IsPlayerForCombat) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.CriticChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double bonus = RollTunable(npcDef == null ? null : npcDef.CriticDamage, def.Damage);
			swingArgs.DamageAfterAC += bonus;
			CombatMessages.Send(attacker, "[Combat] Critical hit!");
			CombatMessages.Send(swingArgs.defender, "[Combat] " + attacker.Name + " critically hits you!");
		}
	}
}
