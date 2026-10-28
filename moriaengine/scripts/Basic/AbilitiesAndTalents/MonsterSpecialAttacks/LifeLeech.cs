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
	/// #797 e_lifeleech (20 monsters, "vampire leech"). Archive: every swing heals the monster
	/// by tag.npc_lifeLeech ({min,max}); unset, by {maxhits/300+20, maxhits/200+35}. Never
	/// above maxhits.</summary>
	[ViewableClass]
	public class LifeLeechAbilityDef : PassiveAbilityDef {
		private FieldValue amount;

		public LifeLeechAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.amount = this.InitTypedField("lifeLeechAmount", new double[] { 0, 0 }, typeof(double[]));
		}

		/// <summary>Hit points leeched per landed swing; {0,0} (the default) means the archive's
		/// own maxhits-scaled amount.</summary>
		public double[] Amount {
			get { return (double[]) this.amount.CurrentValue; }
			set { this.amount.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static LifeLeechAbilityDef lifeLeechAbilityDef;
		internal static LifeLeechAbilityDef LifeLeechDef {
			get {
				if (lifeLeechAbilityDef == null) {
					lifeLeechAbilityDef = AbstractScript.GetByDefname("a_tevent_e_lifeleech") as LifeLeechAbilityDef;
				}
				return lifeLeechAbilityDef;
			}
		}

		/// <summary>#797 e_lifeleech - attacker side, after a landed swing. Returns hits healed.</summary>
		public static int TryApplyLifeLeech(Character attacker) {
			LifeLeechAbilityDef def = LifeLeechDef;
			if (!HasAbilityDef(attacker, def) || !attacker.IsAliveAndValid) {
				return 0;
			}
			NPCDef npcDef = attacker.Def as NPCDef;
			int maxHits = attacker.MaxHits;
			double amount;
			if ((npcDef != null) && (npcDef.LifeLeechAmount != null)) {
				amount = NPCDef.RollStatRange(npcDef.LifeLeechAmount);
			} else if (def.Amount[1] > 0) {
				amount = ScriptUtil.GetRandInRange(def.Amount[0], def.Amount[1]);
			} else {
				amount = ScriptUtil.GetRandInRange(maxHits / 300 + 20, maxHits / 200 + 35);
			}
			int healed = Math.Max(0, Math.Min((int) Math.Round(amount), maxHits - attacker.Hits));
			attacker.Hits = (short) (attacker.Hits + healed);
			return healed;
		}
	}
}
