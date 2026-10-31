using System.Linq;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - a GM's text is parsed as a script: .goch "hello".</summary>
	public static class ChatCommands {
		private static string Text(ScriptArgs sa) {
			if ((sa == null) || (sa.Argv == null) || (sa.Argv.Length == 0)) {
				return null;
			}
			return string.Join(" ", sa.Argv.Select(o => o.ToString()));
		}

		[SteamFunction]
		public static void Zprava(Character self) {
			if (self is Player player) {
				D_Zprava.Show(player);
			}
		}

		[SteamFunction]
		public static void Chat(Character self) {
			if (self is Player player) {
				D_ChatOptions.Show(player);
			}
		}

		[SteamFunction]
		public static void GuildScream(Character self) {
			string where = (self.Region != null) ? self.Region.Name : "";
			ChatSystem.SendToGuild(self, "Pomoc ! Jsem na souradnicich " + self.X + ", " + self.Y + " - " + where);
		}

		[SteamFunction]
		public static void Goch(Character self, ScriptArgs sa) {
			ChatSystem.SendToRealm(self, ChatChannel.Gondor, Text(sa));
		}

		[SteamFunction]
		public static void Evch(Character self, ScriptArgs sa) {
			ChatSystem.SendToRealm(self, ChatChannel.Mordor, Text(sa));
		}

		[SteamFunction]
		public static void Gmch(Character self, ScriptArgs sa) {
			ChatSystem.SendToEveryone(self, ChatChannel.Gm, Text(sa));
		}

		[SteamFunction]
		public static void All(Character self, ScriptArgs sa) {
			ChatSystem.SendToEveryone(self, ChatChannel.All, Text(sa));
		}

		[SteamFunction]
		public static void Mute(Player self) {
			self.Target(SingletonScript<Targ_ChatMute>.Instance);
		}
	}

	public class Targ_ChatMute : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			bool silenced = !ChatSystem.IsSilenced(targetted);
			ChatSystem.SetSilenced(targetted, silenced);
			self.SysMessage(targetted.Name + (silenced ? " muted." : " unmuted."));
			return TargetResult.Done;
		}
	}
}
