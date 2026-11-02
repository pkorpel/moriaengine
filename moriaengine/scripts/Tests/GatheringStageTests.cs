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

		/// <summary>#976 - no light at all, on open ground with nothing lit near.</summary>
		[SteamFunction]
		public static void TestDarkBlow(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			foreach (AbstractItem thing in self.GetMap().GetItemsInRange(self.X, self.Y, 3)) {
				if ((thing is Item item) && CraftingSkillDef.IsOfType(item, "t_light_lit")) {
					item.Delete();
				}
			}
			LightAndWeather.DayLight = 65;
			LightAndWeather.NightLight = 65;
			NPC target = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			target.Hits = 1000;
			WeaponSkillTargetQueuePlugin.AddTarget(self, target);
			Console.WriteLine("DARK_BLOW: started");
		}

		[SteamFunction]
		public static void TestLightBack(Character self) {
			LightAndWeather.DayLight = 0;
			LightAndWeather.NightLight = 0;
			Console.WriteLine("LIGHT_BACK: done");
		}

		[SteamFunction]
		public static void TestGatherDelays(Character self) {
			Console.WriteLine("GATHER_DELAYS: at100=" + GatheringDelaySystem.GetDelaySecondsForSkillPercent(100).ToString(CultureInfo.InvariantCulture)
				+ " at110=" + GatheringDelaySystem.GetDelaySecondsForSkillPercent(110).ToString("0.0", CultureInfo.InvariantCulture)
				+ " strokes=" + GatheringDelaySystem.instance.leastStrokes + "-" + GatheringDelaySystem.instance.mostStrokes);
		}
	}
}
