using System;
using System.Globalization;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1011 - helpers for Tests/Protocol/WhoAmITests.cs.</summary>
	public static class WhoAmITests {
		private static readonly TagKey bornTK = TagKey.Acquire("_bornAt_");

		[SteamFunction]
		public static void TestWhoReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player player = (Player) self;
			player.Profession = Professions.Mage;
			player.Nation = Nation.Rohir;
			player.RefreshTitle();
			Console.WriteLine("WHO_READY: name=" + player.Name + " title=" + player.Title);
		}

		[SteamFunction]
		public static void TestWhoPlayer(Character self) {
			self.Account.PLevel = 1;
			Console.WriteLine("WHO_PLAYER: done");
		}

		[SteamFunction]
		public static void TestWhoKillRabbit(Character self) {
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 1, self.Y, self.Z, self.M);
			rabbit.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			rabbit.CauseDeath(self);
			Console.WriteLine("WHO_KILLED: done");
		}

		[SteamFunction]
		public static void TestWhoDie(Character self) {
			self.CauseDeath(null);
			Console.WriteLine("WHO_DIED: dead=" + self.Flag_Dead);
		}

		/// <summary>This character's birth, that many days ago.</summary>
		[SteamFunction]
		public static void TestWhoAge(Character self, ScriptArgs sa) {
			double days = double.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			self.SetTag(bornTK, Globals.TimeInSeconds - (days * 24 * 60 * 60) - 1);
			Console.WriteLine("WHO_AGED: done");
		}

		[SteamFunction]
		public static void TestWhoFemale(Character self) {
			self.Model = 0x191;
			Console.WriteLine("WHO_FEMALE: " + self.IsFemale);
		}
	}
}
