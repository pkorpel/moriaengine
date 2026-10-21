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
	/// Server-side setup helper for #561's protocol test (Tests/Protocol/NpcHpPoolTests.cs) -
	/// creates npc_test_hp_pool (scripts/Tests/NpcHpPoolTest.def) and reports the resulting
	/// MaxHits/Hits/MaxStam/Stam/MaxMana/Mana, proving NPCDef.MaxHitsRange/MaxStamRange/
	/// MaxManaRange actually apply on NPC.On_Create AND that current values get raised to
	/// match (full-heal-on-seed), not left at the old 100-point default.
	/// </summary>
	public static class NpcHpPoolTests {
		[SteamFunction]
		public static void TestCreateHpPoolNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_hp_pool");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("NPC_HP_POOL_TEST_RESULT: maxhits=" + npc.MaxHits + " hits=" + npc.Hits
				+ " maxstam=" + npc.MaxStam + " stam=" + npc.Stam
				+ " maxmana=" + npc.MaxMana + " mana=" + npc.Mana);
		}
	}
}
