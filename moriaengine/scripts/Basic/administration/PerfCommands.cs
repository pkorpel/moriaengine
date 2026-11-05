using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1179 - where the game's time goes, live: time spent holding the global lock, by what held it (packets by type,
	/// timers by type, the movement and sync queues), and time spent waiting for it. Off by default (steamengine.ini's
	/// [diagnostics] perfCounters). GM commands, by the default rule (plevel 4).</summary>
	public static class PerfCommands {
		private const int TopHolders = 15;

		/// <summary>.perf: the counts since the counters were switched on or last reset.</summary>
		[SteamFunction]
		public static void Perf(Character self) {
			string report = PerfCounters.Report(TopHolders);
			Console.WriteLine("PERF: " + report.Replace("\n", "\n  PERF: "));
			foreach (string line in report.Split('\n')) {
				self.SysMessage(line.Trim());
			}
			if (!PerfCounters.Enabled) {
				self.SysMessage("The counters are off: .perfon switches them on.");
			}
		}

		[SteamFunction]
		public static void PerfOn(Character self) {
			PerfCounters.Enabled = true;
			PerfCounters.Reset(); //a fresh window even when it was on already (steamengine.ini's perfCounters)
			Console.WriteLine("PERF: counters on, by " + self.Name);
			self.SysMessage("Perf counters on, counting from now. .perf shows them, .perfoff stops them.");
		}

		[SteamFunction]
		public static void PerfOff(Character self) {
			PerfCounters.Enabled = false;
			Console.WriteLine("PERF: counters off, by " + self.Name);
			self.SysMessage("Perf counters off. .perf still shows what they counted.");
		}

		[SteamFunction]
		public static void PerfReset(Character self) {
			PerfCounters.Reset();
			Console.WriteLine("PERF: counters reset, by " + self.Name);
			self.SysMessage("Perf counters reset.");
		}
	}
}
