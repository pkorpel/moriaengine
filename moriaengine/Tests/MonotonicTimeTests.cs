using System;
using System.Diagnostics;
using System.Threading;
using SteamEngine.Common;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#1196 - the game clock's real time: Stopwatch readings to TimeSpan, exact, monotonic, no overflow.</summary>
	public class MonotonicTimeTests {
		[Fact]
		public void ElapsedIsExactForWholeAndPartSeconds() {
			long f = Stopwatch.Frequency;
			Assert.Equal(TimeSpan.Zero, MonotonicTime.Elapsed(1000, 1000));
			Assert.Equal(TimeSpan.FromSeconds(1), MonotonicTime.Elapsed(0, f));
			Assert.Equal(TimeSpan.FromMilliseconds(1500), MonotonicTime.Elapsed(5, 5 + f + (f / 2)));
		}

		[Fact]
		public void ElapsedDoesntOverflowOverAYearOfUptime() {
			long f = Stopwatch.Frequency;
			long year = 366L * 24 * 3600;
			Assert.Equal(TimeSpan.FromSeconds(year), MonotonicTime.Elapsed(0, year * f));
		}

		[Fact]
		public void FollowsRealTimeAndNeverGoesBack() {
			long start = MonotonicTime.Now();
			Stopwatch watch = Stopwatch.StartNew();
			TimeSpan last = TimeSpan.Zero;
			while (watch.ElapsedMilliseconds < 100) {
				TimeSpan now = MonotonicTime.Elapsed(start, MonotonicTime.Now());
				Assert.True(now >= last);
				last = now;
			}
			Thread.Sleep(50);
			TimeSpan total = MonotonicTime.Elapsed(start, MonotonicTime.Now());
			Assert.InRange(total.TotalMilliseconds, 140, 5000);
		}
	}
}
