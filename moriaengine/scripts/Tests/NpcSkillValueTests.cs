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
	/// Server-side setup helper for #562's protocol test (Tests/Protocol/NpcSkillValueTests.cs) -
	/// creates npc_test_skill_values (scripts/Tests/NpcSkillValueTest.def) and reports the
	/// resulting Wrestling/MagicResist skill values, proving NPCDef.SkillValueRanges actually
	/// apply on NPC.On_Create.
	/// </summary>
	public static class NpcSkillValueTests {
		[SteamFunction]
		public static void TestCreateSkillValueNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_skill_values");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			int wrestling = npc.GetRealSkillValue(SkillName.Wrestling);
			int magicResist = npc.GetRealSkillValue(SkillName.MagicResist);
			Console.WriteLine("NPC_SKILL_VALUE_TEST_RESULT: wrestling=" + wrestling + " magicresist=" + magicResist);
		}
	}
}
