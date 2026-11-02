using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1038 - the AI runs in real time, so each step here sets the scene and asks later.</summary>
	public static class NpcUnseenTargetTests {
		private static NPC hunter;

		[SteamFunction]
		public static void TestUnseenStart(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Account.PLevel = 1;
			hunter = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 4, self.Y, self.Z, self.M);
			hunter.AggroMode = AggroMode.Hostile;
			NPCAiTimer.EnsureInstalled(hunter);
			NPCAiTimer.GetFor(hunter).EngageTarget(self);
			Console.WriteLine("UNSEEN_STARTED");
		}

		[SteamFunction]
		public static void TestUnseenGoInvisible(Character self) {
			InvisibilitySpellDef.MakeInvisible(self, 120);
			Console.WriteLine("UNSEEN_INVISIBLE");
		}

		[SteamFunction]
		public static void TestUnseenBeSeen(Character self) {
			self.Flag_InvisByMagic = false;
			self.Flag_Hidden = false;
			Console.WriteLine("UNSEEN_VISIBLE");
		}

		[SteamFunction]
		public static void TestUnseenHide(Character self) {
			self.Flag_Hidden = true;
			Console.WriteLine("UNSEEN_HIDDEN");
		}

		[SteamFunction]
		public static void TestUnseenDetector(Character self) {
			hunter.Delete();
			hunter = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_detect_attacker")).Create(self.X + 1, self.Y, self.Z, self.M);
			hunter.AggroMode = AggroMode.Hostile;
			NPCAiTimer.EnsureInstalled(hunter);
			NPCAiTimer.GetFor(hunter).EngageTarget(self);
			Console.WriteLine("UNSEEN_DETECTOR hasDetect=" + MonsterSpecialAttacks.HasDetect(hunter));
		}

		[SteamFunction]
		public static void TestUnseenReport(Character self) {
			NPCAiTimer timer = NPCAiTimer.GetFor(hunter);
			Console.WriteLine("UNSEEN_REPORT: seq=" + (++reports) + " targetIsMe=" + (timer.Target == self) + " war=" + hunter.Flag_WarMode +
				" state=" + timer.LastState + " invisible=" + self.Flag_InvisByMagic + " hidden=" + self.Flag_Hidden + " end");
		}

		private static int reports;
	}
}
