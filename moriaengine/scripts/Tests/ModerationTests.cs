using System;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1107 - server side of Tests/Protocol/ModerationTests.cs.</summary>
	public static class ModerationTests {
		private static Point4D before;

		[SteamFunction]
		public static void TestJailReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			before = new Point4D(self.X, self.Y, self.Z, self.M);
			Console.WriteLine("JAIL_READY: uid=" + self.Uid + " jailPoint=" + Jail.JailPoint);
		}

		/// <summary>After .jail: where it is, whether a move out lands in jail again, a login, and a monster's interest.</summary>
		[SteamFunction]
		public static void TestJailReport(Character self) {
			Point4D jail = Jail.JailPoint;
			bool inJail = Jail.IsJailed(self) && (self.M == jail.M) && (Point2D.GetSimpleDistance(self, jail) <= 2);
			self.Go(before);
			bool goRedirected = (self.M == jail.M) && (Point2D.GetSimpleDistance(self, jail) <= 2);
			self.P(before.X, before.Y, before.Z, before.M);
			Jail.OnLogin(self);
			bool loginRedirected = (self.M == jail.M) && (Point2D.GetSimpleDistance(self, jail) <= 2);
			NPC wolf = (NPC) ((NPCDef) ThingDef.GetByDefname("c_wolf")).Create(self.X + 1, self.Y, self.Z, self.M);
			wolf.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			wolf.AggroMode = AggroMode.Hostile;
			self.Account.PLevel = 1;
			bool ignored = !wolf.ShouldAggroOn(self);
			self.Account.PLevel = 7;
			wolf.Delete();
			Console.WriteLine("JAIL_REPORT: inJail=" + inJail + " goRedirected=" + goRedirected + " loginRedirected=" + loginRedirected + " monstersIgnore=" + ignored);
		}

		[SteamFunction]
		public static void TestForgiveReport(Character self) {
			bool back = (self.M == before.M) && (Point2D.GetSimpleDistance(self, before) <= 1);
			Console.WriteLine("FORGIVE_REPORT: jailed=" + Jail.IsJailed(self) + " backWhereItWas=" + back);
		}
	}
}
