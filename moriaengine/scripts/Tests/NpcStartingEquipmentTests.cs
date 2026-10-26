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
	/// Server-side setup helper for #563's protocol test (Tests/Protocol/
	/// NpcStartingEquipmentTests.cs) - creates npc_test_starting_equipment
	/// (scripts/Tests/NpcStartingEquipmentTest.def) and reports where its starting
	/// equipment actually landed, proving NPCDef.StartingEquipment applies on
	/// NPC.On_Create and that ThingDef.Create(this) correctly equips the bow while
	/// dropping the arrows into the backpack.
	/// </summary>
	public static class NpcStartingEquipmentTests {
		[SteamFunction]
		public static void TestCreateStartingEquipmentNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_starting_equipment");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);

			AbstractItem equipped = npc.FindLayer(LayerNames.Hand1) ?? npc.FindLayer(LayerNames.Hand2);
			string equippedDefname = (equipped == null) ? "none" : ((ItemDef) equipped.Def).PrettyDefname;
			bool equippedIsNewbie = (equipped is Item equippedItem) && equippedItem.HasAttribute(ItemAttributes.attr_newbie);

			int arrowAmount = -1;
			bool arrowsAreNewbie = false;
			foreach (AbstractItem item in npc.Backpack) {
				if (((ItemDef) item.Def).PrettyDefname == "i_arrow_copper") {
					arrowAmount = item.Amount;
					arrowsAreNewbie = (item is Item arrowItem) && arrowItem.HasAttribute(ItemAttributes.attr_newbie);
					break;
				}
			}

			Console.WriteLine("NPC_STARTING_EQUIPMENT_TEST_RESULT: equipped=" + equippedDefname +
				" arrowAmount=" + arrowAmount + " equippedIsNewbie=" + equippedIsNewbie + " arrowsAreNewbie=" + arrowsAreNewbie);
		}
	}
}
