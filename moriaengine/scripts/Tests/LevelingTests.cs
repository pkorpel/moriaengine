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

	/// <summary>
	/// Server-side setup/inspection helpers for the leveling system's protocol tests
	/// (Tests/Protocol/LevelingTests.cs).
	/// </summary>
	public static class LevelingTests {
		[SteamFunction]
		public static void TestSetLevelAndExperience(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			player.Level = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			player.Experience = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			Console.WriteLine("LEVELING_TEST_SETUP: level=" + player.Level + " experience=" + player.Experience);
		}

		[SteamFunction]
		public static void TestAddExperience(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			int amount = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			LevelingSystem.AddExperience(player, amount);
			Console.WriteLine("LEVELING_TEST_RESULT: level=" + player.Level + " experience=" + player.Experience);
		}
	}
}
