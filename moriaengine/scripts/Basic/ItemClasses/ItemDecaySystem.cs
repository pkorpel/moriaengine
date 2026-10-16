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

using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>Generic decay system (#215/#219) - redesigned per explicit user follow-up
	/// request away from the original periodic-full-world-scan design, to a real per-item
	/// BoundTimer created the moment an item (or corpse) actually lands on the ground.
	///
	/// Why this is better than the sweep it replaces: no global per-tick scan cost at all (a
	/// decaying item just has its own ordinary timer, exactly like every other per-instance
	/// timer in this engine - NPCAiTimer, SpawnTimer, etc.) - the scan's own CPU cost scaled
	/// with total world item count every tick regardless of how many were actually decaying;
	/// this scales with only the currently-eligible count, at zero steady-state cost otherwise.
	/// It's also genuinely simpler to reason about: eligibility is decided once, at the exact
	/// moment it could have changed (landing on the ground, being picked up, the flag being
	/// toggled, a corpse's ownership becoming known) - not re-derived from scratch every tick.
	///
	/// Persistence comes for free: BoundTimer instances live in the same TagHolder.tags
	/// Hashtable as ordinary tags (TagHolder.AddTimer), which the generic item-save pipeline
	/// already serializes - a scheduled ItemDecayTimer survives a save/reload exactly like any
	/// other per-item timer already does (confirmed precedent: NPCAiTimer's WanderLeash,
	/// SpawnTimer's own rescheduling). No explicit save/load code needed here at all.</summary>
	public static class ItemDecaySystem {
		private static readonly TimerKey decayTimerTK = TimerKey.Acquire("_itemDecay_");

		/// <summary>Re-evaluates whether `item` should currently be on a decay clock, and
		/// (re)schedules or cancels its own per-item timer to match. Always clears any existing
		/// timer first and re-derives from scratch, so calling this more than once in a row (or
		/// before all the relevant state is known yet - see Corpse.InitFromChar's own call,
		/// made after Owner is set, correcting an earlier call made during creation before it
		/// was) is always safe and self-correcting, never additive.</summary>
		public static void ScheduleOrCancel(Item item) {
			item.DeleteTimer(decayTimerTK);
			double? minutes = GetDecayMinutesIfEligible(item);
			if (minutes.HasValue) {
				ItemDecayTimer timer = new ItemDecayTimer();
				item.AddTimer(decayTimerTK, timer);
				timer.DueInSeconds = minutes.Value * 60;
			}
		}

		/// <summary>Unconditional cancel - used from Item.On_LeaveRegion, which fires exactly
		/// when an item stops being on the ground (about to enter a container/character) but
		/// BEFORE its own Cont is actually updated to the new value (Trigger_LeaveGround's own
		/// precondition is Cont still being null) - so ScheduleOrCancel's own "is this on the
		/// ground" check can't be trusted at that exact moment; this skips straight to "no,
		/// cancel it" instead.</summary>
		public static void Cancel(Item item) {
			item.DeleteTimer(decayTimerTK);
		}

		/// <summary>Null if not currently eligible (not on the ground, exempt, or not flagged);
		/// otherwise the real minutes to use, from DecaySettings.
		///
		/// Default policy decided here, same as the original pass: attr_decay is OFF by
		/// default for every non-corpse item unless a ".def" line or a GM/script explicitly
		/// sets it - matches this engine's existing "incomplete config defaults permissive"
		/// convention. Corpses, by contrast, ALWAYS decay by default once on the ground -
		/// there's no flag gating corpse decay at all, matching real UO (every corpse
		/// eventually disappears, no content author has to opt a corpse into it).</summary>
		private static double? GetDecayMinutesIfEligible(Item item) {
			if (item.Cont != null) {
				return null; //not on the ground - nothing decays while held/contained/equipped
			}
			if (IsInNoDecayRegion(item)) {
				return null;
			}

			Corpse corpse = item as Corpse;
			if (corpse != null) {
				return (corpse.Owner != null)
					? DecaySettings.instance.playerCorpseDecayMinutes
					: DecaySettings.instance.npcCorpseDecayMinutes;
			}

			if (!item.HasAttribute(ItemAttributes.attr_decay)) {
				return null;
			}
			if (item.HasAttribute(ItemAttributes.attr_questitem)) {
				return null; //exempt, no clock at all
			}
			return DecaySettings.instance.itemDecayMinutes;
		}

		private static bool IsInNoDecayRegion(Item item) {
			FlaggedRegion region = item.GetMap().GetRegionFor(item) as FlaggedRegion;
			return (region != null) && region.Flag_NoDecay;
		}

		[SaveableClass]
		public class ItemDecayTimer : BoundTimer {
			[LoadingInitializer]
			public ItemDecayTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				Item item = (Item) cont;
				if (!item.IsDeleted) {
					item.Delete();
				}
			}
		}
	}
}
