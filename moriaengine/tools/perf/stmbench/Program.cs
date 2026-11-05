// #1165 throwaway: what Shielded (STM) costs per access, and whether an NPC-think-shaped workload scales across cores with it.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Shielded;

static class Program {
	const int N = 30000;          //NPCs, the design world
	const int Neighbours = 50;    //things one think reads (FindTarget + abilities + movement checks), 4 fields each = 200 reads
	const int HotTargets = 1;     //mass combat: every NPC in the hot spot hits the same player

	sealed class PlainNpc { public int X, Y, Hits = 1000, Target; }
	sealed class StmNpc {
		public readonly Shielded<int> X = new Shielded<int>(), Y = new Shielded<int>(), Hits = new Shielded<int>(1000), Target = new Shielded<int>();
	}

	static long attempts;

	static void Main(string[] args) {
		Console.WriteLine($".NET {Environment.Version}, {Environment.ProcessorCount} cores, Shielded 4.4.9");
		Micro();
		Think();
		Console.WriteLine("done");
	}

	static void Report(string name, long ops, Stopwatch sw, long bytes) {
		Console.WriteLine($"  {name,-58} {sw.Elapsed.TotalMilliseconds * 1e6 / ops,9:F1} ns/op {bytes / (double) ops,8:F1} B/op");
	}

	static void Micro() {
		Console.WriteLine("== per access");
		const int Ops = 10_000_000;
		var p = new PlainNpc();
		var s = new StmNpc();
		var fields = Enumerable.Range(0, 1000).Select(i => new Shielded<int>(i)).ToArray();
		long sum = 0;
		for (int round = 0; round < 2; round++) { //first round is warm-up
			bool show = round == 1;
			long a0 = GC.GetAllocatedBytesForCurrentThread(); var sw = Stopwatch.StartNew();
			for (int i = 0; i < Ops; i++) { sum += p.X; }
			sw.Stop(); if (show) Report("plain field read", Ops, sw, GC.GetAllocatedBytesForCurrentThread() - a0);

			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops; i++) { sum += s.X.Value; }
			sw.Stop(); if (show) Report("Shielded read, outside a transaction", Ops, sw, GC.GetAllocatedBytesForCurrentThread() - a0);

			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops / 1000; i++) {
				Shield.InTransaction(() => { for (int j = 0; j < 1000; j++) { sum += fields[j].Value; } });
			}
			sw.Stop(); if (show) Report("Shielded read, 1000 distinct fields per transaction", Ops, sw, GC.GetAllocatedBytesForCurrentThread() - a0);

			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops / 1000; i++) {
				Shield.InTransaction(() => { for (int j = 0; j < 1000; j++) { fields[j].Value = j + i; } });
			}
			sw.Stop(); if (show) Report("Shielded write, 1000 distinct fields per transaction", Ops / 1000 * 1000, sw, GC.GetAllocatedBytesForCurrentThread() - a0);

			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops / 10; i++) { Shield.InTransaction(() => { }); }
			sw.Stop(); if (show) Report("empty transaction", Ops / 10, sw, GC.GetAllocatedBytesForCurrentThread() - a0);

			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops / 10; i++) { int k = i; Shield.InTransaction(() => { fields[k % 1000].Value = k; }); }
			sw.Stop(); if (show) Report("transaction writing 1 field", Ops / 10, sw, GC.GetAllocatedBytesForCurrentThread() - a0);

			var dict = new Dictionary<int, object>(); var sdict = new ShieldedDict<int, object>();
			Shield.InTransaction(() => { for (int i = 0; i < 64; i++) { dict[i] = i; sdict[i] = i; } });
			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops; i++) { if (dict.TryGetValue(i & 63, out var o)) { sum++; } }
			sw.Stop(); if (show) Report("Dictionary lookup (a tag)", Ops, sw, GC.GetAllocatedBytesForCurrentThread() - a0);
			a0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
			for (int i = 0; i < Ops / 1000; i++) {
				Shield.InTransaction(() => { for (int j = 0; j < 1000; j++) { if (sdict.TryGetValue(j & 63, out var o)) { sum++; } } });
			}
			sw.Stop(); if (show) Report("ShieldedDict lookup in a transaction", Ops, sw, GC.GetAllocatedBytesForCurrentThread() - a0);
		}
		GC.KeepAlive(sum);
	}

	//one NPC think: read 4 fields of 50 neighbours, then move (write X, Y), set Target, and in mass combat hit the target's Hits
	static int ThinkPlain(PlainNpc[] w, int i, bool hot, Random r) {
		int acc = 0;
		for (int k = 1; k <= Neighbours; k++) {
			PlainNpc n = w[(i + k) % N];
			acc += n.X + n.Y + n.Hits + n.Target;
		}
		PlainNpc me = w[i];
		me.X += r.Next(3) - 1; me.Y += r.Next(3) - 1; me.Target = acc & 1023;
		if (hot) { w[0].Hits -= 1; }
		return acc;
	}

	static void ThinkStm(StmNpc[] w, int i, bool hot, bool commute, Random r) {
		Shield.InTransaction(() => {
			Interlocked.Increment(ref attempts);
			int acc = 0;
			for (int k = 1; k <= Neighbours; k++) {
				StmNpc n = w[(i + k) % N];
				acc += n.X.Value + n.Y.Value + n.Hits.Value + n.Target.Value;
			}
			StmNpc me = w[i];
			me.X.Value = me.X.Value + r.Next(3) - 1; me.Y.Value = me.Y.Value + r.Next(3) - 1; me.Target.Value = acc & 1023;
			if (hot) {
				if (commute) { w[0].Hits.Commute((ref int h) => h--); } else { w[0].Hits.Value = w[0].Hits.Value - 1; }
			}
		});
	}

	static void Think() {
		Console.WriteLine($"== NPC think model: {N} NPCs, each think reads 4 fields of {Neighbours} neighbours and writes 3 of its own");
		var plain = Enumerable.Range(0, N).Select(_ => new PlainNpc()).ToArray();
		var stm = Enumerable.Range(0, N).Select(_ => new StmNpc()).ToArray();
		const int Passes = 4; //each pass = every NPC thinks once
		object globalLock = new object();

		for (int round = 0; round < 2; round++) {
			bool show = round == 1;
			var r = new Random(1);
			var sw = Stopwatch.StartNew();
			for (int pass = 0; pass < Passes; pass++) { for (int i = 0; i < N; i++) { ThinkPlain(plain, i, false, r); } }
			sw.Stop();
			double plainUs = sw.Elapsed.TotalMilliseconds * 1000 / (Passes * N);
			if (show) Console.WriteLine($"  plain, 1 thread (today's model)                           {plainUs,8:F2} us/think");
			attempts = 0; sw.Restart();
			for (int pass = 0; pass < Passes; pass++) { for (int i = 0; i < N; i++) { ThinkStm(stm, i, false, false, r); } }
			sw.Stop();
			if (show) Console.WriteLine($"  Shielded, 1 thread                                        {sw.Elapsed.TotalMilliseconds * 1000 / (Passes * N),8:F2} us/think  ({sw.Elapsed.TotalMilliseconds * 1000 / (Passes * N) / plainUs:F1}x plain)");
		}

		foreach (bool hot in new[] { false, true }) {
			foreach (bool commute in hot ? new[] { false, true } : new[] { false }) {
				foreach (int threads in new[] { 1, 2, 4, 8 }) {
					attempts = 0;
					long thinks = 0;
					var sw = Stopwatch.StartNew();
					var ts = Enumerable.Range(0, threads).Select(t => new Thread(() => {
						var r = new Random(t);
						//each thread takes its own slice of NPCs, as a sector-partitioned scheduler would
						int from = t * N / threads, to = (t + 1) * N / threads;
						for (int pass = 0; pass < Passes; pass++) {
							for (int i = from; i < to; i++) {
								//hot spot: the first 200 NPCs of every slice fight player 0
								ThinkStm(stm, i, hot && (i - from) < 200, commute, r);
								Interlocked.Increment(ref thinks);
							}
						}
					})).ToList();
					ts.ForEach(t => t.Start()); ts.ForEach(t => t.Join());
					sw.Stop();
					Console.WriteLine($"  Shielded, {threads} thread(s), {(hot ? (commute ? "hot spot, commute" : "hot spot") : "spread out"),-18} {thinks / sw.Elapsed.TotalSeconds,10:F0} thinks/s  retries {100.0 * (attempts - thinks) / thinks,6:F1}%");
				}
			}
		}
	}
}
