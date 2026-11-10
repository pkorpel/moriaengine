using System;
using System.Diagnostics;
using System.IO;
using Xunit;

using SteamEngine.Tests.Protocol;

namespace SteamEngine.Tests.Perf {

	/// <summary>#1161/#1179 - boot on the world PerfProbeTests saved last (its save phase), and time the load. No collection
	/// fixture on purpose: that one wipes saves/ before booting. Run it with scripts/test-perf.sh, after PerfProbeTests.</summary>
	[Collection(PerfCollection.Name)]
	public class PerfLoadTests {
		private static readonly string OutFile = Environment.GetEnvironmentVariable("PERF_OUT") ?? "/tmp/perf-out.txt";

		[PerfFact]
		public void BootOnTheSavedWorld() {
			var sw = Stopwatch.StartNew();
			using var servers = GameServerPairFixture.BootPreservingSaves();
			long bootMs = sw.ElapsedMilliseconds;
			string output = servers.GameOutputSnapshot();
			File.AppendAllText(OutFile, DateTime.Now.ToString("HH:mm:ss") + " LOAD boot-to-ready=" + bootMs + "ms\n");
			foreach (string line in output.Split('\n')) {
				if (line.Contains("Loading") || line.Contains("Loaded") || line.Contains("took") || line.Contains("Server Initialisation") || line.Contains("Compiling scripts") || line.Contains("sectors")) {
					File.AppendAllText(OutFile, "    " + line.Trim() + "\n");
				}
			}
			using var gm = CommandPermissionTests.Connect("perfgm", "Perf Gm Two");
			int mark = servers.GameOutputSnapshot().Length;
			gm.SendSpeech(".TestPerf \"counts\"");
			Assert.True(servers.WaitForGameOutputSince("PERF_PROBE: counts", mark, TimeSpan.FromSeconds(120)));
			string after = servers.GameOutputSnapshot();
			int i = after.IndexOf("PERF_PROBE: counts", mark, StringComparison.Ordinal);
			File.AppendAllText(OutFile, "    " + after.Substring(i, after.IndexOf(" |", i, StringComparison.Ordinal) - i) + "\n");

			//#1360 - a loaded world's first save (and a second for comparison): #1228's one missed pass line was measured
			//only after building the world
			for (int n = 1; n <= 2; n++) {
				mark = servers.GameOutputSnapshot().Length;
				gm.SendSpeech(".TestPerf \"save\"");
				Assert.True(servers.WaitForGameOutputSince("PERF_PROBE: save timings", mark, TimeSpan.FromSeconds(300)));
				string saved = servers.GameOutputSnapshot();
				foreach (string line in saved.Substring(mark).Split('\n')) {
					if (line.Contains("PERF_PROBE: save", StringComparison.Ordinal)) {
						File.AppendAllText(OutFile, "    LOAD save" + n + " " + line.Trim() + "\n");
					}
				}
			}
		}
	}
}
