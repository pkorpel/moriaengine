using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

using SteamEngine.Tests.Protocol;

namespace SteamEngine.Tests.Perf {

	/// <summary>#1161/#1179 - the perf harness's scenario: populate a world in phases and measure each (performance-review.md
	/// section 9). Server side: scripts/Tests/PerfProbe.cs. Run it with scripts/test-perf.sh.
	/// Env: PERF_OUT (results file), PERF_RECTS (dungeon rects file, default tools/perf/dungeon-rects.txt), PERF_SCALE
	/// (1.0 = the design world), PERF_PLAYERS (50), PERF_SECONDS (30 per measuring window), PERF_PHASES (all, or a list of
	/// players,save,stress), PERF_CONFIG (Debug, or Release once it builds).</summary>
	[Collection(PerfCollection.Name)]
	public class PerfProbeTests {
		private GameServerPairFixture fixture;
		private static readonly string OutFile = Environment.GetEnvironmentVariable("PERF_OUT") ?? "/tmp/perf-out.txt";
		private static readonly string Rects = Environment.GetEnvironmentVariable("PERF_RECTS");
		private static readonly double Scale = double.Parse(Environment.GetEnvironmentVariable("PERF_SCALE") ?? "1", System.Globalization.CultureInfo.InvariantCulture);
		private static readonly int Players = int.Parse(Environment.GetEnvironmentVariable("PERF_PLAYERS") ?? "50");
		private static readonly int MeasureSeconds = int.Parse(Environment.GetEnvironmentVariable("PERF_SECONDS") ?? "30");
		private static readonly string Phases = Environment.GetEnvironmentVariable("PERF_PHASES") ?? "all";

		private static void Log(string line) {
			File.AppendAllText(OutFile, DateTime.Now.ToString("HH:mm:ss") + " " + line.Replace(" ## ", "\n    ") + "\n");
			ProgressLog.Write("PERF " + (line.Length > 150 ? line.Substring(0, 150) : line));
		}

		private string Do(TestGameClient gm, string args, int timeoutSeconds = 900) {
			string what = args.Split(' ')[0];
			string marker = "PERF_PROBE: " + what + " ";
			int mark = this.fixture.GameOutputSnapshot().Length;
			var sw = Stopwatch.StartNew();
			gm.SendSpeech(".TestPerf \"" + args + "\"");
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(timeoutSeconds)), "No '" + marker + "' after " + args);
			string output = this.fixture.GameOutputSnapshot();
			int index = output.IndexOf(marker, mark, StringComparison.Ordinal);
			int end = output.IndexOf(" |", index, StringComparison.Ordinal);
			string line = output.Substring(index, end - index);
			Log("[" + args + "] (" + sw.ElapsedMilliseconds + "ms round trip) " + line);
			return line;
		}

		private void Measure(TestGameClient gm, string label, int seconds, List<Walker> walkers) {
			this.Do(gm, "start");
			foreach (Walker w in walkers) {
				w.Reset();
			}
			Thread.Sleep(seconds * 1000);
			this.Do(gm, "report " + label);
			if (walkers.Count > 0) {
				var all = walkers.SelectMany(w => w.Snapshot()).ToList();
				all.Sort();
				int timeouts = walkers.Sum(w => w.timeouts);
				int rejected = walkers.Sum(w => w.rejected);
				string s = all.Count == 0 ? "no steps" : string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"steps={0} ({1:F1}/s) rttMs p50={2:F1} p95={3:F1} p99={4:F1} max={5:F1} rejected={6} timeouts={7} alive={8}",
					all.Count, all.Count / (double) seconds, all[all.Count / 2], all[(int) (all.Count * 0.95)], all[Math.Min(all.Count - 1, (int) (all.Count * 0.99))], all[all.Count - 1],
					rejected, timeouts, walkers.Count(w => w.alive));
				Log("CLIENT " + label + " " + s);
			}
		}

		internal sealed class Walker {
			private readonly TestGameClient client;
			private readonly List<double> rtt = new List<double>();
			internal int timeouts, rejected;
			internal volatile bool alive = true;
			private volatile bool run = true;
			private Thread thread;

			internal Walker(TestGameClient client, int seed) {
				this.client = client;
				var rnd = new Random(seed);
				this.thread = new Thread(() => {
					byte seq = 0;
					int dir = rnd.Next(8);
					while (this.run) {
						if (rnd.Next(6) == 0) {
							dir = rnd.Next(8);
						}
						var sw = Stopwatch.StartNew();
						try {
							this.client.SendMoveRequest(dir, true, seq);
							var r = this.client.ReadMoveResult(TimeSpan.FromSeconds(5));
							double ms = sw.Elapsed.TotalMilliseconds;
							lock (this.rtt) {
								this.rtt.Add(ms);
								if (!r.Accepted) {
									this.rejected++;
								}
							}
							seq = r.Accepted ? (byte) (seq == 255 ? 1 : seq + 1) : (byte) 0;
						} catch (TimeoutException) {
							lock (this.rtt) {
								this.timeouts++;
							}
							seq = 0;
						} catch (Exception) {
							this.alive = false;
							return;
						}
						int left = 220 - (int) sw.ElapsedMilliseconds;
						if (left > 0) {
							Thread.Sleep(left);
						}
					}
				});
				this.thread.IsBackground = true;
				this.thread.Start();
			}

			internal void Reset() {
				lock (this.rtt) {
					this.rtt.Clear();
					this.timeouts = 0;
					this.rejected = 0;
				}
			}

			internal List<double> Snapshot() {
				lock (this.rtt) {
					return new List<double>(this.rtt);
				}
			}

			internal void Stop() {
				this.run = false;
				this.thread.Join(6000);
				this.client.Dispose();
			}
		}

		private static string Letters(int i) {
			return "Perf " + (char) ('A' + (i / 26)) + (char) ('a' + (i % 26)) + "x";
		}

		private static int N(int design) {
			return (int) Math.Round(design * Scale);
		}

		private bool Phase(string name) {
			return Phases == "all" || Phases.Split(',').Contains(name);
		}

		[PerfFact]
		public void PopulatedWorldUnderLoad() {
			using (this.fixture = new GameServerPairFixture()) {
				this.Run(Rects ?? Path.Combine(this.fixture.RepoRoot, "tools", "perf", "dungeon-rects.txt"));
			}
		}

		private void Run(string rects) {
			Log("==== run scale=" + Scale + " players=" + Players + " phases=" + Phases + " config=" + (Environment.GetEnvironmentVariable("PERF_CONFIG") ?? "Debug"));
			using var gm = CommandPermissionTests.Connect("perfgm", "Perf Gm");
			this.Do(gm, "autoaccounts");
			var walkers = new List<Walker>();

			this.Measure(gm, "P0-empty-world", MeasureSeconds, walkers);
			this.Do(gm, "bench 0 4763 1363");

			//design world: 15k dungeon monsters in the archive's 98 dungeon rects over 15 planes, 150k decor items in them,
			//15k overworld NPCs over plane 0 and 1, 2000 stored containers x 150 items (banks, houses)
			this.Do(gm, "rects " + rects + " " + N(15000) + " " + N(150000));
			this.Measure(gm, "P1-dungeons-" + N(15000) + "npc", MeasureSeconds, walkers);
			this.Do(gm, "npcs " + N(10000) + " 0 300 300 4700 3600");
			this.Do(gm, "npcs " + N(5000) + " 1 300 300 4700 3600");
			this.Do(gm, "ground " + N(50000) + " 0 300 300 4700 3600");
			this.Do(gm, "stored " + N(2000) + " 150");
			this.Measure(gm, "P2-design-world-no-players", MeasureSeconds, walkers);
			this.Do(gm, "bench 10 5975 3464"); //inside a_Ledove_jeskyne_level_1, the largest dungeon rect
			this.Do(gm, "bench 0 4763 1363");

			if (this.Phase("players")) {
				for (int i = 0; i < Players; i++) {
					TestGameClient c = CommandPermissionTests.Connect("perf" + i.ToString("00"), Letters(i));
					walkers.Add(new Walker(c, i));
				}
				this.Do(gm, "players");
				this.Measure(gm, "P3-" + Players + "-players-crowd-in-town", MeasureSeconds * 2, walkers);
				//half of them in the largest dungeon (mass combat), the rest across a_MORIA
				this.Do(gm, "scatter 10 5816 3257 120 120");
				this.Measure(gm, "P4-players-in-dense-dungeon", MeasureSeconds * 2, walkers);
			}

			if (this.Phase("save")) {
				this.Do(gm, "save", 1800);
				this.Do(gm, "save", 1800);
			}

			if (this.Phase("stress")) {
				this.Do(gm, "rects " + rects + " " + N(10000) + " 0");
				this.Do(gm, "npcs " + N(10000) + " 0 300 300 4700 3600");
				this.Measure(gm, "P5-stress-50k-npcs", MeasureSeconds * 2, walkers);
				if (this.Phase("save")) {
					this.Do(gm, "save", 1800);
				}
			}

			foreach (Walker w in walkers) {
				w.Stop();
			}
			Log("==== done");
		}
	}
}
