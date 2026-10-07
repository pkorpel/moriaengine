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

using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	public class E_CommandsAuthorization_Global : CompiledTriggerGroup {
		/// <summary>
		/// Use this const for all non-player functions where the plevel number has no special meaning 
		/// except for differentiation 'player available function' / 'player forbidden function'
		/// </summary>
		const int MORE_THAN_PLAYER = 2;

		const int PLEVEL_VISITOR = 0;
		const int PLEVEL_PLAYER = 1;
		const int PLEVEL_SEER = 2;
		const int PLEVEL_COUNSELOR = 3;
		const int PLEVEL_GM = 4;
		const int PLEVEL_ADMIN = 5;
		const int PLEVEL_DEVELOPER = 6;
		const int PLEVEL_OWNER = 7;

		/// <summary>Meaning all commands except the ones for higher levels</summary>
		const int plevelToAllCommands = 4;

		Dictionary<string, int> commands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		//set the command plevels here... AND COMMENT IT!!!!!!!!!!!!!!!!!!!
		public E_CommandsAuthorization_Global() {
			//players
			this.commands["Where"] = PLEVEL_PLAYER;//Show my coordinates and map region position			
			this.commands["Sendgmpage"] = PLEVEL_PLAYER;//Post a new GMPage using an input dialog
			this.commands["Resync"] = PLEVEL_PLAYER;//resend nearby stuff
			this.commands["Messages"] = PLEVEL_PLAYER;//delayed messages board


			//GMs
			//Print the value of given expression
			this.commands["show"] = MORE_THAN_PLAYER;
			//[Show the targetting cross to perform a given expression on target
			this.commands["x"] = MORE_THAN_PLAYER;
			this.commands["Page"] = MORE_THAN_PLAYER;//Post a new GMPage specified in arguments using direct input

			//admins
			this.commands["DeletePlayer"] = PLEVEL_ADMIN;//only admin can delete players
			this.commands["DeleteAccount"] = PLEVEL_ADMIN;//only admin can delete accounts
			this.commands["SetAccountPassword"] = PLEVEL_ADMIN;//only admin can set account passwords
			this.commands["BlockAccount"] = PLEVEL_ADMIN;//only admin can block account
			this.commands["UnBlockAccount"] = PLEVEL_ADMIN;//only admin can unblock account

			this.commands["ScriptedAccount"] = PLEVEL_ADMIN;//ScriptedAccount constructor - not supposed to be used at all
			this.commands["CreateGameAccount"] = PLEVEL_ADMIN;//only admin can create accounts
		}

		public int on_Command(Globals globals, ISrc commandSrc, string cmd) {
			int plevel = commandSrc.MaxPlevel;
			int cmdPlevel;
			if (this.commands.TryGetValue(cmd, out cmdPlevel))
			{
				if (plevel >= cmdPlevel) {
					return 0;
				}
				return 1;
			}
			if (plevel < plevelToAllCommands) {
				return 1;
			}
			return 0;
		}
	}
}