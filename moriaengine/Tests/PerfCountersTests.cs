using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using SteamEngine.Common;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#1179 - PerfCounters, the live answer to "where does the game's time go": off, a hook records nothing; on, it
	/// records each holder of the lock and the waits for it, in a fixed histogram. Static state: its own collection, run with
	/// nothing beside it (Globals' ini loading in another test sets Enabled too).</summary>
	[Collection(PerfCountersCollection.Name)]
	public class PerfCountersTests {
		private const string Holder = "PerfCountersTests.Holder";

		[Fact]
		public void OffRecordsNothingAndOnRecordsHoldsAndWaits() {
			try {
				PerfCounters.Enabled = false;
				PerfCounters.Reset();
				long wait = PerfCounters.Now();
				Assert.Equal(0, wait);
				long held = PerfCounters.Acquired(wait);
				Assert.Equal(0, held);
				PerfCounters.Held(Holder, held);
				string off = PerfCounters.Report(10);
				Assert.StartsWith("perfCounters off", off);
				Assert.DoesNotContain(Holder, off);
				Assert.DoesNotContain("lockWait", off);

				PerfCounters.Enabled = true;
				for (int i = 0; i < 3; i++) {
					wait = PerfCounters.Now();
					Assert.NotEqual(0, wait);
					held = PerfCounters.Acquired(wait);
					Thread.Sleep(2);
					PerfCounters.Held(Holder, held);
				}
				string on = PerfCounters.Report(10);
				Assert.StartsWith("perfCounters on", on);
				Assert.Contains("lockWait n=3 ", on);
				Match m = Regex.Match(on, Regex.Escape(Holder) + @": n=(\d+) total=(\d+)ms");
				Assert.True(m.Success, on);
				Assert.Equal("3", m.Groups[1].Value);
				Assert.True(int.Parse(m.Groups[2].Value) >= 6, on); //three holds of 2 ms at least
				Assert.True(int.Parse(Window(on)) >= 6, on); //and the window, open while on, spans them

				//switched off, what it counted stays readable; a hold that began while on still lands
				wait = PerfCounters.Now();
				held = PerfCounters.Acquired(wait);
				PerfCounters.Enabled = false;
				PerfCounters.Held(Holder, held);
				string stopped = PerfCounters.Report(10);
				Assert.Contains(Holder + ": n=4 ", stopped);
				//and the window stops with it, or busy% would shrink the longer it's off
				Thread.Sleep(50);
				Assert.Equal(Window(stopped), Window(PerfCounters.Report(10)));

				//switched on again: a fresh window
				PerfCounters.Enabled = true;
				Assert.DoesNotContain(Holder, PerfCounters.Report(10));
			} finally {
				PerfCounters.Enabled = false;
				PerfCounters.Reset();
			}
		}

		private static string Window(string report) {
			Match m = Regex.Match(report, @"window=(\d+)ms");
			Assert.True(m.Success, report);
			return m.Groups[1].Value;
		}

		[Fact]
		public void WaitPercentilesComeFromTheHistogram() {
			try {
				PerfCounters.Enabled = true;
				PerfCounters.Reset();
				//99 waits of about nothing and one of about 20 ms: p50 and p99 sit well below the long one (10 ms leaves room for a
				//descheduled call on a loaded machine), max is it
				for (int i = 0; i < 99; i++) {
					PerfCounters.Acquired(Stopwatch.GetTimestamp());
				}
				PerfCounters.Acquired(Stopwatch.GetTimestamp() - Stopwatch.Frequency / 50);
				string report = PerfCounters.Report(10);
				Match m = Regex.Match(report, @"lockWait n=100 p50<([\d.]+)ms p99<([\d.]+)ms max=([\d.]+)ms");
				Assert.True(m.Success, report);
				Assert.True(double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) < 10, report);
				Assert.True(double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) < 10, report);
				Assert.True(double.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture) >= 19, report);
			} finally {
				PerfCounters.Enabled = false;
				PerfCounters.Reset();
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PerfCountersCollection {
		public const string Name = "PerfCounters";
	}
}
