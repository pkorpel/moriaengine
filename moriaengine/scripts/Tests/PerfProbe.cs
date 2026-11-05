using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Networking;
using SteamEngine.Regions;
using Timer = SteamEngine.Timers.Timer;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1161/#1179 - server side of the perf harness (Tests/Perf/PerfProbeTests.cs): populate a world, measure what it
	/// costs. Only as .TestPerf, so only on a test server (the harness's environment variable). How to run: performance-review.md
	/// section 9, scripts/test-perf.sh.</summary>
	public static class PerfProbe {
		private static readonly string[] npcDefs = { "c_skeleton", "c_zombie", "c_troll", "c_ogre", "c_ettin", "c_orc_lord" };
		private static readonly string[] groundDefs = { "i_barrel", "i_rock_plain", "i_bones", "i_chest_wooden_brass", "i_torch" };
		private static readonly string[] packDefs = { "i_gold", "i_bandage", "i_ingot_iron", "i_arrow", "i_rock_plain", "i_bones" };
		private static readonly Random rnd = new Random(1161);
		private static readonly List<Thing> populated = new List<Thing>();

		private static LatenessTimer probe;
		private static Thread lockProber;
		private static volatile bool lockProberRun;
		private static readonly List<double> lockProbeMs = new List<double>();
		private static readonly object lockProbeSync = new object();
		private static TimeSpan cpuAtStart;
		private static long wallAtStart;
		private static int gc0, gc1, gc2;
		private static TimeSpan pauseAtStart;
		private static long allocAtStart;

		private sealed class LatenessTimer : Timer {
			internal readonly List<double> latenessMs = new List<double>();
			internal long last;
			internal const double PeriodMs = 100;

			internal LatenessTimer() {
				this.PeriodInSeconds = PeriodMs / 1000;
			}

			protected override void OnTimeout() {
				long now = Stopwatch.GetTimestamp();
				if (this.last != 0) {
					this.latenessMs.Add(((now - this.last) * 1000.0 / Stopwatch.Frequency) - PeriodMs);
				}
				this.last = now;
			}
		}

		private static object GlobalLock {
			get {
				Type main = typeof(Globals).Assembly.GetType("SteamEngine.MainClass");
				return main.GetField("globalLock", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			}
		}

		private static int TimerCount {
			get {
				object pq = typeof(Timer).GetField("priorityQueue", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
				return (int) pq.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pq);
			}
		}

		private static string Pct(List<double> values) {
			if (values.Count == 0) {
				return "n=0";
			}
			double[] v = values.ToArray();
			Array.Sort(v);
			return string.Format(CultureInfo.InvariantCulture, "n={0} p50={1:F1} p95={2:F1} p99={3:F1} max={4:F1}",
				v.Length, v[v.Length / 2], v[(int) (v.Length * 0.95)], v[Math.Min(v.Length - 1, (int) (v.Length * 0.99))], v[v.Length - 1]);
		}

		private static string Counts() {
			int onGround = 0, chars = 0, npcs = 0;
			foreach (Thing t in Thing.AllThings) {
				if (t is AbstractCharacter ch) {
					chars++;
					if (!ch.IsPlayer) {
						npcs++;
					}
				} else if (t.IsOnGround) {
					onGround++;
				}
			}
			return string.Format(CultureInfo.InvariantCulture, "items={0} groundItems={1} chars={2} npcs={3} timers={4} heapMB={5:F0} wsMB={6:F0}",
				AbstractItem.Instances, onGround, chars, npcs, TimerCount, GC.GetTotalMemory(false) / 1048576.0,
				Process.GetCurrentProcess().WorkingSet64 / 1048576.0);
		}

		private static void Out(string what, string line) {
			Console.WriteLine("PERF_PROBE: " + what + " " + line + " |");
		}

		[SteamFunction]
		public static void TestPerf(Character self, ScriptArgs sa) {
			//#1179 - only on a test server: a GM's Test* commands pass the default rule on a live shard too (#951), and this one
			//spawns tens of thousands of things, moves players and starts a thread that never stops
			if (Environment.GetEnvironmentVariable(TestHarness.CommandsVariable) != "1") {
				self.SysMessage(".TestPerf runs only on a test server.");
				return;
			}
			string[] a = sa.Args.Trim().Trim('"').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (a.Length == 0) {
				self.SysMessage(".TestPerf \"<what> <args>\": see performance-review.md section 9.");
				return;
			}
			string what = a[0];
			Stopwatch sw = Stopwatch.StartNew();
			switch (what) {
				case "autoaccounts":
					Globals.AutoAccountCreation = true;
					Out(what, "on");
					break;
				case "npcs": { //npcs n plane x y w h
						int n = int.Parse(a[1]); byte m = byte.Parse(a[2]);
						int x0 = int.Parse(a[3]), y0 = int.Parse(a[4]), w = int.Parse(a[5]), h = int.Parse(a[6]);
						int itemsBefore = AbstractItem.Instances;
						Map map = Map.GetMap(m);
						for (int i = 0; i < n; i++) {
							int x = x0 + rnd.Next(w), y = y0 + rnd.Next(h);
							int z = map.GetFixedZ(new Point3D(x, y, 100));
							Thing t = ThingDef.GetByDefname(npcDefs[i % npcDefs.Length]).Create(x, y, z, m);
							populated.Add(t);
							if (t is NPC npc) {
								MonsterLoot.FlushCreatureStart(npc);
							}
						}
						Out(what, n + " in " + sw.ElapsedMilliseconds + "ms itemsAdded=" + (AbstractItem.Instances - itemsBefore) + " " + Counts());
						break;
					}
				case "ground": { //ground n plane x y w h
						int n = int.Parse(a[1]); byte m = byte.Parse(a[2]);
						int x0 = int.Parse(a[3]), y0 = int.Parse(a[4]), w = int.Parse(a[5]), h = int.Parse(a[6]);
						Map map = Map.GetMap(m);
						for (int i = 0; i < n; i++) {
							int x = x0 + rnd.Next(w), y = y0 + rnd.Next(h);
							int z = map.GetFixedZ(new Point3D(x, y, 100));
							Item item = (Item) ThingDef.GetByDefname(groundDefs[i % groundDefs.Length]).Create(x, y, z, m);
							item.SetAttribute(ItemAttributes.attr_move_never, true);
							populated.Add(item);
						}
						Out(what, n + " in " + sw.ElapsedMilliseconds + "ms " + Counts());
						break;
					}
				case "rects": { //rects file npcs groundItems: spread over "plane x y w h name" lines by area
						var rects = File.ReadAllLines(a[1]).Select(l => l.Split(' ')).Select(p => (m: byte.Parse(p[0]), x: int.Parse(p[1]), y: int.Parse(p[2]), w: int.Parse(p[3]), h: int.Parse(p[4]))).ToList();
						int nNpcs = int.Parse(a[2]), nItems = int.Parse(a[3]);
						long area = rects.Sum(r => (long) r.w * r.h);
						int itemsBefore = AbstractItem.Instances;
						int madeNpcs = 0, madeItems = 0;
						foreach (var r in rects) {
							Map map = Map.GetMap(r.m);
							int npcsHere = (int) Math.Round(nNpcs * (double) r.w * r.h / area);
							int itemsHere = (int) Math.Round(nItems * (double) r.w * r.h / area);
							for (int i = 0; i < npcsHere; i++) {
								int x = r.x + rnd.Next(r.w), y = r.y + rnd.Next(r.h);
								Thing t = ThingDef.GetByDefname(npcDefs[rnd.Next(npcDefs.Length)]).Create(x, y, map.GetFixedZ(new Point3D(x, y, 100)), r.m);
								if (t is NPC npc) {
									MonsterLoot.FlushCreatureStart(npc);
								}
								madeNpcs++;
							}
							for (int i = 0; i < itemsHere; i++) {
								int x = r.x + rnd.Next(r.w), y = r.y + rnd.Next(r.h);
								Item item = (Item) ThingDef.GetByDefname(groundDefs[rnd.Next(groundDefs.Length)]).Create(x, y, map.GetFixedZ(new Point3D(x, y, 100)), r.m);
								item.SetAttribute(ItemAttributes.attr_move_never, true);
								madeItems++;
							}
						}
						Out(what, string.Format(CultureInfo.InvariantCulture, "npcs={0} ground={1} packItems={2} in {3}ms {4}",
							madeNpcs, madeItems, AbstractItem.Instances - itemsBefore - madeItems, sw.ElapsedMilliseconds, Counts()));
						break;
					}
				case "stored": { //stored containers itemsEach - player banks/houses: never on a map anyone visits
						int n = int.Parse(a[1]), each = int.Parse(a[2]);
						for (int i = 0; i < n; i++) {
							Container box = (Container) ThingDef.GetByDefname("i_backpack").Create(100 + (i % 2000), 100 + (i / 2000), 0, 250);
							for (int j = 0; j < each; j++) {
								ThingDef.GetByDefname(packDefs[j % packDefs.Length]).Create(box);
							}
						}
						Out(what, n + "x" + each + " in " + sw.ElapsedMilliseconds + "ms " + Counts());
						break;
					}
				case "start":
					StartMeasure();
					Out(what, "ok");
					break;
				case "report":
					Out(what, a.Length > 1 ? a[1] : "", true);
					break;
				case "save":
				case "savesplit": { //#1203 - savesplit formats each file into memory first: formatting and writing timed apart
						bool split = (a[0] == "savesplit");
						WorldSaver.FormatInMemory = split;
						try {
							Globals.Save();
						} finally {
							WorldSaver.FormatInMemory = false;
						}
						long ms = sw.ElapsedMilliseconds;
						string newest = Directory.GetDirectories("saves", "*", SearchOption.AllDirectories)
							.Where(d => Directory.GetFiles(d).Length > 0).OrderByDescending(Directory.GetLastWriteTimeUtc).First();
						long bytes = Directory.GetFiles(newest).Sum(f => new FileInfo(f).Length);
						Out(what, string.Format(CultureInfo.InvariantCulture, "ms={0} sizeMB={1:F1} dir={2} {3}", ms, bytes / 1048576.0, newest, Counts()));
						Out(what, "timings " + WorldSaver.LastTimings);
						break;
					}
				case "bench": { //bench plane x y
						byte m = byte.Parse(a[1]); int x = int.Parse(a[2]), y = int.Parse(a[3]);
						Map map = Map.GetMap(m);
						var sb = new StringBuilder();
						Bench(sb, "chars14", () => { int c = 0; foreach (var ch in map.GetCharsInRange(x, y, 14)) { c++; } return c; });
						Bench(sb, "things18", () => { int c = 0; foreach (var t in map.GetThingsInRange(x, y, 18)) { c++; } return c; });
						Bench(sb, "players18", () => { int c = 0; foreach (var t in map.GetPlayersInRange(x, y, 18)) { c++; } return c; });
						Bench(sb, "items18", () => { int c = 0; foreach (var t in map.GetItemsInRange(x, y, 18)) { c++; } return c; });
						long t0 = Stopwatch.GetTimestamp();
						int all = 0;
						foreach (Thing t in Thing.AllThings) {
							all++;
						}
						sb.AppendFormat(CultureInfo.InvariantCulture, " allThings={0} in {1:F1}ms", all, (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
						Out(what, m + "," + x + "," + y + sb);
						break;
					}
				case "scatter": { //scatter plane x y w h: every online player but self, at random in the rect, made hard to kill
						byte m = byte.Parse(a[1]);
						int x0 = int.Parse(a[2]), y0 = int.Parse(a[3]), w = int.Parse(a[4]), h = int.Parse(a[5]);
						Map map = Map.GetMap(m);
						int moved = 0;
						foreach (AbstractCharacter ch in new List<AbstractCharacter>(GameServer.GetAllPlayers())) {
							if (ch == self) {
								continue;
							}
							int x = x0 + rnd.Next(w), y = y0 + rnd.Next(h);
							Character c = (Character) ch;
							c.Str = 30000;
							c.Hits = c.MaxHits;
							c.Go(x, y, map.GetFixedZ(new Point3D(x, y, 100)), m);
							moved++;
						}
						Out(what, moved + " moved");
						break;
					}
				case "players":
					Out(what, GameServer.GetAllPlayers().Count().ToString(CultureInfo.InvariantCulture));
					break;
				case "counts":
					Out(what, Counts());
					break;
				default:
					Out(what, "unknown");
					break;
			}
		}

		private static void Bench(StringBuilder sb, string name, Func<int> query) {
			int found = query();
			const int Runs = 2000;
			long alloc0 = GC.GetAllocatedBytesForCurrentThread();
			long t0 = Stopwatch.GetTimestamp();
			for (int i = 0; i < Runs; i++) {
				query();
			}
			double us = (Stopwatch.GetTimestamp() - t0) * 1e6 / Stopwatch.Frequency / Runs;
			long bytes = (GC.GetAllocatedBytesForCurrentThread() - alloc0) / Runs;
			sb.AppendFormat(CultureInfo.InvariantCulture, " {0}: found={1} {2:F1}us {3}B", name, found, us, bytes);
		}

		private static void StartMeasure() {
			if (probe == null) {
				probe = new LatenessTimer();
				lockProberRun = true;
				object gl = GlobalLock;
				lockProber = new Thread(() => {
					while (lockProberRun) {
						long t0 = Stopwatch.GetTimestamp();
						lock (gl) {
						}
						double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
						lock (lockProbeSync) {
							lockProbeMs.Add(ms);
						}
						Thread.Sleep(20);
					}
				});
				lockProber.IsBackground = true;
				lockProber.Start();
			}
			probe.latenessMs.Clear();
			probe.last = 0;
			lock (lockProbeSync) {
				lockProbeMs.Clear();
			}
			PerfCounters.Enabled = true; //#1179 - off by default; switching it on starts a fresh window
			PerfCounters.Reset();
			Process p = Process.GetCurrentProcess();
			cpuAtStart = p.TotalProcessorTime;
			wallAtStart = Stopwatch.GetTimestamp();
			gc0 = GC.CollectionCount(0);
			gc1 = GC.CollectionCount(1);
			gc2 = GC.CollectionCount(2);
			pauseAtStart = GC.GetTotalPauseDuration();
			allocAtStart = GC.GetTotalAllocatedBytes();
		}

		private static void Out(string what, string label, bool report) {
			double wallS = (Stopwatch.GetTimestamp() - wallAtStart) / (double) Stopwatch.Frequency;
			double cpuS = (Process.GetCurrentProcess().TotalProcessorTime - cpuAtStart).TotalSeconds;
			string lockProbes;
			lock (lockProbeSync) {
				lockProbes = Pct(lockProbeMs);
			}
			string line = string.Format(CultureInfo.InvariantCulture,
				"{0} wall={1:F1}s cpu={2:F0}%core gc0/1/2={3}/{4}/{5} gcPause={6:F0}ms allocMBps={7:F1} players={8} {9}\n  timerLatenessMs {10}\n  lockWaitProbeMs {11}\n  {12}",
				label, wallS, 100 * cpuS / wallS,
				GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1, GC.CollectionCount(2) - gc2,
				(GC.GetTotalPauseDuration() - pauseAtStart).TotalMilliseconds,
				(GC.GetTotalAllocatedBytes() - allocAtStart) / 1048576.0 / wallS,
				GameServer.GetAllPlayers().Count(), Counts(), Pct(probe.latenessMs), lockProbes, PerfCounters.Report(14));
			Out(what, line.Replace("\n", " ## "));
		}
	}
}
