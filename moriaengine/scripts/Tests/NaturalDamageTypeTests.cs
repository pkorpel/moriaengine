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
	/// Server-side setup helper for #607's protocol test
	/// (Tests/Protocol/NaturalDamageTypeTests.cs) - creates a bare-handed test NPC (no
	/// weapon equipped) and reports its live Character.WeaponDamageType, proving
	/// NPCDef.NaturalDamageType overrides the usual WeaponType-derived default
	/// (CombatCalculator.GetWeaponDamageType) for a monster's own natural attack.
	/// </summary>
	public static class NaturalDamageTypeTests {
		[SteamFunction]
		public static void TestCreateNaturalDamageNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_natural_damage_type");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("NATURAL_DAMAGE_TYPE_TEST_RESULT: configured=" + npc.WeaponDamageType);

			NPCDef tripleDef = (NPCDef) ThingDef.GetByDefname("npc_test_natural_damage_type_triple");
			NPC tripleNpc = (NPC) tripleDef.Create(self.X + 3, self.Y, self.Z, self.M);
			Console.WriteLine("NATURAL_DAMAGE_TYPE_TEST_RESULT: triple=" + tripleNpc.WeaponDamageType);

			NPCDef unconfiguredDef = (NPCDef) ThingDef.GetByDefname("npc_test_natural_damage_type_unconfigured");
			NPC unconfiguredNpc = (NPC) unconfiguredDef.Create(self.X + 2, self.Y, self.Z, self.M);
			Console.WriteLine("NATURAL_DAMAGE_TYPE_TEST_RESULT: unconfigured=" + unconfiguredNpc.WeaponDamageType);
		}
	}
}
