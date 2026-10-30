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
	/// Server-side setup helper for #606's protocol test (Tests/Protocol/NpcResistTests.cs) -
	/// creates npc_test_resist (scripts/Tests/NpcResistTest.def) and reports the resulting
	/// Character.ResistX values, proving the real engine ResistX mechanism (already live,
	/// consumed in DamageManager.GetResistModifier - just never had any .def content behind
	/// it before this issue's converter mapping) actually applies the exact field names the
	/// converter now emits from the real archive's tag.resist_X tags.
	/// </summary>
	public static class NpcResistTests {
		[SteamFunction]
		public static void TestCreateResistNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_resist");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("NPC_RESIST_TEST_RESULT: poison=" + npc.ResistPoison +
				" fire=" + npc.ResistFire + " electric=" + npc.ResistElectric +
				" slashing=" + npc.ResistSlashing + " stabbing=" + npc.ResistStabbing +
				" paralyse=" + npc.ResistParalyse + " curse=" + npc.ResistCurse + " sharp=" + npc.ResistSharp);
			Console.WriteLine("NPC_RESIST_MODIFIER:" +
				" slashing=" + DamageManager.GetResistModifier(npc, DamageType.PhysicalSlashing).ToString(CultureInfo.InvariantCulture) +
				" sharp=" + DamageManager.GetResistModifier(npc, DamageType.PhysicalSharp).ToString(CultureInfo.InvariantCulture) +
				" two=" + DamageManager.GetResistModifier(npc, DamageType.Physical | DamageType.Slashing | DamageType.Blunt).ToString(CultureInfo.InvariantCulture) +
				" fire=" + DamageManager.GetResistModifier(npc, DamageType.MagicFire).ToString(CultureInfo.InvariantCulture) +
				" both=" + DamageManager.GetResistModifier(npc, DamageType.PhysicalBlunt | DamageType.Fire).ToString(CultureInfo.InvariantCulture) +
				" plain=" + DamageManager.GetResistModifier(npc, DamageType.Physical).ToString(CultureInfo.InvariantCulture));
		}
	}
}
