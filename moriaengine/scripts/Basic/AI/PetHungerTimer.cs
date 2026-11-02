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
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Hunger/decay/starvation for tamed pets and ridden mounts (#593), real archive formula
	/// (systems/jezditka.scp's per-mount "i_hladovitko" hunger tracker, ported as a proper
	/// C# timer instead of the Sphere hidden-item trick). Attached/removed automatically from
	/// Character.ControlMaster's own setter - installed only for NPCDefs with real FoodTypes
	/// data configured (see NPCDef.FoodTypes's own doc comment).
	///
	/// Deliberately broader than the real source: the archive's own decay mechanism only ever
	/// exists while a mount is actively being ridden - no equivalent was found anywhere for a
	/// tamed-but-never-ridden pet (confirmed by direct source search). Per explicit user
	/// decision, this applies to every ControlMaster-owned NPC, ridden or not, matching the
	/// user's own original "tamed pets/ridden mounts" framing rather than the narrower
	/// ridden-only real mechanic.
	/// </summary>
	[SaveableClass]
	public class PetHungerTimer : BoundTimer {
		//Real archive cadences (systems/jezditka.scp's mt_sitOnFoodTimer/mt_freeFoodTimer/
		//mt_lowFoodTimer) - decay is 3.6x slower while unridden than while ridden, but once
		//critical, checks happen much more often (and nag/dismount on EVERY check).
		private const double RiddenDecayIntervalSeconds = 1000;
		private const double UnriddenDecayIntervalSeconds = 3600;
		private const double CriticalCheckIntervalSeconds = 60;
		private const int CriticalFoodThreshold = 3;

		//Real source's "more2" - while critical, food only actually drops every 10th check
		//(600s effective), even though the dismount nag/check itself fires every 60s.
		private const int CriticalDecayCountdownStart = 10;

		private static readonly TimerKey hungerTimerTK = TimerKey.Acquire("_petHungerTimer_");

		[SaveableData] public int food;
		[SaveableData] public int criticalCountdown = CriticalDecayCountdownStart;

		[LoadingInitializer]
		public PetHungerTimer() {
		}

		/// <summary>Starts hunger tracking for a freshly-owned pet - a no-op if already
		/// installed, or if this NPC's species was never configured with real FoodTypes data
		/// (see NPCDef.FoodTypes's own doc comment for why that's the deliberate default).</summary>
		public static void EnsureInstalled(NPC self) {
			if (self.HasTimer(hungerTimerTK)) {
				return;
			}
			NPCDef def = (NPCDef) self.Def;
			if ((def.FoodTypes == null) || (def.FoodTypes.Count == 0)) {
				return;
			}
			PetHungerTimer timer = new PetHungerTimer();
			timer.food = def.MaxFood; //a freshly-tamed pet starts full, not already starving
			self.AddTimer(hungerTimerTK, timer);
			//BoundTimer's own OnTimeout wrapper auto-deletes a timer whose PeriodInSeconds AND
			//DueInSeconds both read as "disabled" at the moment it fires - true for ANY fired
			//timer that was only ever driven by DueInSeconds (the trigger condition reads the
			//just-elapsed fireAt, not a before-firing snapshot). Keeping Period non-negative
			//here avoids that false-positive cleanup; the real re-scheduling for this timer's
			//own VARYING cadence still happens explicitly via DueInSeconds in OnTimeout below,
			//same as every other tick - this placeholder value is otherwise unused.
			timer.PeriodInSeconds = RiddenDecayIntervalSeconds;
			timer.DueInSeconds = RiddenDecayIntervalSeconds;
		}

		/// <summary>How fed it is, or full for one nobody feeds: wild animals have no food here.</summary>
		internal static int FoodOf(NPC self, int full) {
			return (self.GetTimer(hungerTimerTK) is PetHungerTimer hunger) ? hunger.food : full;
		}

		/// <summary>taming_success: FOOD-1.</summary>
		internal static void LoseFood(NPC self, int amount) {
			if (self.GetTimer(hungerTimerTK) is PetHungerTimer hunger) {
				hunger.food -= amount;
			}
		}

		/// <summary>Stops hunger tracking (released/rebelled pet) - safe to call even if
		/// never installed (e.g. this NPCDef has no FoodTypes configured).</summary>
		public static void Remove(NPC self) {
			self.RemoveTimer(hungerTimerTK);
		}

		public static PetHungerTimer GetFor(NPC self) {
			return self.GetTimer(hungerTimerTK) as PetHungerTimer;
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC self = cont as NPC;
			if ((self == null) || self.IsDeleted || self.Flag_Dead) {
				return;
			}
			this.Tick(self);
		}

		/// <summary>The actual decay/critical/dismount/starvation logic, separated from
		/// OnTimeout so a test can drive exactly one tick directly and deterministically
		/// (same "internal pure-enough helper, directly testable" precedent as
		/// NPCAiTimer.PickSpell) instead of needing to wait on this timer's own real-time
		/// background-thread scheduling.</summary>
		internal void Tick(NPC self) {
			Character master = self.ControlMaster;
			if (master == null) {
				return; //no longer owned - Character.ControlMaster's setter already calls Remove() on release/rebel, this is just a safety net
			}
			if (master.IsGM) {
				this.DueInSeconds = RiddenDecayIntervalSeconds; //GMs' pets never starve - matches the real source's own mounted-GM exemption, extended here to non-ridden pets too for consistency
				return;
			}

			bool ridden = self.Rider != null;

			if (this.food < CriticalFoodThreshold) {
				if (ridden) {
					Character rider = self.Rider as Character;
					if (rider != null) {
						rider.Dismount();
					}
				}
				if (this.criticalCountdown <= 0) {
					this.food--;
					this.criticalCountdown = CriticalDecayCountdownStart;
				} else {
					this.criticalCountdown--;
				}
				this.DueInSeconds = CriticalCheckIntervalSeconds;
			} else {
				this.food--;
				if (this.food < CriticalFoodThreshold) {
					this.DueInSeconds = CriticalCheckIntervalSeconds;
				} else {
					this.DueInSeconds = ridden ? RiddenDecayIntervalSeconds : UnriddenDecayIntervalSeconds;
				}
			}

			if (this.food <= 0) {
				this.food = 0;
				self.Hits = 0; //matches systems/skills/taming.scp's own starvation consequence ("act.hits=0") - the normal death-on-zero-hits path takes over from here
			}
		}
	}
}
