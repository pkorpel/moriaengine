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
	/// #778 - e_ovinovac ("strangler vine", Erebor systems/new_events.scp). A player who shoves
	/// past the vine (#767's @personalSpace) gets entangled: neither of them can move and the vine
	/// attacks. The grip loosens once the vine has taken HitsToBreak landed hits, when it dies, or
	/// when the two no longer share a tile. Erebor checked the last two with a 2s timer item on the
	/// victim; here they're checked whenever the victim tries to step (CheckCanMove), which is the
	/// only time the hold matters. One victim at a time; invulnerable characters aren't grabbed.
	/// </summary>
	[ViewableClass]
	public class StranglerVineAbilityDef : PassiveAbilityDef {
		private FieldValue hitsToBreak;

		public StranglerVineAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.hitsToBreak = this.InitTypedField("vineHitsToBreak", 5, typeof(int));
		}

		/// <summary>Landed hits the vine has to take before it lets go - Erebor counted 1 + 4.</summary>
		public int HitsToBreak {
			get { return (int) this.hitsToBreak.CurrentValue; }
			set { this.hitsToBreak.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static StranglerVineAbilityDef stranglerVineAbilityDef;
		internal static StranglerVineAbilityDef StranglerVineDef {
			get {
				if (stranglerVineAbilityDef == null) {
					stranglerVineAbilityDef = AbstractScript.GetByDefname("a_tevent_e_ovinovac") as StranglerVineAbilityDef;
				}
				return stranglerVineAbilityDef;
			}
		}

		private static readonly TagKey entangledByTK = TagKey.Acquire("_entangledBy_");        //on the victim: the vine
		private static readonly TagKey entangledVictimTK = TagKey.Acquire("_entangledVictim_"); //on the vine: the victim
		private static readonly TagKey entangleHitsLeftTK = TagKey.Acquire("_entangleHitsLeft_");

		/// <summary>From NPC.On_PersonalSpace - `pusher` just shoved past `vine`.</summary>
		public static void TryEntangle(NPC vine, Character pusher) {
			StranglerVineAbilityDef def = StranglerVineDef;
			Ability ability;
			if ((def == null) || !vine.HasAbility(def, out ability)) {
				return;
			}
			if (vine.Flag_Dead || !pusher.IsPlayer || pusher.Flag_Dead || pusher.Flag_Invulnerable) {
				return;
			}
			Character held = vine.GetTag(entangledVictimTK) as Character;
			if ((held != null) && IsHoldIntact(vine, held)) {
				return; //already holding someone
			}

			vine.SetTag(entangledVictimTK, pusher);
			vine.SetTag(entangleHitsLeftTK, def.HitsToBreak);
			pusher.SetTag(entangledByTK, vine);
			pusher.RedMessage("You have been entangled and cannot move!");
			NPCAiTimer timer = NPCAiTimer.GetFor(vine);
			if (timer != null) {
				timer.EngageTarget(pusher);
			}
		}

		/// <summary>From Character.CheckCanMove - false while `self` is held by a vine, or is a vine
		/// holding someone. A hold that's no longer intact is released here, and the step goes ahead.</summary>
		public static bool CheckEntangledCanMove(Character self) {
			Character vine = self.GetTag(entangledByTK) as Character;
			if (vine != null) {
				if (IsHoldIntact(vine, self)) {
					self.SysMessage("You are entangled and cannot move!");
					return false;
				}
				Release(vine, self);
				return true;
			}
			Character victim = self.GetTag(entangledVictimTK) as Character;
			if (victim != null) {
				if (IsHoldIntact(self, victim)) {
					return false; //the vine holds on, it doesn't walk off
				}
				Release(self, victim);
			}
			return true;
		}

		/// <summary>From DamageManager.ProcessSwing - `defender` took a landed hit. A vine holding
		/// someone counts it toward HitsToBreak.</summary>
		public static void CountEntangleHit(Character defender) {
			Character victim = defender.GetTag(entangledVictimTK) as Character;
			if (victim == null) {
				return;
			}
			object tag = defender.GetTag(entangleHitsLeftTK);
			int hitsLeft = (tag is int) ? (int) tag : 1;
			hitsLeft = HitsLeftAfterHit(hitsLeft);
			if (hitsLeft <= 0) {
				Release(defender, victim);
			} else {
				defender.SetTag(entangleHitsLeftTK, hitsLeft);
			}
		}

		/// <summary>Pure: hits left after one more landed hit (never below 0).</summary>
		public static int HitsLeftAfterHit(int hitsLeft) {
			return Math.Max(0, hitsLeft - 1);
		}

		/// <summary>Pure: a hold lasts while both are alive and still share the same tile.</summary>
		public static bool IsHoldIntact(bool vineAlive, bool victimAlive, int vineX, int vineY, int vineM, int victimX, int victimY, int victimM) {
			return vineAlive && victimAlive && (vineX == victimX) && (vineY == victimY) && (vineM == victimM);
		}

		private static bool IsHoldIntact(Character vine, Character victim) {
			if (vine.IsDeleted || victim.IsDeleted) {
				return false;
			}
			if (victim.GetTag(entangledByTK) != vine) {
				return false;
			}
			return IsHoldIntact(!vine.Flag_Dead, !victim.Flag_Dead, vine.X, vine.Y, vine.M, victim.X, victim.Y, victim.M);
		}

		private static void Release(Character vine, Character victim) {
			if ((vine != null) && !vine.IsDeleted && (vine.GetTag(entangledVictimTK) == victim)) {
				vine.RemoveTag(entangledVictimTK);
				vine.RemoveTag(entangleHitsLeftTK);
			}
			if ((victim != null) && !victim.IsDeleted && (victim.GetTag(entangledByTK) == vine)) {
				victim.RemoveTag(entangledByTK);
				if (!victim.Flag_Dead) {
					victim.SysMessage("The grip loosens...");
				}
			}
		}
	}
}
