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

	/// <summary>#797 e_summoneater_vamp (18 monsters) - a summon eater that also swallows its
	/// meal's strength: the eaten summon's str/dex/int and hits/mana/stamina are added to its
	/// own, each capped at 30000 (the archive's own guard against the 32k stat overflow), and
	/// its max hits/mana/stamina grow to fit.</summary>
	[ViewableClass]
	public class SummonEaterVampAbilityDef : PassiveAbilityDef {
		private FieldValue statCap;

		public SummonEaterVampAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.statCap = this.InitTypedField("summonEaterVampStatCap", 30000, typeof(int));
		}

		/// <summary>No stat grows past this.</summary>
		public int StatCap {
			get { return (int) this.statCap.CurrentValue; }
			set { this.statCap.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static SummonEaterVampAbilityDef summonEaterVampAbilityDef;
		internal static SummonEaterVampAbilityDef SummonEaterVampDef {
			get {
				if (summonEaterVampAbilityDef == null) {
					summonEaterVampAbilityDef = AbstractScript.GetByDefname("a_tevent_e_summoneater_vamp") as SummonEaterVampAbilityDef;
				}
				return summonEaterVampAbilityDef;
			}
		}

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
			bool vamp = HasAbilityDef(attacker, SummonEaterVampDef);
			if (!vamp && !HasAbilityDef(attacker, SummonEaterDef)) {
				return false;
			}
			if (!SummonSpellDef.IsSummonedCreature(defender)) {
				return false;
			}
			if (vamp) {
				AbsorbSummonStats(attacker, defender, SummonEaterVampDef.StatCap);
			}
			defender.Delete();
			return true;
		}

		private static void AbsorbSummonStats(Character eater, Character meal, int cap) {
			eater.Str = AddCapped(eater.Str, meal.Str, cap);
			eater.Dex = AddCapped(eater.Dex, meal.Dex, cap);
			eater.Int = AddCapped(eater.Int, meal.Int, cap);
			short hits = AddCapped(eater.Hits, meal.Hits, cap);
			short mana = AddCapped(eater.Mana, meal.Mana, cap);
			short stam = AddCapped(eater.Stam, meal.Stam, cap);
			if (eater.MaxHits < hits) {
				eater.MaxHits = hits;
			}
			if (eater.MaxMana < mana) {
				eater.MaxMana = mana;
			}
			if (eater.MaxStam < stam) {
				eater.MaxStam = stam;
			}
			eater.Hits = hits;
			eater.Mana = mana;
			eater.Stam = stam;
			eater.Emote("devours its prey and grows stronger!");
		}

		private static short AddCapped(int own, int added, int cap) {
			return (short) System.Math.Max(0, System.Math.Min(cap, own + added));
		}
	}
}
