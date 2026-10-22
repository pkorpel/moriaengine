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
	/// Server-side setup helper for #586's protocol test (Tests/Protocol/NpcResourcesTests.cs) -
	/// loads npc_test_resources (scripts/Tests/NpcResourcesTest.def) and reports its parsed
	/// ButcherResources list, proving the data template round-trips correctly. No butchering
	/// interaction exists to consume this yet - this only confirms the data itself.
	/// </summary>
	public static class NpcResourcesTests {
		[SteamFunction]
		public static void TestReportNpcResources(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_resources");
			System.Collections.Generic.List<NPCDef.ResourceEntry> entries = def.ButcherResources;
			int count = (entries == null) ? 0 : entries.Count;
			string entry0 = (count > 0) ? (entries[0].Amount + " " + entries[0].Defname) : "none";
			string entry1 = (count > 1) ? (entries[1].Amount + " " + entries[1].Defname) : "none";
			Console.WriteLine("NPC_RESOURCES_TEST_RESULT: count=" + count + " entry0=" + entry0 + " entry1=" + entry1);
		}
	}
}
