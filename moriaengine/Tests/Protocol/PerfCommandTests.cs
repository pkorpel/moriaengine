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
