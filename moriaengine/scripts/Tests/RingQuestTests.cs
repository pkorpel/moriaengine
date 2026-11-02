using System;
using System.Globalization;
using System.Linq;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1009 - helpers for Tests/Protocol/RingQuestTests.cs.</summary>
	public static class RingQuestTests {
		private static Player killer;

		[SteamFunction]
		public static void TestRingKiller(Character self) {
			killer = (Player) self;
			killer.Realm = Realm.Mordor;
			killer.Level = 30;
			killer.RemoveTag(NazgulRings.questKillTimesTK);
			Console.WriteLine("RING_KILLER: done");
		}

		[SteamFunction]
		public static void TestRingVictim(Character self, ScriptArgs sa) {
			self.Realm = Realm.Gondor;
			((Player) self).Level = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			Console.WriteLine("RING_VICTIM: done");
		}

		[SteamFunction]
		public static void TestRingKilled(Character self, ScriptArgs sa) {
			for (int i = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture); i > 0; i--) {
				NazgulRings.OnDeath((Player) self, killer);
			}
			Console.WriteLine("RING_KILLED: done");
		}

		/// <summary>Every remembered kill, that many seconds older.</summary>
		[SteamFunction]
		public static void TestRingAge(Character self, ScriptArgs sa) {
			double seconds = double.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			string times = (string) killer.GetTag(NazgulRings.questKillTimesTK);
			killer.SetTag(NazgulRings.questKillTimesTK, string.Join(";", times.Split(';').Select(time =>
				(double.Parse(time, CultureInfo.InvariantCulture) - seconds).ToString("R", CultureInfo.InvariantCulture))));
			Console.WriteLine("RING_AGED: done");
		}

		[SteamFunction]
		public static void TestRingReport(Character self) {
			Console.WriteLine("RING_REPORT: recent=" + NazgulRings.RecentQuestKills(killer).Count + " passed=" + NazgulRings.PassedQuest(killer));
		}
	}
}
