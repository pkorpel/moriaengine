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
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #791 - server side of Tests/Protocol/MonsterLootTests.cs: real converted monsters with
	/// "Loot =" lines (Erebor's @NPCRestock) and "Equip = i_monster_create" (Erebor's creaturestart).
	/// </summary>
	public static class MonsterLootTests {
		[SteamFunction]
		public static void TestOrcLoot(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC orc = Spawn("c_orc", self);
			int exp = orc.ExperienceReward;
			int expected = MonsterLoot.GoldFromExperience(exp, MonsterLoot.IsDungeonMapPlane(orc.M),
				LootSettings.instance.goldPermilleSurface, LootSettings.instance.goldPermilleDungeon);
			int gold = CountInPack(orc, "i_gold");
			bool boots = CountInPack(orc, "i_boots_thigh") > 0;
			bool monsterCreateItem = false;
			foreach (AbstractItem item in orc) {
				if (MonsterLoot.IsMonsterCreateMarker(item.Def.PrettyDefname)) {
					monsterCreateItem = true;
				}
			}

			int x = orc.X, y = orc.Y;
			orc.CauseDeath(null);
			int corpseGold = 0;
			bool corpseBoots = false;
			foreach (Thing t in self.GetMap().GetThingsInRange(x, y, 0)) {
				Corpse corpse = t as Corpse;
				if (corpse == null) {
					continue;
				}
				foreach (Item i in corpse.EnumShallow()) {
					string defname = i.Def.PrettyDefname;
					if (defname == "i_gold") {
						corpseGold += i.Amount;
					} else if (defname == "i_boots_thigh") {
						corpseBoots = true;
					}
				}
			}
			Console.WriteLine("MONSTER_LOOT: exp=" + exp + " gold=" + gold + " expectedGold=" + expected +
				" boots=" + boots + " monsterCreateItem=" + monsterCreateItem +
				" corpseGold=" + corpseGold + " corpseBoots=" + corpseBoots);
		}

		/// <summary>loot_cyclops nests tm_rocks/tm_boulders templates (no CONTAINER of their own),
		/// which used to make Container.NewItem delete the backpack they'd just filled.</summary>
		[SteamFunction]
		public static void TestCyclopsLoot(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC cyclops = Spawn("c_cyclops", self);
			AbstractItem pack = cyclops.FindLayer(LayerNames.Pack);
			int items = 0;
			if (pack != null) {
				foreach (AbstractItem i in pack) {
					items++;
				}
			}
			Console.WriteLine("CYCLOPS_LOOT: backpackAlive=" + ((pack != null) && !pack.IsDeleted) + " items=" + items);
			cyclops.Delete();
		}

		/// <summary>loot_OGRE has "ITEM=RANDOM_JEWEL,{0 1}" - a rolled 0 must mean no item, not an
		/// empty stack.</summary>
		[SteamFunction]
		public static void TestNoZeroAmountLoot(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			int zero = 0, total = 0;
			List<NPC> ogres = new List<NPC>();
			for (int n = 0; n < 20; n++) {
				NPC ogre = Spawn("c_ogre", self);
				ogres.Add(ogre);
				foreach (AbstractItem i in ogre.Backpack) {
					total++;
					if (i.Amount <= 0) {
						zero++;
					}
				}
			}
			foreach (NPC ogre in ogres) {
				ogre.Delete();
			}
			Console.WriteLine("ZERO_LOOT: items=" + total + " zeroAmount=" + zero);
		}

		private static NPC Spawn(string defname, Character near) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname(defname);
			return (NPC) def.Create(near.X + 1, near.Y, near.Z, near.M);
		}

		private static int CountInPack(NPC npc, string defname) {
			int amount = 0;
			foreach (AbstractItem i in npc.Backpack) {
				if (i.Def.PrettyDefname == defname) {
					amount += i.Amount;
				}
			}
			return amount;
		}
	}
}
