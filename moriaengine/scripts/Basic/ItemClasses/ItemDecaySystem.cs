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

		/// <summary>#385 - test-support only: reports whether `item` currently has its decay
		/// timer scheduled, so a test can confirm a GM toggling attr_decay off/on a corpse via
		/// D_AttrList actually cancels/restores the real timer, not just the flag's own value.</summary>
		public static bool HasActiveTimer(Item item) {
			return item.HasTimer(decayTimerTK);
		}

		/// <summary>Null if not currently eligible (not on the ground or exempt); otherwise the
		/// real minutes to use, from DecaySettings.
		///
		/// Default policy (#371, flipped from the original #219 pass): a non-corpse item loose
		/// on the ground decays BY DEFAULT now, matching real Sphere (where the global
		/// DECAYTIMER ini setting decays virtually everything loose by default - attr_decay
		/// itself is only ever used by real content as a forcing/override flag on specific
		/// authored items, never as the default-off master gate the original pass modeled it
		/// as). attr_decay therefore no longer gates anything here - it's vestigial-safe, not
		/// removed from the enum, since real/future content setting it explicitly changes
		/// nothing (decay already applies). Exemptions: attr_questitem (unchanged),
		/// attr_lockeddown/attr_secure (new - a player's intentionally-secured house items must
		/// not start vanishing just because they're sitting on the floor instead of in a
		/// container), and a Flag_NoDecay region (unchanged). Corpses, by contrast, decay by
		/// default once on the ground (#383 sets attr_decay = true on every corpse at creation
		/// for exactly this) - but unlike non-corpse items, a corpse's attr_decay IS a real gate
		/// (#385): a GM can explicitly disable it via D_AttrList (e.g. to preserve a corpse as a
		/// monument/evidence), and SetAttribute's existing ScheduleOrCancel re-evaluation means
		/// re-checking it later resumes the normal corpse-rate timer.</summary>
		private static double? GetDecayMinutesIfEligible(Item item) {
			if (item.Cont != null) {
				return null; //not on the ground - nothing decays while held/contained/equipped
			}
			if (IsInNoDecayRegion(item)) {
				return null;
			}

			Corpse corpse = item as Corpse;
			if (corpse != null) {
				if (!corpse.HasAttribute(ItemAttributes.attr_decay)) {
					return null; //#385 - a GM explicitly disabled decay on this specific corpse
				}
				return (corpse.Owner != null)
					? DecaySettings.instance.playerCorpseDecayMinutes
					: DecaySettings.instance.npcCorpseDecayMinutes;
			}

			if (item.HasAttribute(ItemAttributes.attr_questitem)) {
				return null; //exempt, no clock at all
			}
			if (item.Flag_NonMovable) {
				//#697 - a permanently-fixed item (attr_static/attr_move_never, e.g. a GM-placed
				//.static fixture or a .spawn gem) is conceptually the same "never trash, meant to
				//persist indefinitely" category as a locked-down house item below - #371's
				//"everything loose on the ground decays by default" policy never accounted for
				//this, so any such item (not just new ones - every attr_movenever item already in
				//the real archive content) was silently at risk of decaying away once dropped.
				return null;
			}
			if (item.HasAttribute(ItemAttributes.attr_lockeddown) || item.HasAttribute(ItemAttributes.attr_secure)) {
				return null; //player-secured house items are never trash, regardless of attr_decay
			}
			return DecaySettings.instance.itemDecayMinutes;
		}

		private static bool IsInNoDecayRegion(Item item) {
			FlaggedRegion region = item.GetMap().GetRegionFor(item) as FlaggedRegion;
			return (region != null) && region.Flag_NoDecay;
		}

		//#941 - DeepCopyableClass: a stack on the ground that's split copies its timers, and this one aborted the pickup
		[SaveableClass, DeepCopyableClass]
		public class ItemDecayTimer : BoundTimer {
			[LoadingInitializer, DeepCopyImplementation]
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
