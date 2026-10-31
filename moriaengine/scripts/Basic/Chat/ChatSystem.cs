using System;
using System.Collections.Generic;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - Erebor's systems/chat.scp: server-wide channels, heard as system messages. Erebor
	/// refused ( [ ] &lt; { to keep them out of Sphere scripts, which has no meaning here.</summary>
	public static class ChatSystem {
		public const int MaxMuted = 10;
		private const ChatChannel GondorDefault = ChatChannel.Gondor | ChatChannel.All | ChatChannel.Gm | ChatChannel.Guild;
		private const ChatChannel MordorDefault = ChatChannel.Mordor | ChatChannel.All | ChatChannel.Gm | ChatChannel.Guild;

		private static readonly TagKey flippedTK = TagKey.Acquire("_chat_flipped_");
		private static readonly TagKey mutedNamesTK = TagKey.Acquire("_chat_muted_names_");
		private static readonly TagKey silencedTK = TagKey.Acquire("_chat_silenced_");
		private static readonly TagKey nextMessageTK = TagKey.Acquire("_chat_next_message_");
		private static readonly TagKey guildBoxTK = TagKey.Acquire("_chat_guild_box_");

		/// <summary>initChat: everything but the other realm, and a GM has both.</summary>
		private static ChatChannel DefaultChannels(Character ch) {
			if (ch.IsGM) {
				return GondorDefault | MordorDefault;
			}
			switch (ch.Realm) {
				case Realm.Gondor:
					return GondorDefault;
				case Realm.Mordor:
					return MordorDefault;
				default:
					return ChatChannel.All | ChatChannel.Gm | ChatChannel.Guild;
			}
		}

		/// <summary>What's kept is what the player changed, so a new realm brings its own channel.</summary>
		public static ChatChannel ChannelsOf(Character ch) {
			object flipped = ch.GetTag(flippedTK);
			return (flipped != null) ? (DefaultChannels(ch) ^ (ChatChannel) Convert.ToInt32(flipped)) : DefaultChannels(ch);
		}

		public static bool IsOn(Character ch, ChatChannel channel) {
			return (ChannelsOf(ch) & channel) != ChatChannel.None;
		}

		public static void Flip(Character ch, ChatChannel channel) {
			object flipped = ch.GetTag(flippedTK);
			ch.SetTag(flippedTK, ((flipped != null) ? Convert.ToInt32(flipped) : 0) ^ (int) channel);
		}

		public static List<string> MutedNames(Character ch) {
			return (ch.GetTag(mutedNamesTK) as List<string>) ?? new List<string>();
		}

		public static bool Mute(Character ch, string name) {
			List<string> names = new List<string>(MutedNames(ch));
			name = name.Trim();
			if ((name.Length == 0) || (names.Count >= MaxMuted) || names.Exists(muted => string.Equals(muted, name, StringComparison.OrdinalIgnoreCase))) {
				return false;
			}
			names.Add(name);
			ch.SetTag(mutedNamesTK, names);
			return true;
		}

		public static void Unmute(Character ch, int index) {
			List<string> names = new List<string>(MutedNames(ch));
			if ((index >= 0) && (index < names.Count)) {
				names.RemoveAt(index);
				if (names.Count == 0) {
					ch.RemoveTag(mutedNamesTK);
				} else {
					ch.SetTag(mutedNamesTK, names);
				}
			}
		}

		private static bool HasMuted(Character listener, string senderName) {
			return (senderName != null) && MutedNames(listener).Exists(muted => string.Equals(muted, senderName, StringComparison.OrdinalIgnoreCase));
		}

		public static bool IsSilenced(Character ch) {
			return ch.HasTag(silencedTK);
		}

		/// <summary>.mute: no realm chat for them, as on Erebor.</summary>
		public static void SetSilenced(Character ch, bool silenced) {
			if (silenced) {
				ch.SetTag(silencedTK, true);
			} else {
				ch.RemoveTag(silencedTK);
			}
		}

		public static bool GuildBox(Character ch) {
			return ch.HasTag(guildBoxTK);
		}

		public static void SetGuildBox(Character ch, bool on) {
			if (on) {
				ch.SetTag(guildBoxTK, true);
			} else {
				ch.RemoveTag(guildBoxTK);
			}
		}

		/// <summary>f_chatrecieve, but for the guild: everyone hears who's muted no one, on the channel,
		/// and a realm's channel is for its realm and for GMs.</summary>
		public static bool Hears(Character listener, ChatChannel channel, string senderName) {
			if (HasMuted(listener, senderName) || !IsOn(listener, channel)) {
				return false;
			}
			switch (channel) {
				case ChatChannel.Gondor:
					return (listener.Realm == Realm.Gondor) || listener.IsGM;
				case ChatChannel.Mordor:
					return (listener.Realm == Realm.Mordor) || listener.IsGM;
				case ChatChannel.Gm:
					return listener.IsGM;
				default:
					return true;
			}
		}

		private static int HueOf(ChatChannel channel) {
			ChatSettings settings = ChatSettings.instance;
			switch (channel) {
				case ChatChannel.Gondor:
					return settings.gondorHue;
				case ChatChannel.Mordor:
					return settings.mordorHue;
				case ChatChannel.Gm:
					return settings.gmHue;
				case ChatChannel.Guild:
					return settings.guildHue;
				default:
					return settings.allHue;
			}
		}

		private static void Deliver(IEnumerable<Character> listeners, ChatChannel channel, string senderName, string line) {
			int hue = HueOf(channel);
			foreach (Character listener in listeners) {
				if (listener.IsOnline && Hears(listener, channel, senderName)) {
					listener.SysMessage(line, hue);
				}
			}
		}

		private static IEnumerable<Character> EveryoneOnline() {
			foreach (AbstractCharacter player in GameServer.GetAllPlayers()) {
				if (player is Character ch) {
					yield return ch;
				}
			}
		}

		/// <summary>The checks of every sender: there's text, it's not too long, not too soon. The text
		/// comes back trimmed and cut, null if it's not going anywhere.</summary>
		private static string Prepared(Character sender, string text) {
			text = (text ?? "").Trim();
			if (text.Length == 0) {
				return null;
			}
			ChatSettings settings = ChatSettings.instance;
			if (!sender.IsGM) {
				object next = sender.GetTag(nextMessageTK);
				double wait = (next != null) ? (Convert.ToDouble(next) - Globals.TimeInSeconds) : 0;
				if (wait > 0) {
					sender.RedMessage("Pockej chvili, nez posles dalsi zpravu.");
					return null;
				}
				sender.SetTag(nextMessageTK, Globals.TimeInSeconds + settings.secondsBetweenMessages);
			}
			if (text.Length > settings.maxLength) {
				sender.RedMessage("Zprava byla prilis dlouha, proto byla zkracena.");
				text = text.Substring(0, settings.maxLength);
			}
			return text;
		}

		private static bool IsOnWithMessage(Character sender, ChatChannel channel) {
			if (IsOn(sender, channel)) {
				return true;
			}
			sender.SysMessage("You are not currently on that channel.");
			return false;
		}

		private static string Line(Character sender, string text) {
			return "*" + sender.Name + "* " + text;
		}

		/// <summary>.zprava without the guild box: to the sender's realm.</summary>
		public static void SendToRealm(Character sender, string text) {
			switch (sender.Realm) {
				case Realm.Gondor:
					SendToRealm(sender, ChatChannel.Gondor, text);
					break;
				case Realm.Mordor:
					SendToRealm(sender, ChatChannel.Mordor, text);
					break;
				default:
					sender.RedMessage("Nepatris k zadne risi, nemas komu psat.");
					break;
			}
		}

		/// <summary>goch, evch. A GM can use either.</summary>
		public static void SendToRealm(Character sender, ChatChannel channel, string text) {
			Realm realm = (channel == ChatChannel.Gondor) ? Realm.Gondor : Realm.Mordor;
			if (((sender.Realm != realm) && !sender.IsGM) || !IsOnWithMessage(sender, channel)) {
				return;
			}
			if (IsSilenced(sender)) {
				sender.SysMessage("You try to chat, but find yourself mute!");
				return;
			}
			text = Prepared(sender, text);
			if (text != null) {
				Console.WriteLine(channel + " chat: " + sender.Name + ": " + text);
				Deliver(EveryoneOnline(), channel, sender.Name, Line(sender, text));
			}
		}

		public static void SendToGuild(Character sender, string text) {
			Guild guild = Guild.GetGuild(sender);
			if (guild == null) {
				sender.SysMessage("Nejsi clenem zadne guildy.");
				return;
			}
			if (IsOnWithMessage(sender, ChatChannel.Guild)) {
				text = Prepared(sender, text);
				if (text != null) {
					Console.WriteLine("Guild chat: " + sender.Name + ": " + text);
					Deliver(guild.Members, ChatChannel.Guild, sender.Name, Line(sender, text));
				}
			}
		}

		/// <summary>gmch, All.</summary>
		public static void SendToEveryone(Character sender, ChatChannel channel, string text) {
			if (IsOnWithMessage(sender, channel)) {
				text = Prepared(sender, text);
				if (text != null) {
					Deliver(EveryoneOnline(), channel, sender.Name, Line(sender, text));
				}
			}
		}

		/// <summary>guildMsg: from nobody in particular, so no one's muted it.</summary>
		public static void GuildNotice(Guild guild, string text) {
			Deliver(guild.Members, ChatChannel.Guild, null, text);
		}

		/// <summary>What a lost Ring of Power says: nobody's sending it, so it's not limited.</summary>
		public static void Announce(ChatChannel channel, string speaker, string text) {
			Deliver(EveryoneOnline(), channel, speaker, "*" + speaker + "* " + text);
		}
	}
}
