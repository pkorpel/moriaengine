/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System.Linq;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Text commands around guilds (enhancements #14): chat, info and diplomacy. Joining, ranks
	/// and the name are run from the guildstone (#884, D_GuildMenu).
	/// </summary>
	public static class GuildCommands {
		[SteamFunction]
		public static void GuildLeave(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("Nejsi clenem zadne guildy.");
				return;
			}
			guild.Resign(self);
		}

		/// <summary>#884 - Erebor's .guildMembers: who of my guild is online and where.</summary>
		[SteamFunction]
		public static void GuildMembers(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.RedMessage("Nejsi clenem zadne guildy.");
			} else if (!guild.IsFullMember(self)) {
				self.RedMessage("Jako adept nemas na tento prikaz narok.");
			} else {
				self.Dialog(self, SingletonScript<D_GuildOnline>.Instance, new DialogArgs(guild));
			}
		}

		/// <summary>#884 - Erebor's .toggleabbrev.</summary>
		[SteamFunction]
		public static void ToggleAbbrev(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild != null) {
				guild.ToggleAbbreviation(self);
			}
		}

		[SteamFunction]
		public static void GuildSay(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			string text = JoinArgs(sa);
			if (string.IsNullOrEmpty(text)) {
				return;
			}
			guild.SendGuildMessage(self, text);
		}

		[SteamFunction]
		public static void GuildInfo(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			self.SysMessage("Guild: " + guild.Name + (string.IsNullOrEmpty(guild.Abbreviation) ? "" : " [" + guild.Abbreviation + "]"));
			self.SysMessage("Master: " + (guild.Master != null ? guild.Master.Name : "(none)"));
			self.SysMessage("Members: " + string.Join(", ", guild.Members.Select(m => m.Name)));
		}

		[SteamFunction]
		public static void GuildAbbreviation(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			if (!guild.IsLeader(self)) {
				self.SysMessage("Only the guild's master can set the abbreviation.");
				return;
			}
			guild.Abbreviation = JoinArgs(sa);
			self.SysMessage("Guild abbreviation set to '" + guild.Abbreviation + "'.");
		}

		private static void WithTargetGuild(Character self, ScriptArgs sa, System.Action<Guild, Guild> action) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			if (!guild.IsLeader(self)) {
				self.SysMessage("Only the guild's master can manage diplomacy.");
				return;
			}
			string targetName = JoinArgs(sa);
			Guild other = Guild.FindByName(targetName);
			if (other == null) {
				self.SysMessage("No such guild '" + targetName + "'.");
				return;
			}
			action(guild, other);
		}

		[SteamFunction]
		public static void GuildDeclareWar(Character self, ScriptArgs sa) {
			WithTargetGuild(self, sa, (guild, other) => {
				guild.DeclareWarOn(other);
				self.SysMessage("Your guild declares war on '" + other.Name + "'.");
			});
		}

		[SteamFunction]
		public static void GuildMakePeace(Character self, ScriptArgs sa) {
			WithTargetGuild(self, sa, (guild, other) => {
				guild.MakePeaceWith(other);
				self.SysMessage("Your guild makes peace with '" + other.Name + "'.");
			});
		}

		[SteamFunction]
		public static void GuildDeclareAlliance(Character self, ScriptArgs sa) {
			WithTargetGuild(self, sa, (guild, other) => {
				guild.DeclareAllianceWith(other);
				self.SysMessage("Your guild forms an alliance with '" + other.Name + "'.");
			});
		}

		[SteamFunction]
		public static void GuildBreakAlliance(Character self, ScriptArgs sa) {
			WithTargetGuild(self, sa, (guild, other) => {
				guild.BreakAllianceWith(other);
				self.SysMessage("Your guild breaks its alliance with '" + other.Name + "'.");
			});
		}

		private static string JoinArgs(ScriptArgs sa) {
			if ((sa == null) || (sa.Argv == null) || (sa.Argv.Length == 0)) {
				return null;
			}
			return string.Join(" ", sa.Argv.Select(o => o.ToString()));
		}
	}
}
