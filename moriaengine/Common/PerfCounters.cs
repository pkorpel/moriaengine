using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SteamEngine.Common {
	/// <summary>#1179 (from #1161's harness): where the game thread's time goes. Time spent holding globalLock by its main
	/// holders (a packet type, a timer type, the movement and sync queues; not logins, logouts or the console), and time
	/// spent waiting for it. Off by default:
	/// steamengine.ini's perfCounters, or an admin's .perfon. While off, a hook only reads Enabled.
	/// A hook: <c>long w = PerfCounters.Now();</c> before taking the lock, <c>long t = PerfCounters.Acquired(w);</c> once it's
	/// taken, <c>PerfCounters.Held(key, t);</c> before letting it go.</summary>
	public static class PerfCounters {
		private sealed class Entry {
			internal long count, ticks, max;
		}

		//lock waits by size: bucket i holds waits under 2^i microseconds (the last one everything longer), so the memory is
		//fixed however long the counters run on a live shard
		internal const int WaitBuckets = 32;

		private static readonly Dictionary<object, Entry> entries = new Dictionary<object, Entry>();
		private static readonly object sync = new object(); //a leaf lock, taken while holding globalLock, never the other way
		private static readonly long[] waitBuckets = new long[WaitBuckets];
		private static long waitCount, waitMaxTicks;
		private static long windowStart = Stopwatch.GetTimestamp();
		private static long windowEnd = windowStart; //when it was switched off, 0 while on: an off window stops growing (after windowStart: static fields initialise in order)
		private static volatile bool enabled;

		/// <summary>Switching it on starts a fresh window; switching it off keeps what was counted, for Report.</summary>
		public static bool Enabled {
			get { return enabled; }
			set {
				lock (sync) {
					if (value != enabled) {
						enabled = value;
						if (value) {
							Reset(); //after enabled is set, or Reset would close the new window at once
						} else {
							windowEnd = Stopwatch.GetTimestamp();
						}
					}
				}
			}
		}

		/// <summary>A timestamp to start from, or 0 while off.</summary>
		public static long Now() {
			return enabled ? Stopwatch.GetTimestamp() : 0;
		}

		/// <summary>The lock is taken: counts the wait since waitStart and returns when holding began (0 if waitStart was 0).</summary>
		public static long Acquired(long waitStart) {
			if (waitStart == 0) {
				return 0;
			}
			long now = Stopwatch.GetTimestamp();
			AddLockWait(now - waitStart);
			return now;
		}

		/// <summary>The lock is about to be let go: counts the hold since heldSince under the holder's type (nothing if heldSince
		/// was 0).</summary>
		public static void Held(Type holder, long heldSince) {
			if (heldSince != 0) {
				Add(holder, Stopwatch.GetTimestamp() - heldSince);
			}
		}

		/// <summary>As Held(Type), for a holder named by a fixed string. Only Types and strings are keys: their Equals and
		/// GetHashCode can't take a lock, and this runs under the leaf lock sync.</summary>
		public static void Held(string holder, long heldSince) {
			if (heldSince != 0) {
				Add(holder, Stopwatch.GetTimestamp() - heldSince);
			}
		}

		private static void Add(object key, long ticks) {
			lock (sync) {
				if (!entries.TryGetValue(key, out Entry e)) {
					e = new Entry();
					entries[key] = e;
				}
				e.count++;
				e.ticks += ticks;
				if (ticks > e.max) {
					e.max = ticks;
				}
			}
		}

		private static void AddLockWait(long ticks) {
			double micro = ticks * 1000000.0 / Stopwatch.Frequency; //a double: a long multiply overflows on waits of hours
			int bucket = 0;
			while ((bucket < WaitBuckets - 1) && (micro >= (1L << bucket))) {
				bucket++;
			}
			lock (sync) {
				waitBuckets[bucket]++;
				waitCount++;
				if (ticks > waitMaxTicks) {
					waitMaxTicks = ticks;
				}
			}
		}

		public static void Reset() {
			lock (sync) {
				entries.Clear();
				Array.Clear(waitBuckets, 0, WaitBuckets);
				waitCount = 0;
				waitMaxTicks = 0;
				windowStart = Stopwatch.GetTimestamp();
				windowEnd = enabled ? 0 : windowStart;
			}
		}

		private static double Ms(long ticks) {
			return ticks * 1000.0 / Stopwatch.Frequency;
		}

		/// <summary>The smallest bucket bound, in ms, below which at least the given share of the waits fall; the longest wait
		/// itself when that's in the last, open bucket.</summary>
		private static double WaitPercentileMs(double share) {
			long needed = (long) Math.Ceiling(waitCount * share);
			long seen = 0;
			for (int i = 0; i < WaitBuckets - 1; i++) {
				seen += waitBuckets[i];
				if (seen >= needed) {
					return (1L << i) / 1000.0;
				}
			}
			return Ms(waitMaxTicks);
		}

		/// <summary>The window since the last reset: how busy globalLock was, the waits for it, and its top holders.</summary>
		public static string Report(int top) {
			lock (sync) {
				double window = Ms(((windowEnd != 0) ? windowEnd : Stopwatch.GetTimestamp()) - windowStart);
				long busy = entries.Values.Sum(e => e.ticks);
				var sb = new StringBuilder();
				sb.AppendFormat(CultureInfo.InvariantCulture, "perfCounters {0}, window={1:F0}ms lockBusy={2:F0}ms ({3:F1}%)",
					enabled ? "on" : "off", window, Ms(busy), (window > 0) ? (100 * Ms(busy) / window) : 0);
				if (waitCount > 0) {
					sb.AppendFormat(CultureInfo.InvariantCulture, " lockWait n={0} p50<{1:F3}ms p99<{2:F3}ms max={3:F1}ms",
						waitCount, WaitPercentileMs(0.5), WaitPercentileMs(0.99), Ms(waitMaxTicks));
				}
				foreach (var kv in entries.OrderByDescending(kv => kv.Value.ticks).Take(top)) {
					string name = (kv.Key is Type t) ? t.Name : kv.Key.ToString();
					Entry e = kv.Value;
					sb.AppendFormat(CultureInfo.InvariantCulture, "\n  {0}: n={1} total={2:F0}ms ({3:F1}%) avg={4:F1}us max={5:F1}ms",
						name, e.count, Ms(e.ticks), (window > 0) ? (100 * Ms(e.ticks) / window) : 0, 1000 * Ms(e.ticks) / e.count, Ms(e.max));
				}
				return sb.ToString();
			}
		}
	}
}
