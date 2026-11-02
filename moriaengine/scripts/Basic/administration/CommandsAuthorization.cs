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

		/// <summary>Set by the protocol tests' fixture, and by nothing on a real server. Public so a test can close it.</summary>
		public static bool testCommandsOpen = Environment.GetEnvironmentVariable(SteamEngine.Common.TestHarness.CommandsVariable) == "1";

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

			//#839: Erebor's quest journal, for everybody
			this.commands["Questy"] = PLEVEL_PLAYER;
			this.commands["MenuStyle"] = PLEVEL_PLAYER; //#1004: to compare the menus
			this.commands["WhoAmI"] = PLEVEL_PLAYER; //#1011: Erebor's
			this.commands["FrostBolt"] = PLEVEL_PLAYER;
			this.commands["NecroBolt"] = PLEVEL_PLAYER;
			this.commands["NecroLight"] = PLEVEL_PLAYER;
			this.commands["NecroAnimate"] = PLEVEL_PLAYER;
			this.commands["NecroSummon"] = PLEVEL_PLAYER;
			this.commands["NecroArmor"] = PLEVEL_PLAYER;
			this.commands["SetEquip0"] = PLEVEL_PLAYER;
			this.commands["SetEquip1"] = PLEVEL_PLAYER;
			this.commands["SetEquip2"] = PLEVEL_PLAYER;
			this.commands["SetEquip3"] = PLEVEL_PLAYER;
			this.commands["SetEquip4"] = PLEVEL_PLAYER;
			this.commands["SetEquip5"] = PLEVEL_PLAYER;
			this.commands["SetEquip6"] = PLEVEL_PLAYER;
			this.commands["SetEquip7"] = PLEVEL_PLAYER;
			this.commands["SetEquip8"] = PLEVEL_PLAYER;
			this.commands["SetEquip9"] = PLEVEL_PLAYER;
			this.commands["SetEquip10"] = PLEVEL_PLAYER;
			this.commands["SetEquip11"] = PLEVEL_PLAYER;
			this.commands["SetEquip12"] = PLEVEL_PLAYER;
			this.commands["SetEquip13"] = PLEVEL_PLAYER;
			this.commands["SetEquip14"] = PLEVEL_PLAYER;
			this.commands["SetEquip15"] = PLEVEL_PLAYER;
			this.commands["Equip0"] = PLEVEL_PLAYER;
			this.commands["Equip1"] = PLEVEL_PLAYER;
			this.commands["Equip2"] = PLEVEL_PLAYER;
			this.commands["Equip3"] = PLEVEL_PLAYER;
			this.commands["Equip4"] = PLEVEL_PLAYER;
			this.commands["Equip5"] = PLEVEL_PLAYER;
			this.commands["Equip6"] = PLEVEL_PLAYER;
			this.commands["Equip7"] = PLEVEL_PLAYER;
			this.commands["Equip8"] = PLEVEL_PLAYER;
			this.commands["Equip9"] = PLEVEL_PLAYER;
			this.commands["Equip10"] = PLEVEL_PLAYER;
			this.commands["Equip11"] = PLEVEL_PLAYER;
			this.commands["Equip12"] = PLEVEL_PLAYER;
			this.commands["Equip13"] = PLEVEL_PLAYER;
			this.commands["Equip14"] = PLEVEL_PLAYER;
			this.commands["Equip15"] = PLEVEL_PLAYER;
			this.commands["Heal0"] = PLEVEL_PLAYER;
			this.commands["Heal1"] = PLEVEL_PLAYER;
			this.commands["Heal2"] = PLEVEL_PLAYER;
			this.commands["Heal3"] = PLEVEL_PLAYER;
			this.commands["Heal4"] = PLEVEL_PLAYER;
			this.commands["Heal5"] = PLEVEL_PLAYER;
			this.commands["Heal6"] = PLEVEL_PLAYER;
			this.commands["Heal7"] = PLEVEL_PLAYER;
			this.commands["Heal8"] = PLEVEL_PLAYER;
			this.commands["Heal9"] = PLEVEL_PLAYER;
			this.commands["Heal10"] = PLEVEL_PLAYER;
			this.commands["Heal11"] = PLEVEL_PLAYER;
			this.commands["Heal12"] = PLEVEL_PLAYER;
			this.commands["Heal13"] = PLEVEL_PLAYER;
			this.commands["Heal14"] = PLEVEL_PLAYER;
			this.commands["Heal15"] = PLEVEL_PLAYER;
			this.commands["SamHeal0"] = PLEVEL_PLAYER;
			this.commands["SamHeal1"] = PLEVEL_PLAYER;
			this.commands["SamHeal2"] = PLEVEL_PLAYER;
			this.commands["SamHeal3"] = PLEVEL_PLAYER;
			this.commands["SamHeal4"] = PLEVEL_PLAYER;
			this.commands["SamHeal5"] = PLEVEL_PLAYER;
			this.commands["SamHeal6"] = PLEVEL_PLAYER;
			this.commands["SamHeal7"] = PLEVEL_PLAYER;
			this.commands["SamHeal8"] = PLEVEL_PLAYER;
			this.commands["SamHeal9"] = PLEVEL_PLAYER;
			this.commands["SamHeal10"] = PLEVEL_PLAYER;
			this.commands["SamHeal11"] = PLEVEL_PLAYER;
			this.commands["SamHeal12"] = PLEVEL_PLAYER;
			this.commands["SamHeal13"] = PLEVEL_PLAYER;
			this.commands["SamHeal14"] = PLEVEL_PLAYER;
			this.commands["SamHeal15"] = PLEVEL_PLAYER;
			this.commands["Provo"] = PLEVEL_PLAYER;
			this.commands["AstralPower"] = PLEVEL_PLAYER;
			this.commands["Enlightment"] = PLEVEL_PLAYER;
			this.commands["Improvement"] = PLEVEL_PLAYER;
			this.commands["Check"] = PLEVEL_PLAYER;
			this.commands["Mesec"] = PLEVEL_PLAYER;
			this.commands["Repair"] = PLEVEL_PLAYER;
			this.commands["Vigilance"] = PLEVEL_PLAYER;
			this.commands["Stance"] = PLEVEL_PLAYER;
			this.commands["SwitchMagic"] = PLEVEL_PLAYER;

			//New command (#593): feed a tamed pet/mount from the player's own backpack.
			//Explicitly allowlisted rather than relying on the default-deny fallback - this
			//is a genuine player-facing pet-care command, not a GM tool.
			this.commands["Feed"] = PLEVEL_PLAYER;

			//Same default-deny gap as the Guild commands below - a real player-facing command
			//(level/experience/combat-stats dialog, scripts/Basic/dialogs/explevel.scp) missing
			//from this allowlist meant it silently required GM (plevel>=4) by the default-deny
			//fallback. Only ever worked by accident for whichever account happened to be the
			//very first one created on a fresh server (AbstractAccount.HandleLoginAttempt grants
			//that one account MaximalPlevel) - every other player was silently denied (#146).
			this.commands["explevel"] = PLEVEL_PLAYER;
			//what its dialogs use inside doesn't need entries here: only the word typed is checked (#406)

			//Guild commands - gated by their own internal leader/member checks, not plevel;
			//missing from this allowlist meant they silently required GM (plevel>=4) by the
			//default-deny fallback below, so no ordinary player could ever use them
			this.commands["Party"] = PLEVEL_PLAYER;
			this.commands["Boosts"] = PLEVEL_PLAYER;//#923 what affects me
			this.commands["Zprava"] = PLEVEL_PLAYER;//#931
			this.commands["Chat"] = PLEVEL_PLAYER;//#931
			this.commands["GuildScream"] = PLEVEL_PLAYER;//#931
			this.commands["PotionCure"] = PLEVEL_PLAYER;//#927
			this.commands["PotionInvis"] = PLEVEL_PLAYER;//#927
			this.commands["PotionReactive"] = PLEVEL_PLAYER;//#927
			this.commands["PotionReflection"] = PLEVEL_PLAYER;//#927
			this.commands["PotionReflex"] = PLEVEL_PLAYER;//#927
			this.commands["PotionNightsight"] = PLEVEL_PLAYER;//#927
			this.commands["PotionShrink"] = PLEVEL_PLAYER;//#927
			this.commands["DragonResist"] = PLEVEL_PLAYER;//#927
			this.commands["PotionHeal"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirHeal"] = PLEVEL_PLAYER;//#927
			this.commands["PotionRefresh"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirRefresh"] = PLEVEL_PLAYER;//#927
			this.commands["PotionEnergy"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirEnergy"] = PLEVEL_PLAYER;//#927
			this.commands["PotionStrength"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirStrength"] = PLEVEL_PLAYER;//#927
			this.commands["PotionClever"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirClever"] = PLEVEL_PLAYER;//#927
			this.commands["PotionAgility"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirAgility"] = PLEVEL_PLAYER;//#927
			this.commands["PotionMobility"] = PLEVEL_PLAYER;//#927
			this.commands["PotionSpellShield"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirSpellShield"] = PLEVEL_PLAYER;//#927
			this.commands["PotionStoneskin"] = PLEVEL_PLAYER;//#927
			this.commands["ElixirStoneskin"] = PLEVEL_PLAYER;//#927
			this.commands["ThrowExplosion"] = PLEVEL_PLAYER;//#927
			this.commands["ThrowDispel"] = PLEVEL_PLAYER;//#927
			this.commands["NavrhBan"] = PLEVEL_PLAYER;
			this.commands["Honor"] = PLEVEL_PLAYER;
			this.commands["TownControl"] = PLEVEL_PLAYER;
			this.commands["TownGuardsKill"] = PLEVEL_PLAYER;
			this.commands["TownGuardsStop"] = PLEVEL_PLAYER;
			this.commands["TownGuardsContinue"] = PLEVEL_PLAYER;
			this.commands["TownGuardsFlee"] = PLEVEL_PLAYER;
			this.commands["TownGuardsCome"] = PLEVEL_PLAYER;
			this.commands["MordorTownControl"] = PLEVEL_PLAYER;
			this.commands["MordorTownGuardsKill"] = PLEVEL_PLAYER;
			this.commands["MordorTownGuardsStop"] = PLEVEL_PLAYER;
			this.commands["MordorTownGuardsContinue"] = PLEVEL_PLAYER;
			this.commands["MordorTownGuardsFlee"] = PLEVEL_PLAYER;
			this.commands["MordorTownGuardsCome"] = PLEVEL_PLAYER;
			this.commands["GuardsControl"] = PLEVEL_PLAYER;
			this.commands["KingGuardsKill"] = PLEVEL_PLAYER;
			this.commands["KingGuardsStop"] = PLEVEL_PLAYER;
			this.commands["KingGuardsContinue"] = PLEVEL_PLAYER;
			this.commands["KingGuardsFlee"] = PLEVEL_PLAYER;
			this.commands["KingGuardsCome"] = PLEVEL_PLAYER;
			this.commands["MordorGuardsControl"] = PLEVEL_PLAYER;
			this.commands["MarghulsGuardsKill"] = PLEVEL_PLAYER;
			this.commands["MarghulsGuardsStop"] = PLEVEL_PLAYER;
			this.commands["MarghulsGuardsContinue"] = PLEVEL_PLAYER;
			this.commands["MarghulsGuardsFlee"] = PLEVEL_PLAYER;
			this.commands["MarghulsGuardsCome"] = PLEVEL_PLAYER;
			this.commands["Fear"] = PLEVEL_PLAYER;
			this.commands["Blacklist"] = PLEVEL_PLAYER;
			this.commands["GuildLeave"] = PLEVEL_PLAYER;
			this.commands["GuildMembers"] = PLEVEL_PLAYER;
			this.commands["ToggleAbbrev"] = PLEVEL_PLAYER;
			this.commands["GuildSay"] = PLEVEL_PLAYER;
			this.commands["GuildInfo"] = PLEVEL_PLAYER;
			this.commands["GuildAbbreviation"] = PLEVEL_PLAYER;
			this.commands["GuildDeclareWar"] = PLEVEL_PLAYER;
			this.commands["GuildMakePeace"] = PLEVEL_PLAYER;
			this.commands["GuildDeclareAlliance"] = PLEVEL_PLAYER;
			this.commands["GuildBreakAlliance"] = PLEVEL_PLAYER;

			//Same default-deny gap as the Guild commands above - gated by its own
			//owner-standing-in-the-house check, not plevel.

			//GMs
			//Print the value of given expression
			this.commands["show"] = MORE_THAN_PLAYER;
			//[Show the targetting cross to perform a given expression on target
			this.commands["x"] = MORE_THAN_PLAYER;
			this.commands["Page"] = MORE_THAN_PLAYER;//Post a new GMPage specified in arguments using direct input

			//admins
			//the world's setups, for experienced GMs (#951)
			this.commands["SetupWorld"] = PLEVEL_ADMIN;
			this.commands["SetupHouses"] = PLEVEL_ADMIN;
			this.commands["SetupTowns"] = PLEVEL_ADMIN;
			this.commands["SetupDungeonEntrances"] = PLEVEL_ADMIN;
			this.commands["SetupExpertQuest"] = PLEVEL_ADMIN;
			this.commands["SetupMasterQuest"] = PLEVEL_ADMIN;
			this.commands["SetupGrandmasterQuest"] = PLEVEL_ADMIN;

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
			// they must stay callable by the non-GM accounts the harness deliberately creates to
			// exercise real player-level authorization paths - see #72. They compile into every
			// server and can make anyone a GM, so only the harness gets this (#951).
			if (testCommandsOpen && cmd.StartsWith("Test", StringComparison.OrdinalIgnoreCase)) {
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