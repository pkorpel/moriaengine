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
	/// Text-command interface for guilds (enhancements #14) - modeled on PartyCommands'
	/// invite/accept/decline flow, since that's the proven working pattern for role-based
	/// membership in this codebase. No gump/packet UI (see Guild.cs's class doc) - commands
	/// instead, same approach as HouseCommands for house release.
	/// </summary>
	public static class GuildCommands {
		[SteamFunction]
		public static void GuildCreate(Character self, ScriptArgs sa) {
			if (Guild.GetGuild(self) != null) {
				self.SysMessage("You are already in a guild.");
				return;
			}
			string name = JoinArgs(sa);
			if (string.IsNullOrEmpty(name)) {
				self.SysMessage("Usage: .guildcreate <name>");
				return;
			}
			if (Guild.FindByName(name) != null) {
				self.SysMessage("A guild with that name already exists.");
				return;
			}
			GuildDef.NewGuild(self, name);
			self.SysMessage("You have founded the guild '" + name + "'.");
		}

		[SteamFunction]
		public static void GuildInvite(Character self, ScriptArgs sa) {
			Player player = self as Player;
			if (player == null) {
				return;
			}
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			if (!guild.IsLeader(self)) {
				self.SysMessage("Only the guild's founder can invite new members.");
				return;
			}
			player.SysMessage("Whom do you want to invite to the guild?");
			player.Target(SingletonScript<Targ_GuildInvite>.Instance, guild);
		}

		[SteamFunction]
		public static void GuildAccept(Character self, ScriptArgs sa) {
			Guild guild = FindInvitingGuild(self);
			if (guild == null) {
				self.SysMessage("No one has invited you to a guild.");
				return;
			}
			guild.ClearInvite(self);
			RolesManagement.TryAssign(self, guild);
			self.SysMessage("You have joined the guild '" + guild.Name + "'.");
		}

		[SteamFunction]
		public static void GuildDecline(Character self, ScriptArgs sa) {
			Guild guild = FindInvitingGuild(self);
			if (guild == null) {
				self.SysMessage("No one has invited you to a guild.");
				return;
			}
			guild.ClearInvite(self);
			self.SysMessage("You decline the guild invitation.");
		}

		[SteamFunction]
		public static void GuildLeave(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			if (guild.IsLeader(self) && (guild.Members.Count > 1)) {
				self.SysMessage("As founder, you must disband the guild (.guilddisband) or there must be no one else left in it.");
				return;
			}
			RolesManagement.TryUnAssign(self, guild);
			self.SysMessage("You have left the guild.");
			if (guild.Members.Count == 0) {
				guild.Dispose();
			}
		}

		[SteamFunction]
		public static void GuildDisband(Character self, ScriptArgs sa) {
			Guild guild = Guild.GetGuild(self);
			if (guild == null) {
				self.SysMessage("You are not in a guild.");
				return;
			}
			if (!guild.IsLeader(self)) {
				self.SysMessage("Only the guild's founder can disband it.");
				return;
			}
			guild.SendGuildMessage(self, "The guild has been disbanded.");
			guild.Dispose();
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
			self.SysMessage("Founder: " + (guild.Founder != null ? guild.Founder.Name : "(none)"));
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
				self.SysMessage("Only the guild's founder can set the abbreviation.");
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
				self.SysMessage("Only the guild's founder can manage diplomacy.");
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

		private static Guild FindInvitingGuild(Character self) {
			foreach (Guild g in Guild.AllGuilds) {
				if (g.HasPendingInvite(self)) {
					return g;
				}
			}
			return null;
		}

		private static string JoinArgs(ScriptArgs sa) {
			if ((sa == null) || (sa.Argv == null) || (sa.Argv.Length == 0)) {
				return null;
			}
			return string.Join(" ", sa.Argv.Select(o => o.ToString()));
		}
	}

	public class Targ_GuildInvite : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Guild guild = (Guild) parameter;
			if (Guild.GetGuild(targetted) != null) {
				self.SysMessage(targetted.Name + " is already in a guild.");
				return TargetResult.Done;
			}
			if (!guild.TryInvite(targetted)) {
				self.SysMessage("You can't invite " + targetted.Name + ".");
				return TargetResult.Done;
			}
			self.SysMessage("You invite " + targetted.Name + " to the guild.");
			targetted.SysMessage(self.Name + " invites you to join the guild '" + guild.Name + "'. Use .guildaccept or .guilddecline.");
			return TargetResult.Done;
		}
	}
}
