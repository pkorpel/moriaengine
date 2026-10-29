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

	/// <summary>#807 - server-side helpers for Tests/Protocol/ItemRefuserTests.cs.</summary>
	public static class ItemRefuserTests {
		[SteamFunction]
		public static void TestSetupItemRefuser(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC refuser = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_item_refuser")).Create(self.X + 1, self.Y, self.Z, self.M);
			NPC keeper = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_item_keeper")).Create(self.X - 1, self.Y, self.Z, self.M);
			ItemDef swordDef = (ItemDef) ThingDef.GetByDefname("i_sword_broad_iron");
			Container backpack = self.Backpack;
			AbstractItem itemA = (AbstractItem) swordDef.Create(backpack);
			AbstractItem itemB = (AbstractItem) swordDef.Create(backpack);
			AbstractItem itemC = (AbstractItem) swordDef.Create(backpack);
			Console.WriteLine("REFUSER_SETUP: refuser=" + refuser.Uid + " keeper=" + keeper.Uid +
				" itemA=" + itemA.Uid + " itemB=" + itemB.Uid + " itemC=" + itemC.Uid + " backpack=" + backpack.Uid);
		}

		/// <summary>Where the item ended up: in my own backpack, or on whom.</summary>
		[SteamFunction]
		public static void TestReportItemHolder(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			AbstractItem item = Thing.UidGetItem(uid);
			if (item == null) {
				Console.WriteLine("REFUSER_RESULT: uid=" + uid + " exists=False");
				return;
			}
			Thing top = item.TopObj();
			Console.WriteLine("REFUSER_RESULT: uid=" + uid + " exists=True inMyPack=" + (item.Cont == self.Backpack) +
				" holder=" + ((top == null) ? "none" : top.Name) + " dragging=" + self.HasPickedUp(item));
		}
	}
}
