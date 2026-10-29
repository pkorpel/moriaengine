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

using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #799 e_craft_monster (13 monsters - quest beasts meant for crafters). Archive: a blow
	/// from a Craftsman-profession player takes an extra {10,20} hits off it; if the weapon is
	/// one that player crafted personally (CraftingSkillDef.GetCrafter), 1 in 4 is a critical
	/// for another {15,25}. Other attackers get no bonus.</summary>
	[ViewableClass]
	public class CraftMonsterAbilityDef : PassiveAbilityDef {
		private FieldValue bonus;
		private FieldValue critChance;
		private FieldValue critBonus;

		public CraftMonsterAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.bonus = this.InitTypedField("craftMonsterBonus", new double[] { 10, 20 }, typeof(double[]));
			this.critChance = this.InitTypedField("craftMonsterCritChance", (double) 250, typeof(double));
			this.critBonus = this.InitTypedField("craftMonsterCritBonus", new double[] { 15, 25 }, typeof(double[]));
		}

		/// <summary>Extra damage from any Craftsman's blow.</summary>
		public double[] Bonus {
			get { return (double[]) this.bonus.CurrentValue; }
			set { this.bonus.CurrentValue = value; }
		}

		/// <summary>Permille chance of a critical with a weapon the attacker crafted.</summary>
		public double CritChance {
			get { return (double) this.critChance.CurrentValue; }
			set { this.critChance.CurrentValue = value; }
		}

		/// <summary>Extra damage of that critical.</summary>
		public double[] CritBonus {
			get { return (double[]) this.critBonus.CurrentValue; }
			set { this.critBonus.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static CraftMonsterAbilityDef craftMonsterAbilityDef;
		internal static CraftMonsterAbilityDef CraftMonsterDef {
			get {
				if (craftMonsterAbilityDef == null) {
					craftMonsterAbilityDef = AbstractScript.GetByDefname("a_tevent_e_craft_monster") as CraftMonsterAbilityDef;
				}
				return craftMonsterAbilityDef;
			}
		}

		/// <summary>#799 e_craft_monster - defender side, after a landed swing. Returns the extra
		/// damage dealt.</summary>
		public static int TryApplyCraftMonster(Character attacker, Character defender) {
			CraftMonsterAbilityDef def = CraftMonsterDef;
			if (!HasAbilityDef(defender, def) || !defender.IsAliveAndValid || !attacker.IsPlayerForCombat ||
					!Professions.HasProfession(attacker, Professions.Craftsman)) {
				return 0;
			}
			double extra = ScriptUtil.GetRandInRange(def.Bonus[0], def.Bonus[1]);
			Weapon weapon = attacker.Weapon;
			if ((weapon != null) && (CraftingSkillDef.GetCrafter(weapon) == attacker) && RollChance(def.CritChance)) {
				extra += ScriptUtil.GetRandInRange(def.CritBonus[0], def.CritBonus[1]);
				attacker.SysMessage("You land a critical blow.");
			}
			return DamageManager.CauseDamage(attacker, defender, DamageType.Irresistable, extra);
		}
	}
}
