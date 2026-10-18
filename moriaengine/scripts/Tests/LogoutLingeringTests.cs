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
	}
}
