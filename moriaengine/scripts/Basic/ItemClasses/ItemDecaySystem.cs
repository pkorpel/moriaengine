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

using System.Collections.Generic;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>Generic item-decay sweep (#215/#219) - no such mechanism existed in moriaengine
	/// before this; FlaggedRegion's own "flag_nodecay" was a toggle with nothing to toggle.
	///
	/// Periodically scans every live Item for attr_decay-flagged ones currently lying loose on
	/// the ground (Cont == null), and deletes them once DecaySettings.decayIntervalMinutes has
	/// elapsed since they were first observed there. attr_questitem-flagged items and items
	/// inside a flag_nodecay region are exempt - the decay clock simply never starts for them
	/// (re-checked every sweep, so removing either exemption later lets the clock start normally
	/// from that point on, it doesn't retroactively "owe" decay time).
	///
	/// Default policy decision (#219 explicitly asked for one, not a silent guess): attr_decay
	/// is OFF by default for every item unless a ".def" "ATTR=" line or a GM explicitly sets it -
	/// matches this engine's existing "incomplete/unconfigured content defaults to the
	/// permissive, nothing-happens behavior" convention (e.g. an empty allowedWeaponTypes list
	/// means unrestricted, not restricted-to-nothing). Nothing already placed in the world starts
	/// decaying just because this feature now exists.
	///
	/// Uses a periodic full Thing.AllThings scan (same recurring-BoundTimer-on-Globals.Instance
	/// idiom as LightAndWeather.WorldClockTimer) rather than a live add/remove registry - avoids
	/// needing a separate re-registration hook for items reloaded from a save file (the registry
	/// approach would silently "forget" any item that was mid-decay across a restart), at the
	/// cost of a full-world scan each tick. Fine at this engine's scale.</summary>
	public static class ItemDecaySystem {
		private static readonly TagKey decayDeadlineTK = TagKey.Acquire("_decayDeadline_");
		private static readonly TimerKey sweepTK = TimerKey.Acquire("_itemDecaySweep_");
		private const double DefaultSweepIntervalSeconds = 60;

		/// <summary>Starts the recurring sweep on first use - no reliable compiled-trigger hook
		/// for "@startup" was found (same situation LightAndWeather.EnsureClockRunning already
		/// documented), so this lazily self-installs instead, called from every place an item's
		/// attr_decay flag can become set (Item.On_Create's def-default seeding, Item.
		/// SetAttribute).</summary>
		public static void EnsureSweepRunning() {
			if (!Globals.Instance.HasTimer(sweepTK)) {
				DecaySweepTimer timer = new DecaySweepTimer();
				Globals.Instance.AddTimer(sweepTK, timer);
				timer.DueInSeconds = DefaultSweepIntervalSeconds;
				timer.PeriodInSeconds = DefaultSweepIntervalSeconds;
			}
		}

		/// <summary>Test-only hook (#219, same precedent as LightAndWeather.SpeedUpClockForTest)
		/// - re-arms the already-running sweep timer to a short, test-practical cadence instead
		/// of the real 60-second production interval.</summary>
		public static void SpeedUpSweepForTest(double dueInSeconds, double periodInSeconds) {
			EnsureSweepRunning();
			DecaySweepTimer timer = (DecaySweepTimer) Globals.Instance.GetTimer(sweepTK);
			timer.DueInSeconds = dueInSeconds;
			timer.PeriodInSeconds = periodInSeconds;
		}

		private static void PerformSweep() {
			double now = Globals.TimeInSeconds;

			//snapshot first - PerformSweep can delete items (mutating Thing.AllThings) while
			//iterating, which a live enumerator over the same collection wouldn't tolerate.
			List<Item> candidates = new List<Item>();
			foreach (Thing thing in Thing.AllThings) {
				Item item = thing as Item;
				if ((item != null) && item.HasAttribute(ItemAttributes.attr_decay)) {
					candidates.Add(item);
				}
			}

			foreach (Item item in candidates) {
				if (item.IsDeleted) {
					continue;
				}
				if (item.Cont != null) {
					item.RemoveTag(decayDeadlineTK); //picked up/contained - clock pauses
					continue;
				}
				if (item.HasAttribute(ItemAttributes.attr_questitem)) {
					continue; //exempt, no clock at all
				}
				FlaggedRegion region = item.GetMap().GetRegionFor(item) as FlaggedRegion;
				if ((region != null) && region.Flag_NoDecay) {
					continue; //exempt while in this region
				}

				object deadlineTag = item.GetTag(decayDeadlineTK);
				if (deadlineTag == null) {
					item.SetTag(decayDeadlineTK, now + (DecaySettings.instance.decayIntervalMinutes * 60));
				} else if (now >= (double) deadlineTag) {
					item.Delete();
				}
			}
		}

		[SaveableClass]
		public class DecaySweepTimer : BoundTimer {
			[LoadingInitializer]
			public DecaySweepTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				PerformSweep();
			}
		}
	}
}
