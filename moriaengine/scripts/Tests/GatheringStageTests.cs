using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/GatheringStageTests.cs.</summary>
	public static class GatheringStageTestHelpers {
		[SteamFunction]
		public static void TestGatherReady(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M);
			self.DeleteTimer(GatherDelayTimer.Key);
			self.Flag_Hidden = true;
			Console.WriteLine("GATHER_READY: done");
		}

		[SteamFunction]
		public static void TestGatherBegin(Character self, ScriptArgs sa) {
			int away = int.Parse(sa.Args.Trim('"'), CultureInfo.InvariantCulture);
			bool began = Gathering.Begin(self, new Point3D((ushort) (self.X + away), self.Y, self.Z), SkillName.Mining);
			Console.WriteLine("GATHER_BEGAN: " + began + " hidden=" + self.Flag_Hidden);
		}

		[SteamFunction]
		public static void TestGatherClear(Character self) {
			self.DeleteTimer(GatherDelayTimer.Key);
			Console.WriteLine("GATHER_CLEARED: done");
		}

		[SteamFunction]
		public static void TestGatherDelays(Character self) {
			Console.WriteLine("GATHER_DELAYS: at100=" + GatheringDelaySystem.GetDelaySecondsForSkillPercent(100).ToString(CultureInfo.InvariantCulture)
				+ " at110=" + GatheringDelaySystem.GetDelaySecondsForSkillPercent(110).ToString("0.0", CultureInfo.InvariantCulture)
				+ " strokes=" + GatheringDelaySystem.instance.leastStrokes + "-" + GatheringDelaySystem.instance.mostStrokes);
		}
	}
}
