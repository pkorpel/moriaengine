using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - server-side helpers for Tests/Protocol/ChatTests.cs.</summary>
	public static class ChatTests {
		private static int fails;

		private static void Check(string name, bool ok) {
			if (!ok) {
				fails++;
				Console.WriteLine("CHAT_FAIL: " + name);
			}
		}

		private static Realm RealmOf(string letter) {
			switch (letter) {
				case "G": return Realm.Gondor;
				case "M": return Realm.Mordor;
				case "g": return Realm.GondorOutcast;
				default: return Realm.None;
			}
		}

		/// <summary>Plain, of a realm, with fresh chat settings and no wait.</summary>
		[SteamFunction]
		public static void TestChatReady(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			self.Realm = RealmOf(sa.Args.Trim().Trim('"'));
			self.RemoveTag(TagKey.Acquire("_chat_flipped_"));
			self.RemoveTag(TagKey.Acquire("_chat_muted_names_"));
			self.RemoveTag(TagKey.Acquire("_chat_silenced_"));
			self.RemoveTag(TagKey.Acquire("_chat_guild_box_"));
			self.RemoveTag(TagKey.Acquire("_chat_next_message_"));
			ChatSettings.instance.secondsBetweenMessages = 0;
			Console.WriteLine("CHAT_READY: uid=" + (uint) self.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestChatRate(Character self, ScriptArgs sa) {
			ChatSettings.instance.secondsBetweenMessages = double.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			Console.WriteLine("CHAT_RATE: done");
		}

		[SteamFunction]
		public static void TestChatGm(Character self) {
			self.Account.PLevel = self.Account.MaxPLevel;
			Console.WriteLine("CHAT_GM: done");
		}

		[SteamFunction]
		public static void TestChatSilence(Character self, ScriptArgs sa) {
			ChatSystem.SetSilenced(self, sa.Args.Trim().Trim('"') == "1");
			Console.WriteLine("CHAT_SILENCED: done");
		}

		/// <summary>Who hears what, for every kind of listener.</summary>
		[SteamFunction]
		public static void TestChatRules(Character self) {
			fails = 0;
			self.Account.PLevel = 1;
			self.RemoveTag(TagKey.Acquire("_chat_flipped_"));
			self.RemoveTag(TagKey.Acquire("_chat_muted_names_"));

			self.Realm = Realm.Gondor;
			Check("gondor hears gondor", ChatSystem.Hears(self, ChatChannel.Gondor, "X"));
			Check("gondor doesn't hear mordor", !ChatSystem.Hears(self, ChatChannel.Mordor, "X"));
			Check("not even with the switch", !Flipped(self, ChatChannel.Mordor));
			Check("gondor hears all", ChatSystem.Hears(self, ChatChannel.All, "X"));
			Check("gondor hears no GMs", !ChatSystem.Hears(self, ChatChannel.Gm, "X"));

			self.Realm = Realm.Mordor;
			Check("mordor hears mordor", ChatSystem.Hears(self, ChatChannel.Mordor, "X"));
			Check("mordor doesn't hear gondor", !ChatSystem.Hears(self, ChatChannel.Gondor, "X"));

			foreach (Realm outsider in new[] { Realm.GondorOutcast, Realm.MordorOutcast, Realm.None }) {
				self.Realm = outsider;
				Check(outsider + " hears no realm", !ChatSystem.Hears(self, ChatChannel.Gondor, "X") && !ChatSystem.Hears(self, ChatChannel.Mordor, "X"));
				Check(outsider + " hears all", ChatSystem.Hears(self, ChatChannel.All, "X"));
			}

			self.Realm = Realm.Gondor;
			ChatSystem.Flip(self, ChatChannel.Gondor);
			Check("off is off", !ChatSystem.Hears(self, ChatChannel.Gondor, "X"));
			ChatSystem.Flip(self, ChatChannel.Gondor);
			ChatSystem.Flip(self, ChatChannel.All);
			self.Realm = Realm.Mordor;
			Check("a new realm brings its channel, what's off stays off", ChatSystem.Hears(self, ChatChannel.Mordor, "X") && !ChatSystem.Hears(self, ChatChannel.All, "X"));
			ChatSystem.Flip(self, ChatChannel.All);
			self.Realm = Realm.Gondor;

			Check("a name goes on the list", ChatSystem.Mute(self, "  Sauron "));
			Check("not twice", !ChatSystem.Mute(self, "sauron"));
			Check("muted in any case", !ChatSystem.Hears(self, ChatChannel.Gondor, "SAURON"));
			Check("others aren't", ChatSystem.Hears(self, ChatChannel.Gondor, "Gandalf"));
			Check("not an announcement of nobody", ChatSystem.Hears(self, ChatChannel.Gondor, null));
			for (int i = 1; i < ChatSystem.MaxMuted; i++) {
				ChatSystem.Mute(self, "Name" + i);
			}
			Check("ten at most", !ChatSystem.Mute(self, "Eleventh") && (ChatSystem.MutedNames(self).Count == ChatSystem.MaxMuted));
			ChatSystem.Unmute(self, 0);
			Check("off the list", ChatSystem.Hears(self, ChatChannel.Gondor, "Sauron"));

			self.Account.PLevel = self.Account.MaxPLevel;
			Check("a GM hears both realms", ChatSystem.Hears(self, ChatChannel.Gondor, "X") && ChatSystem.Hears(self, ChatChannel.Mordor, "X"));
			Check("a GM hears GMs", ChatSystem.Hears(self, ChatChannel.Gm, "X"));
			self.Account.PLevel = 1;
			self.RemoveTag(TagKey.Acquire("_chat_flipped_"));
			self.RemoveTag(TagKey.Acquire("_chat_muted_names_"));
			Console.WriteLine("CHAT_RULES: fails=" + fails + " end");
		}

		private static bool Flipped(Character self, ChatChannel channel) {
			ChatSystem.Flip(self, channel);
			bool hears = ChatSystem.Hears(self, channel, "X");
			ChatSystem.Flip(self, channel);
			return hears;
		}

		[SteamFunction]
		public static void TestChatGuild(Character self) {
			GuildStone stone = (GuildStone) ThingDef.GetByDefname("i_guildstone").Create(self.X + 1, self.Y, self.Z, self.M);
			Guild guild = GuildDef.NewGuild(stone);
			guild.AddMember(self);
			guild.Master = self;
			Console.WriteLine("CHAT_GUILD: done");
		}

		[SteamFunction]
		public static void TestChatRingLost(Character self) {
			Player player = (Player) self;
			player.Realm = Realm.Mordor;
			player.SkillRank = SkillRank.Grandmaster;
			PowerBadge ring = NazgulRings.Give(player, null);
			NazgulRings.TakeAway(ring, player);
			Console.WriteLine("CHAT_RING: done");
		}
	}
}
