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
	/// Server-side setup helper for the BloodColor field's protocol test
	/// (Tests/Protocol/NpcBloodColorTests.cs) - reports NPCDef.BloodColor for both the
	/// hex-leading-zero form and the symbolic-color-constant form, proving the generic
	/// Defs:-field/LScript-expression pipeline resolves both real archive shapes
	/// correctly (a hand-written int.Parse would have thrown on the symbolic one).
	/// </summary>
	public static class NpcBloodColorTests {
		[SteamFunction]
		public static void TestReportBloodColors(Character self, ScriptArgs sa) {
			NPCDef hexDef = (NPCDef) ThingDef.GetByDefname("npc_test_blood_color_hex");
			NPCDef symbolicDef = (NPCDef) ThingDef.GetByDefname("npc_test_blood_color_symbolic");
			Console.WriteLine("NPC_BLOOD_COLOR_TEST_RESULT: hex=" + hexDef.BloodColor + " symbolic=" + symbolicDef.BloodColor);
		}
	}
}
