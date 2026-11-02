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
	/// #797 e_phoenix (30 monsters) - "reborning in fire". Archive: after a swing taken, when
	/// hits drop below a fifth of maxhits and maxhits is still above 250, the monster heals to
	/// half its maxhits, that half becomes its new maxhits, and it surrounds itself with a ring
	/// of 8 fire columns (fireshield, column default damage {50,80}). It can happen again each
	/// time it is beaten down, until its maxhits fall to 250. A cursed phoenix (s_curse) fails
	/// to rise 1 time in 3.</summary>
	[ViewableClass]
	public class PhoenixAbilityDef : PassiveAbilityDef {
		private FieldValue hitsThreshold;
		private FieldValue minMaxHits;
		private FieldValue cursedFailChance;
		private FieldValue ringDamage;
		private FieldValue rebornShare;

		public PhoenixAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.hitsThreshold = this.InitTypedField("phoenixHitsThreshold", (double) 200, typeof(double));
			this.minMaxHits = this.InitTypedField("phoenixMinMaxHits", 250, typeof(int));
			this.cursedFailChance = this.InitTypedField("phoenixCursedFailChance", (double) 333, typeof(double));
			this.ringDamage = this.InitTypedField("phoenixRingDamage", new double[] { 50, 80 }, typeof(double[]));
			this.rebornShare = this.InitTypedField("phoenixRebornShare", (double) 500, typeof(double));
		}

		/// <summary>#1022 - permille of maxhits it comes back at, which becomes the new maxhits: half for e_phoenix, three
		/// quarters for e_phoenix_25.</summary>
		public double RebornShare {
			get { return (double) this.rebornShare.CurrentValue; }
			set { this.rebornShare.CurrentValue = value; }
		}

		/// <summary>Rises when hits fall below this permille of maxhits (archive 200 = a fifth).</summary>
		public double HitsThreshold {
			get { return (double) this.hitsThreshold.CurrentValue; }
			set { this.hitsThreshold.CurrentValue = value; }
		}

		/// <summary>No more rebirths once maxhits is at or below this.</summary>
		public int MinMaxHits {
			get { return (int) this.minMaxHits.CurrentValue; }
			set { this.minMaxHits.CurrentValue = value; }
		}

		/// <summary>Permille chance a cursed phoenix fails to rise (archive 1 in 3).</summary>
		public double CursedFailChance {
			get { return (double) this.cursedFailChance.CurrentValue; }
			set { this.cursedFailChance.CurrentValue = value; }
		}

		/// <summary>Fire damage of each column in the rebirth ring.</summary>
		public double[] RingDamage {
			get { return (double[]) this.ringDamage.CurrentValue; }
			set { this.ringDamage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static PhoenixAbilityDef phoenixAbilityDef;
		internal static PhoenixAbilityDef PhoenixDef {
			get {
				if (phoenixAbilityDef == null) {
					phoenixAbilityDef = AbstractScript.GetByDefname("a_tevent_e_phoenix") as PhoenixAbilityDef;
				}
				return phoenixAbilityDef;
			}
		}

		/// <summary>#797 e_phoenix - defender side, after a landed swing. Returns true when it rose.</summary>
		public static bool TryApplyPhoenixRebirth(Character defender) {
			return Rebirth(defender, PhoenixDef) || Rebirth(defender, Phoenix25Def);
		}

		internal static PhoenixAbilityDef Phoenix25Def { get { return Lookup<PhoenixAbilityDef>("a_tevent_e_phoenix_25"); } }

		private static bool Rebirth(Character defender, PhoenixAbilityDef def) {
			if (!HasAbilityDef(defender, def) || !defender.IsAliveAndValid) {
				return false;
			}
			int maxHits = defender.MaxHits;
			if ((maxHits <= def.MinMaxHits) || (defender.Hits >= maxHits * def.HitsThreshold / 1000.0)) {
				return false;
			}
			if (IsCursed(defender) && RollChance(def.CursedFailChance)) {
				return false;
			}
			short reborn = (short) (maxHits * def.RebornShare / 1000.0);
			defender.MaxHits = reborn;
			defender.Hits = reborn;
			defender.Emote("is reborn in fire!");
			for (int dir = 0; dir < 8; dir++) {
				PlaceHazardTile(defender, defender.X + directionDx[dir], defender.Y + directionDy[dir],
					ScriptUtil.GetRandInRange(def.RingDamage[0], def.RingDamage[1]), DamageType.MagicFire, false);
			}
			return true;
		}

		private static DurableCharEffectSpellDef curseSpellDef;
		private static bool IsCursed(Character ch) {
			if (curseSpellDef == null) {
				curseSpellDef = SpellDef.GetByDefname("s_curse") as DurableCharEffectSpellDef;
			}
			return (curseSpellDef != null) && (ch.GetPlugin(curseSpellDef.EffectPluginKey_Spell) != null);
		}
	}
}
