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
	/// #799 e_backstub (20 monsters, "ambush"). Archive: a few seconds after it notices a
	/// player it slips into hiding ("is hiding"). Its first landed hit from hiding on a player
	/// adds (target's maxhits - that hit's damage) x 3/5 - scaled by tag.backstub_damageReducer
	/// permille - and reveals it ("You were stabbed from ambush!"); it can hide again 8 s later.
	/// A hit taken while it is still getting into hiding interrupts that for 10 s. User
	/// decision: faithful, with the 3/5 factor tunable on .settings.</summary>
	[ViewableClass]
	public class BackstabAbilityDef : PassiveAbilityDef {
		private FieldValue hideDelay;
		private FieldValue damageFactor;
		private FieldValue rehideDelay;
		private FieldValue interruptDelay;
		private FieldValue noticeRange;

		public BackstabAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.hideDelay = this.InitTypedField("backstabHideDelay", (double) 4, typeof(double));
			this.damageFactor = this.InitTypedField("backstabDamageFactor", (double) 600, typeof(double));
			this.rehideDelay = this.InitTypedField("backstabRehideDelay", (double) 8, typeof(double));
			this.interruptDelay = this.InitTypedField("backstabInterruptDelay", (double) 10, typeof(double));
			this.noticeRange = this.InitTypedField("backstabNoticeRange", 12, typeof(int));
		}

		/// <summary>Seconds from noticing a player to being hidden.</summary>
		public double HideDelay {
			get { return (double) this.hideDelay.CurrentValue; }
			set { this.hideDelay.CurrentValue = value; }
		}

		/// <summary>Permille of (target maxhits - hit damage) added by the ambush (archive 600 = 3/5).</summary>
		public double DamageFactor {
			get { return (double) this.damageFactor.CurrentValue; }
			set { this.damageFactor.CurrentValue = value; }
		}

		/// <summary>Seconds after an ambush before it can hide again.</summary>
		public double RehideDelay {
			get { return (double) this.rehideDelay.CurrentValue; }
			set { this.rehideDelay.CurrentValue = value; }
		}

		/// <summary>Seconds it can't hide after being interrupted.</summary>
		public double InterruptDelay {
			get { return (double) this.interruptDelay.CurrentValue; }
			set { this.interruptDelay.CurrentValue = value; }
		}

		/// <summary>It starts hiding once a player is this close.</summary>
		public int NoticeRange {
			get { return (int) this.noticeRange.CurrentValue; }
			set { this.noticeRange.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static BackstabAbilityDef backstabAbilityDef;
		internal static BackstabAbilityDef BackstabDef {
			get {
				if (backstabAbilityDef == null) {
					backstabAbilityDef = AbstractScript.GetByDefname("a_tevent_e_backstub") as BackstabAbilityDef;
				}
				return backstabAbilityDef;
			}
		}

		//when it will be hidden (while getting into hiding), and when it may start hiding again
		private static readonly TagKey backstabHiddenAtTK = TagKey.Acquire("_npcBackstabHiddenAt_");
		private static readonly TagKey backstabNextHideTK = TagKey.Acquire("_npcBackstabNextHide_");

		/// <summary>#799 e_backstub, every AI tick: notice a player, get into hiding, be hidden.
		/// Returns true while it is hidden.</summary>
		public static bool TryBackstabHide(NPC self) {
			BackstabAbilityDef def = BackstabDef;
			if (!HasAbilityDef(self, def)) {
				return false;
			}
			if (self.Flag_Hidden) {
				return true;
			}
			double now = Globals.TimeInSeconds;
			object hiddenAt = self.GetTag(backstabHiddenAtTK);
			if (hiddenAt is double) {
				if (now < (double) hiddenAt) {
					return false;
				}
				self.RemoveTag(backstabHiddenAtTK);
				self.Flag_Hidden = true;
				return true;
			}
			object nextHide = self.GetTag(backstabNextHideTK);
			if (((nextHide is double) && (now < (double) nextHide)) || !PlayerNearby(self, def.NoticeRange)) {
				return false;
			}
			self.Emote("is hiding");
			self.SetTag(backstabHiddenAtTK, now + def.HideDelay);
			return false;
		}

		private static bool PlayerNearby(Character self, int range) {
			foreach (Character ch in CharsAround(self.GetMap(), self.X, self.Y, range)) {
				if (ch.IsPlayerForCombat && !ch.IsGM && ch.IsAliveAndValid && !ch.IsNotVisible) {
					return true;
				}
			}
			return false;
		}

		/// <summary>#799 e_backstub, attacker side after a landed hit: the ambush. Returns the bonus damage dealt.</summary>
		public static int TryApplyBackstabAmbush(Character attacker, Character defender, int landedDamage) {
			BackstabAbilityDef def = BackstabDef;
			if (!attacker.Flag_Hidden || !HasAbilityDef(attacker, def)) {
				return 0;
			}
			attacker.Flag_Hidden = false;
			attacker.SetTag(backstabNextHideTK, Globals.TimeInSeconds + def.RehideDelay);
			if (defender.IsDeleted || !defender.IsAliveAndValid || !defender.IsPlayerForCombat) {
				return 0;
			}
			NPCDef npcDef = attacker.Def as NPCDef;
			double reducer = ((npcDef != null) && (npcDef.BackstabDamageReducer != null)) ? NPCDef.RollStatRange(npcDef.BackstabDamageReducer) : 1000;
			double bonus = BackstabBonus(defender.MaxHits, landedDamage, def.DamageFactor, reducer);
			defender.SysMessage("You were stabbed from ambush!");
			return DamageManager.CauseDamage(attacker, defender, DamageType.Irresistable, bonus);
		}

		/// <summary>Pure formula: (maxHits - landedDamage) x factor/1000 x reducer/1000, never negative.</summary>
		public static double BackstabBonus(int maxHits, int landedDamage, double factorPermille, double reducerPermille) {
			return Math.Max(0, maxHits - landedDamage) * factorPermille / 1000 * reducerPermille / 1000;
		}

		/// <summary>#799 e_backstub, defender side after a landed swing: a hit while it is getting
		/// into hiding interrupts that.</summary>
		public static bool TryInterruptBackstabHide(Character defender) {
			if (!(defender.GetTag(backstabHiddenAtTK) is double) || !HasAbilityDef(defender, BackstabDef)) {
				return false;
			}
			defender.RemoveTag(backstabHiddenAtTK);
			defender.SetTag(backstabNextHideTK, Globals.TimeInSeconds + BackstabDef.InterruptDelay);
			defender.Emote("has been interrupted!");
			return true;
		}
	}
}
