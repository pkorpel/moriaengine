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

namespace SteamEngine.CompiledScripts {

	public class t_deed : CompiledTriggerGroup {

	}

	/// <summary>Guildstone (enhancements #14) - previously an empty shell. Points players at the real interaction surface (GuildCommands' text commands - see Guild.cs for why there's no gump here yet) rather than implementing a full guildstone gump.</summary>
	public class t_guildstone : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			Guild guild = Guild.GetGuild(clicker);
			if (guild == null) {
				clicker.SysMessage("You are not in a guild. Use .guildcreate <name> to found one.");
			} else {
				clicker.SysMessage("You are a member of the guild '" + guild.Name + "'. Use .guildinfo for details.");
			}
		}
	}

	public class t_stone_town : CompiledTriggerGroup {

	}

	public class t_normal : CompiledTriggerGroup {

	}
}