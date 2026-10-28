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
	/// Real-user-reported follow-up - e_npc_sniper. Real archive requires BOTH the defender
	/// being a player AND the attacker actively fighting at range (`isfighting_ranged`) - a
	/// strictly narrower gate than CriticAbilityDef's "vs player only" (any weapon). Real
	/// defaults: chance(200,3)=20%, dmg flat 50 (archive's own range tag default, when unset,
	/// is a flat fallback rather than a true {min,max} - using {50,50} here reproduces that).</summary>
	[ViewableClass]
	public class SniperAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;

		public SniperAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("sniperChance", new double[] { 200, 200 }, typeof(double[]));
			this.damage = this.InitTypedField("sniperDamage", new double[] { 50, 50 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to land a critical hit while
		/// fighting at range, vs. players only (same real archive restriction) - deliberately
		/// shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
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
		private static SniperAbilityDef sniperAbilityDef;
		private static SniperAbilityDef SniperDef {
			get {
				if (sniperAbilityDef == null) {
					sniperAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_sniper") as SniperAbilityDef;
				}
				return sniperAbilityDef;
			}
		}

		/// <summary>#npc_sniper - same call-site/shape as TryApplyCritic (before CauseDamage,
		/// mutates swingArgs.DamageAfterAC while it's still mutable), but gated on the attacker
		/// actively fighting at RANGE (matches the real archive's own isfighting_ranged check) in
		/// addition to the defender being a player - strictly narrower than critic's "vs player
		/// only, any weapon".</summary>
		public static void TryApplySniper(Character attacker, WeaponSwingArgs swingArgs) {
			SniperAbilityDef def = SniperDef;
			if (def == null) {
				return;
			}
			if (!swingArgs.defender.IsPlayerForCombat) {
				return;
			}
			if ((attacker.WeaponType != WeaponType.Bow) && (attacker.WeaponType != WeaponType.XBow)) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.SniperChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double bonus = RollTunable(npcDef == null ? null : npcDef.SniperDamage, def.Damage);
			swingArgs.DamageAfterAC += bonus;
			CombatMessages.Send(swingArgs.defender, "[Combat] " + attacker.Name + " hits you with a precise shot!");
		}
	}
}
