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

		/// <summary>#728 - proves random_pants/random_shoes/random_skirt now resolve to a real
		/// ItemDef (NPC.cs's ResolveStartingEquipmentItemDef) instead of silently dropping with
		/// a "not a known ItemDef" warning. All 3 pools happen to equip into distinct real
		/// layers (pants/shoes/skirt don't collide with each other), so all 3 should land
		/// equipped on the NPC, never in the backpack and never missing.</summary>
		[SteamFunction]
		public static void TestCreateRandomClothingEquipNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_random_clothing_equip");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);

			bool pantsEquipped = npc.FindLayer(LayerNames.Pants) != null;
			bool shoesEquipped = npc.FindLayer(LayerNames.Shoes) != null;
			bool skirtEquipped = npc.FindLayer(LayerNames.Skirt) != null;

			Console.WriteLine("RANDOM_CLOTHING_EQUIP_TEST_RESULT: pantsEquipped=" + pantsEquipped +
				" shoesEquipped=" + shoesEquipped + " skirtEquipped=" + skirtEquipped);
		}

		/// <summary>#728 - proves a StartingEquipment item evicted from its hand by a LATER
		/// entry in the same list (i_spellbook then i_axe_exec, both real Layer=1/Hand1, same
		/// order as the real-user-reported c_vguard_mordor_hala_jezdec) ends up in the NPC's
		/// backpack instead of lying on the ground.</summary>
		[SteamFunction]
		public static void TestCreateSpellbookWeaponCollisionNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_spellbook_weapon_collision");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);

			bool weaponEquipped = (npc.FindLayer(LayerNames.Hand1) is Item weaponItem) &&
				(((ItemDef) weaponItem.Def).PrettyDefname == "i_axe_exec");

			bool spellbookInBackpack = false;
			foreach (AbstractItem item in npc.Backpack) {
				if (((ItemDef) item.Def).PrettyDefname == "i_spellbook") {
					spellbookInBackpack = true;
					break;
				}
			}

			Console.WriteLine("SPELLBOOK_WEAPON_COLLISION_TEST_RESULT: weaponEquipped=" + weaponEquipped +
				" spellbookInBackpack=" + spellbookInBackpack);
		}
	}
}
