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

			//New command (#102/A1): "use whatever I'm holding" shortcut, double-clicks the
			//equipped hand1 item (falling back to hand2) via the real Trigger_DClick path.
			this.commands["UseHand"] = PLEVEL_PLAYER;

			//Same default-deny gap as the Guild commands below - a real player-facing command
			//(level/experience/combat-stats dialog, scripts/Basic/dialogs/explevel.scp) missing
			//from this allowlist meant it silently required GM (plevel>=4) by the default-deny
			//fallback. Only ever worked by accident for whichever account happened to be the
			//very first one created on a fresh server (AbstractAccount.HandleLoginAttempt grants
			//that one account MaximalPlevel) - every other player was silently denied (#145).
			this.commands["explevel"] = PLEVEL_PLAYER;
			//explevel.scp's own body calls these two bare (no explicit receiver) as its first two
			//lines, to refresh self's cached combat stat values before displaying them - a bare
			//LScript call like this ALSO goes through this same authorization gate (via
			//OpNode_Lazy_Expression, same mechanism as the explevel entry above), checked against
			//ITS OWN name, separately from the "explevel" command name itself. Missing from this
			//list meant the explevel fix above alone wasn't enough - every non-GM player's first
			//.explevel ever actually attempted would still be denied one line in. Purely a
			//self-targeted cache refresh, safe for any player to trigger on themselves.
			this.commands["InvalidateCombatWeaponValues"] = PLEVEL_PLAYER;
			this.commands["InvalidateCombatArmorValues"] = PLEVEL_PLAYER;

			//Guild commands - gated by their own internal leader/member checks, not plevel;
			//missing from this allowlist meant they silently required GM (plevel>=4) by the
			//default-deny fallback below, so no ordinary player could ever use them
			this.commands["GuildCreate"] = PLEVEL_PLAYER;
			this.commands["GuildInvite"] = PLEVEL_PLAYER;
			this.commands["GuildAccept"] = PLEVEL_PLAYER;
			this.commands["GuildDecline"] = PLEVEL_PLAYER;
			this.commands["GuildLeave"] = PLEVEL_PLAYER;
			this.commands["GuildDisband"] = PLEVEL_PLAYER;
			this.commands["GuildSay"] = PLEVEL_PLAYER;
			this.commands["GuildInfo"] = PLEVEL_PLAYER;
			this.commands["GuildAbbreviation"] = PLEVEL_PLAYER;
			this.commands["GuildDeclareWar"] = PLEVEL_PLAYER;
			this.commands["GuildMakePeace"] = PLEVEL_PLAYER;
			this.commands["GuildDeclareAlliance"] = PLEVEL_PLAYER;
			this.commands["GuildBreakAlliance"] = PLEVEL_PLAYER;

			//Same default-deny gap as the Guild commands above - gated by its own
			//owner-standing-in-the-house check, not plevel.
			this.commands["ReleaseHouse"] = PLEVEL_PLAYER;

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
			// "Test*" commands are verification-harness-only diagnostics (scripts/Tests/*.cs);
			// no production command uses this prefix, and they must stay callable by the
			// non-GM accounts the harness deliberately creates to exercise real player-level
			// authorization paths - see #72.
			if (cmd.StartsWith("Test", StringComparison.OrdinalIgnoreCase)) {
				return 0;
			}
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