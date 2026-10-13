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

		/// <summary>Phase 2 (#127): kills a freshly-spawned NPC directly via CauseDeath (bypassing
		/// real combat RNG/timing entirely - this is testing the experience-grant hook, not combat
		/// itself) and reports the killer's resulting level/experience. c_a_jackrabbit is used
		/// because it's a Calm animal (won't fight back) not used by any other test file sharing
		/// this long-lived server process (see CombatDepthTests.cs's TestGuardsRealmAggro comment
		/// for why that matters - mutating a shared NPCDef that no other scenario touches avoids
		/// leaking state between tests).</summary>
		[SteamFunction]
		public static void TestKillNpcGrantsExperience(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			int min = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int max = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);

			NPCDef rabbitDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			rabbitDef.ExperienceWorth = new int[] { min, max };
			NPC rabbit = (NPC) rabbitDef.Create(self.X + 1, self.Y, self.Z, self.M);
			rabbit.CauseDeath(player);
			Console.WriteLine("LEVELING_KILL_TEST_RESULT: level=" + player.Level + " experience=" + player.Experience);
		}
	}
}
