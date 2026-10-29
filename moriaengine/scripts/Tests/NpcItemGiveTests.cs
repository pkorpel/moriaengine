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

	/// <summary>#808 - server-side helpers for Tests/Protocol/NpcItemGiveTests.cs.</summary>
	public static class NpcItemGiveTests {
		/// <summary>Spawns `defname` next to me, optionally as my pet (and hungry).</summary>
		[SteamFunction]
		public static void TestGiveSpawn(Character self, ScriptArgs sa) {
			string defname = (string) sa.Argv[0];
			bool tame = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture) != 0;
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname(defname)).Create(self.X + 1, self.Y, self.Z, self.M);
			if (tame) {
				npc.ControlMaster = self;
				PetHungerTimer hunger = PetHungerTimer.GetFor(npc);
				if (hunger != null) {
					hunger.food = 1;
				}
			}
			Console.WriteLine("GIVE_SPAWN: " + defname + " npc=" + npc.Uid + " pack=" + npc.Backpack.Uid);
		}

		/// <summary>A sword and 3 raw ribs in my pack.</summary>
		[SteamFunction]
		public static void TestGiveItems(Character self) {
			Container pack = self.Backpack;
			AbstractItem sword = (AbstractItem) ((ItemDef) ThingDef.GetByDefname("i_sword_broad_iron")).Create(pack);
			AbstractItem ribs = (AbstractItem) ((ItemDef) ThingDef.GetByDefname("i_ribs_raw")).Create(pack);
			ribs.Amount = 3;
			Console.WriteLine("GIVE_ITEMS: pack=" + pack.Uid + " sword=" + sword.Uid + " ribs=" + ribs.Uid);
		}

		/// <summary>Where an item is (or that it's gone), plus the npc's hunger.</summary>
		[SteamFunction]
		public static void TestGiveReport(Character self, ScriptArgs sa) {
			uint itemUid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			uint npcUid = Convert.ToUInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			AbstractItem item = Thing.UidGetItem(itemUid);
			NPC npc = Thing.UidGetThing(npcUid) as NPC;
			PetHungerTimer hunger = (npc != null) ? PetHungerTimer.GetFor(npc) : null;
			string where = "gone";
			if ((item != null) && !item.IsDeleted) {
				Thing top = item.TopObj();
				where = "holder=" + ((top == null) ? "none" : top.Name) + " inMyPack=" + (item.Cont == self.Backpack) +
					" inNpcPack=" + ((npc != null) && (item.Cont == npc.Backpack)) + " amount=" + item.Amount +
					" dragging=" + self.HasPickedUp(item);
			}
			Console.WriteLine("GIVE_REPORT: item=" + itemUid + " " + where + " food=" + ((hunger != null) ? hunger.food : -1) +
				" mountedOn=" + ((self.Mount == null) ? "none" : self.Mount.Name));
		}

		[SteamFunction]
		public static void TestGiveDismount(Character self) {
			self.Dismount();
		}
	}
}
