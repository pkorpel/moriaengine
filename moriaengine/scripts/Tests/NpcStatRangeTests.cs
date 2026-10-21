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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side setup helper for #559's protocol test (Tests/Protocol/NpcStatRangeTests.cs) -
	/// creates npc_test_stat_range (scripts/Tests/NpcStatRangeTest.def) and reports the resulting
	/// Str/Dex/Int, proving NPCDef.StrRange/DexRange/IntRange actually apply on NPC.On_Create.
	/// </summary>
	public static class NpcStatRangeTests {
		[SteamFunction]
		public static void TestCreateStatRangeNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_stat_range");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("NPC_STAT_RANGE_TEST_RESULT: str=" + npc.Str + " dex=" + npc.Dex + " int=" + npc.Int);
		}
	}
}
