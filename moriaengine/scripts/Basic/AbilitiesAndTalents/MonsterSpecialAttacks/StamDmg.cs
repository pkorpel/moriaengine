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
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_npc_stam_dmg (19 monsters). Archive: each swing at the monster has
	/// tag.stam_dmg_chance (permille; the script's own intended default is 200) to drain the
	/// attacker's stamina by tag.stam_dmg_power. The archive script falls back to a power of 0
	/// when the tag is unset; 20 (the most common real value) is used instead so an unset
	/// monster still does something.</summary>
	[ViewableClass]
	public class StamDmgAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue power;

		public StamDmgAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("stamDmgChance", new double[] { 200, 200 }, typeof(double[]));
			this.power = this.InitTypedField("stamDmgPower", new double[] { 20, 20 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance per landed swing taken.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Stamina drained from the attacker.</summary>
		public double[] Power {
			get { return (double[]) this.power.CurrentValue; }
			set { this.power.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static StamDmgAbilityDef stamDmgAbilityDef;
		internal static StamDmgAbilityDef StamDmgDef {
			get {
				if (stamDmgAbilityDef == null) {
					stamDmgAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_stam_dmg") as StamDmgAbilityDef;
				}
				return stamDmgAbilityDef;
			}
		}

		/// <summary>#797 e_npc_stam_dmg - defender side, after a landed swing. Returns stamina drained.</summary>
		public static int TryApplyStamDmg(Character attacker, Character defender) {
			StamDmgAbilityDef def = StamDmgDef;
			if (!HasAbilityDef(defender, def) || !attacker.IsAliveAndValid) {
				return 0;
			}
			NPCDef npcDef = defender.Def as NPCDef;
			if (!RollChance(RollTunable(npcDef == null ? null : npcDef.StamDmgChance, def.Chance))) {
				return 0;
			}
			int drained = Math.Min(attacker.Stam, (int) Math.Round(RollTunable(npcDef == null ? null : npcDef.StamDmgPower, def.Power)));
			attacker.Stam = (short) (attacker.Stam - drained);
			attacker.SysMessage(defender.Name + " saps your strength!");
			return drained;
		}
	}
}
