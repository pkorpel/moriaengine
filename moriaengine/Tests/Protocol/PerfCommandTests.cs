using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1179 - the perf counters in a live server: off they count nothing, .perfon counts what holds the global lock (here the
	/// speech packet) and the waits for it, .perfoff stops counting, .perfreset clears it. Server side:
	/// scripts/Basic/administration/PerfCommands.cs.</summary>
	[Collection(PerfCommandCollection.Name)]
	public class PerfCommandTests {
		private const string Account = "perfcmd";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void PerfCountersCountOnlyWhileOn() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect(Account, "Perf Commander")) {
				//off, whatever this checkout's steamengine.ini says (its default, off, is checked in the fast tier)
				CommandPermissionTests.Ask(fixture, client, ".perfoff", "PERF: counters off");
				CommandPermissionTests.Ask(fixture, client, ".perfreset", "PERF: counters reset");
				client.SendSpeech("not counted");
				Thread.Sleep(500);
				string off = Report(fixture, client);
				Assert.StartsWith("PERF: perfCounters off", off);
				Assert.DoesNotContain("InPacket: n=", off);

				CommandPermissionTests.Ask(fixture, client, ".perfon", "PERF: counters on");
				client.SendSpeech("hello there"); //a packet the counters must see
				Thread.Sleep(1500);
				string on = Report(fixture, client);
				Assert.StartsWith("PERF: perfCounters on", on);
				Assert.Contains("TalkRequestInPacket: n=", on);
				Assert.Contains("lockWait n=", on);

				CommandPermissionTests.Ask(fixture, client, ".perfoff", "PERF: counters off");
				Assert.StartsWith("PERF: perfCounters off", Report(fixture, client)); //what it counted stays readable
				CommandPermissionTests.Ask(fixture, client, ".perfreset", "PERF: counters reset");
				string reset = Report(fixture, client);
				Assert.DoesNotContain("InPacket: n=", reset);
				Assert.DoesNotContain("lockWait", reset);
			}
		}

		/// <summary>#1220 - the harness report's longest GC pause: a forced blocking gen2 after .TestPerf start shows up as a full
		/// blocking GC with a pause above 0, and the line names it.</summary>
		[Fact]
		public void HarnessReportSamplesTheLongestGcPause() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect(Account, "Perf Prober")) {
				CommandPermissionTests.Ask(fixture, client, ".TestPerf \"start\"", "PERF_PROBE: start ok");
				CommandPermissionTests.Ask(fixture, client, ".TestPerf \"gc\"", "PERF_PROBE: gc ok");
				int mark = fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestPerf \"report gctest\"");
				Assert.True(fixture.WaitForGameOutputSince("PERF_PROBE: report gctest", mark, Wait), "No report after .TestPerf report");
				string output = fixture.GameOutputSnapshot();
				int start = output.IndexOf("PERF_PROBE: report gctest", mark, StringComparison.Ordinal);
				for (int i = 0; (output.IndexOf(" |", start, StringComparison.Ordinal) < 0) && (i < 50); i++) { //the line's end may still be on its way
					Thread.Sleep(100);
					output = fixture.GameOutputSnapshot();
				}
				string report = output.Substring(start, output.IndexOf(" |", start, StringComparison.Ordinal) - start);
				var m = System.Text.RegularExpressions.Regex.Match(report, @"gcMaxPauseMs=([0-9.]+) \(([^)]*)\) gcSampled=([0-9]+) background=[0-9]+ fullBlocking=([0-9]+)");
				Assert.True(m.Success, "no GC pause sampling in: " + report);
				Assert.True(int.Parse(m.Groups[4].Value) >= 1, "the forced gen2 wasn't counted: " + report);
				Assert.True(double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) > 0, "no pause measured: " + report);
			}
		}

		/// <summary>.perf's report as the server logs it, all of its lines.</summary>
		private static string Report(GameServerPairFixture fixture, TestGameClient client) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".perf");
			Assert.True(fixture.WaitForGameOutputSince("commands 'perf'", mark, Wait), "No end of .perf");
			string output = fixture.GameOutputSnapshot();
			int start = output.IndexOf("PERF: perfCounters", mark, StringComparison.Ordinal);
			Assert.True(start >= 0, "No report after .perf:\n" + output.Substring(mark));
			int end = output.IndexOf("commands 'perf'", start, StringComparison.Ordinal);
			return output.Substring(start, end - start);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PerfCommandCollection {
		public const string Name = "PerfCommand";
	}
}
