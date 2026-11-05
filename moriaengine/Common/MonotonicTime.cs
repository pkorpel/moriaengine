using System;
using System.Diagnostics;

namespace SteamEngine.Common {
	/// <summary>#1196 - real time for the game clock, from Stopwatch: monotonic, so an NTP step or a DST change can't jump every
	/// timer, and no conversion to local time on each read as DateTime.Now does.</summary>
	public static class MonotonicTime {
		public static long Now() {
			return Stopwatch.GetTimestamp();
		}

		/// <summary>The time between two Now() readings as a TimeSpan, exact and without overflow for any uptime: whole seconds
		/// and the rest of a second are converted apart (elapsed * TicksPerSecond overflows a long after 10 days at 1 GHz).</summary>
		public static TimeSpan Elapsed(long from, long to) {
			long elapsed = to - from;
			long frequency = Stopwatch.Frequency;
			long seconds = elapsed / frequency;
			long rest = elapsed % frequency;
			return TimeSpan.FromTicks((seconds * TimeSpan.TicksPerSecond) + (rest * TimeSpan.TicksPerSecond / frequency));
		}
	}
}
