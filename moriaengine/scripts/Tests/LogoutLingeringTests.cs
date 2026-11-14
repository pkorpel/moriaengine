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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>Regression coverage for #416 - GMs (plevel >= Globals.PlevelOfGM) skip the
	/// classic logout-lingering delay (#261, Player.On_LogOut/CharLingeringTimer) and
	/// disappear immediately on disconnect, instead of staying IsLingering for
	/// LogoutSettings.instance.logoutDelaySeconds. Reports via a separate, still-connected
	/// observer character (Thing.UidGetCharacter, same pattern as LevelingTests.cs's
	/// TestKillPlayerAndReportResult) since a character can't observe its own post-disconnect
	/// state from within its own already-closed connection.</summary>
	public static class LogoutLingeringTests {
		[SteamFunction]
		public static void TestReportLingerState(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			Console.WriteLine("LINGER_STATE: uid=" + targetUid + " isLingering=" + target.IsLingering +
				" flagDisconnected=" + target.Flag_Disconnected);
		}

		/// <summary>#1487 - a line to a character (its uid): one its account left has no client of its own, so nobody gets it.</summary>
		[SteamFunction]
		public static void TestTellCharacter(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			target.SysMessage("Zprava pro odesleho");
			Console.WriteLine("TELL_TEST: uid=" + targetUid + " gameStateNull=" + (target.GameState == null));
		}

		private static double? logoutDelayBefore;

		/// <summary>#1487 - the ordinary logout delay, in seconds; "restore" puts back the one before the first change.</summary>
		[SteamFunction]
		public static void TestSetLogoutDelay(Character self, ScriptArgs sa) {
			string arg = string.Concat(sa.Argv[0]);
			if (arg == "restore") {
				if (logoutDelayBefore is double before) {
					LogoutSettings.instance.logoutDelaySeconds = before;
					logoutDelayBefore = null;
				}
			} else {
				logoutDelayBefore = logoutDelayBefore ?? LogoutSettings.instance.logoutDelaySeconds;
				LogoutSettings.instance.logoutDelaySeconds = Convert.ToDouble(arg, CultureInfo.InvariantCulture);
			}
			Console.WriteLine("LOGOUT_DELAY: " + LogoutSettings.instance.logoutDelaySeconds);
		}
	}
}
