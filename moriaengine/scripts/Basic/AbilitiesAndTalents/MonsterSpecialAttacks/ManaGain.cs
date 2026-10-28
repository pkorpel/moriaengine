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
	/// #797 e_managain (17 monsters). Archive: the monster remembers its hits when first hit;
	/// once it has lost another 5% of its maxhits since then it regains a quarter of its
	/// maxmana and starts counting again (healing back up by 5% also restarts the count).</summary>
	[ViewableClass]
	public class ManaGainAbilityDef : PassiveAbilityDef {
		private FieldValue hitsStep;
		private FieldValue manaGain;

		public ManaGainAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.hitsStep = this.InitTypedField("manaGainHitsStep", (double) 50, typeof(double));
			this.manaGain = this.InitTypedField("manaGainAmount", (double) 250, typeof(double));
		}

		/// <summary>Permille of maxhits that must be lost for one mana gain (archive 50 = 5%).</summary>
		public double HitsStep {
			get { return (double) this.hitsStep.CurrentValue; }
			set { this.hitsStep.CurrentValue = value; }
		}

		/// <summary>Permille of maxmana gained each time (archive 250 = a quarter).</summary>
		public double ManaGain {
			get { return (double) this.manaGain.CurrentValue; }
			set { this.manaGain.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static ManaGainAbilityDef manaGainAbilityDef;
		internal static ManaGainAbilityDef ManaGainDef {
			get {
				if (manaGainAbilityDef == null) {
					manaGainAbilityDef = AbstractScript.GetByDefname("a_tevent_e_managain") as ManaGainAbilityDef;
				}
				return manaGainAbilityDef;
			}
		}

		private static readonly TagKey manaGainHitsMarkTK = TagKey.Acquire("_npcManaGainHitsMark_");

		/// <summary>#797 e_managain - defender side, after a landed swing (`landedDamage` already
		/// taken off). Returns the mana gained.</summary>
		public static int TryApplyManaGain(Character defender, int landedDamage) {
			ManaGainAbilityDef def = ManaGainDef;
			if (!HasAbilityDef(defender, def) || !defender.IsAliveAndValid) {
				return 0;
			}
			int step = (int) (defender.MaxHits * def.HitsStep / 1000);
			object markTag = defender.GetTag(manaGainHitsMarkTK);
			int mark = (markTag is int) ? (int) markTag : defender.Hits + landedDamage;
			int hits = defender.Hits;
			if (hits > mark + step) {
				defender.RemoveTag(manaGainHitsMarkTK); //healed back up - count again from the next hit
				return 0;
			}
			if (hits >= mark - step) {
				defender.SetTag(manaGainHitsMarkTK, mark);
				return 0;
			}
			defender.RemoveTag(manaGainHitsMarkTK);
			int gained = Math.Max(0, Math.Min((int) (defender.MaxMana * def.ManaGain / 1000), defender.MaxMana - defender.Mana));
			defender.Mana = (short) (defender.Mana + gained);
			return gained;
		}
	}
}
