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
			NPC killer = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 2, self.Y, self.Z, self.M);
			killer.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			killer.Name = "Killer Rabbit";
			self.CauseDeath(killer);
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
		public static void TestWhoPlayed(Character self, ScriptArgs sa) {
			self.SetTag(TagKey.Acquire("_playedSeconds_"), double.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture));
			Console.WriteLine("WHO_PLAYED: done");
		}

		[SteamFunction]
		public static void TestWhoGuild(Character self) {
			GuildStone stone = (GuildStone) ThingDef.GetByDefname("i_guildstone").Create(self.X, self.Y + 1, self.Z, self.M);
			Guild guild = GuildDef.NewGuild(stone);
			guild.AddMember(self);
			guild.Rename("Who Guild");
			((Player) self).Level = 12;
			((Player) self).SkillRank = SkillRank.Master;
			Console.WriteLine("WHO_GUILD: guild=" + Guild.GetGuild(self)?.Name);
		}

		[SteamFunction]
		public static void TestWhoFirst(Character self) {
			NazgulRings.Give(self, null);
			Console.WriteLine("WHO_FIRST: done");
		}

		[SteamFunction]
		public static void TestWhoMordor(Character self) {
			self.Realm = Realm.Mordor;
			Console.WriteLine("WHO_MORDOR: done");
		}
	}
}
