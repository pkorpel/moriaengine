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
	/// #799 e_golem (20 monsters). Archive: a golem turns on whoever just hit it, with a
	/// chance that drops as it wears down - 35% above 75% hits, 30% above 50%, 25% above 25%,
	/// 20% above 10%, 10% below that.</summary>
	[ViewableClass]
	public class GolemAbilityDef : PassiveAbilityDef {
		private FieldValue chanceAbove75;
		private FieldValue chanceAbove50;
		private FieldValue chanceAbove25;
		private FieldValue chanceAbove10;
		private FieldValue chanceBelow10;

		public GolemAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chanceAbove75 = this.InitTypedField("golemChanceAbove75", (double) 350, typeof(double));
			this.chanceAbove50 = this.InitTypedField("golemChanceAbove50", (double) 300, typeof(double));
			this.chanceAbove25 = this.InitTypedField("golemChanceAbove25", (double) 250, typeof(double));
			this.chanceAbove10 = this.InitTypedField("golemChanceAbove10", (double) 200, typeof(double));
			this.chanceBelow10 = this.InitTypedField("golemChanceBelow10", (double) 100, typeof(double));
		}

		/// <summary>Permille chance to turn on the attacker while above 75% hits.</summary>
		public double ChanceAbove75 {
			get { return (double) this.chanceAbove75.CurrentValue; }
			set { this.chanceAbove75.CurrentValue = value; }
		}

		/// <summary>Same, 50-75% hits.</summary>
		public double ChanceAbove50 {
			get { return (double) this.chanceAbove50.CurrentValue; }
			set { this.chanceAbove50.CurrentValue = value; }
		}

		/// <summary>Same, 25-50% hits.</summary>
		public double ChanceAbove25 {
			get { return (double) this.chanceAbove25.CurrentValue; }
			set { this.chanceAbove25.CurrentValue = value; }
		}

		/// <summary>Same, 10-25% hits.</summary>
		public double ChanceAbove10 {
			get { return (double) this.chanceAbove10.CurrentValue; }
			set { this.chanceAbove10.CurrentValue = value; }
		}

		/// <summary>Same, below 10% hits.</summary>
		public double ChanceBelow10 {
			get { return (double) this.chanceBelow10.CurrentValue; }
			set { this.chanceBelow10.CurrentValue = value; }
		}

		/// <summary>The archive's step table: the retarget chance for a golem at this health.</summary>
		public double ChanceAt(int hits, int maxHits) {
			if (hits > maxHits * 3 / 4) {
				return this.ChanceAbove75;
			} else if (hits > maxHits / 2) {
				return this.ChanceAbove50;
			} else if (hits > maxHits / 4) {
				return this.ChanceAbove25;
			} else if (hits > maxHits / 10) {
				return this.ChanceAbove10;
			}
			return this.ChanceBelow10;
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static GolemAbilityDef golemAbilityDef;
		internal static GolemAbilityDef GolemDef {
			get {
				if (golemAbilityDef == null) {
					golemAbilityDef = AbstractScript.GetByDefname("a_tevent_e_golem") as GolemAbilityDef;
				}
				return golemAbilityDef;
			}
		}

		/// <summary>#799 e_golem - defender side, after a landed swing. Returns true when it turned
		/// on the attacker.</summary>
		public static bool TryApplyGolemRetarget(Character attacker, Character golem) {
			GolemAbilityDef def = GolemDef;
			if (!HasAbilityDef(golem, def) || !golem.IsAliveAndValid) {
				return false;
			}
			if (!RollChance(def.ChanceAt(golem.Hits, golem.MaxHits))) {
				return false;
			}
			SwitchTargetTo(golem, attacker);
			return true;
		}
	}
}
