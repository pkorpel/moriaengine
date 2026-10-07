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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public sealed class e_test_all_generic : CompiledTriggerGroup {
		public override object Run(object self, TriggerKey tk, ScriptArgs sa) {
			if (sa != null) {
				Console.WriteLine("@" + tk.Name + " on " + self + " - parameters:\t" + 
				Tools.ObjToString(sa.Argv));
			} else {
				Console.WriteLine("@" + tk.Name + " on " + self);
			}

			return null;
		}
	}


	//public class e_test_all : CompiledTriggerGroup {
	//    public int On_WarModeChange(Character self, bool changeTo) {
	//        self.SysMessage("@warModeChange: Changing warmode to " + changeTo);
	//        return 0;
	//    }

	//    public int On_CharDClick(Character self, Character cre) {
	//        self.SysMessage("@charDClick: DClicked character " + cre);
	//        return 0;
	//    }
	//    public int On_ItemDClick(Character self, Item item) {
	//        self.SysMessage("@itemDClick: DClicked item " + item);
	//        return 0;
	//    }
	//    public int On_DClick(Character self) {
	//        self.SysMessage("@DClick: " + Globals.Src + " DClicked me");
	//        return 0;
	//    }

	//    public int On_CharClick(Character self, Character cre) {
	//        self.SysMessage("@charClick: Clicked character " + cre);
	//        return 0;
	//    }
	//    public int On_ItemClick(Character self, Item item) {
	//        self.SysMessage("@itemClick: Clicked item " + item);
	//        return 0;
	//    }
	//    public int On_Click(Character self) {
	//        self.SysMessage("@Click: " + Globals.Src + " Clicked me");
	//        return 0;
	//    }

	//    public int On_ItemDropon_Item(Character self, Item target, Item dropped) {
	//        self.SysMessage("@ItemDropon_Item: I am dropping item " + dropped + " on item " + target);
	//        return 0;
	//    }
	//    public int On_ItemDropon_Char(Character self, Character target, Item dropped) {
	//        self.SysMessage("@ItemDropon_Item: I am dropping item " + dropped + " on character " + target);
	//        return 0;
	//    }
	//    public int On_ItemStackOn_Item(Character self, Item dropped, Item target, int x, int y) {
	//        self.SysMessage("@ItemStackOn_Item: Item " + target + " is being 'stacked' with item " + dropped + " on point4d " + x + ", " + y);
	//        if (dropped.Amount == 100) {
	//            self.SysMessage("test: Dropping of amount 100 is cancelled");
	//            return 1;
	//        }
	//        return 0;
	//    }
	//    public int On_ItemStackOn_Char(Character self, Item dropped, Character target, int x, int y) {
	//        self.SysMessage("@ItemStackOn_Char: Character " + target + " is being 'stacked' with item " + dropped + " on point4d " + x + ", " + y);
	//        return 0;
	//    }
	//    public int On_ItemAfterStackOn(Character self, Item dropped) {
	//        self.SysMessage("@ItemAfterStackOn: Item " + dropped + " was 'stacked'.");
	//        return 0;
	//    }
	//    public int On_ItemDropon_Ground(Character self, Item dropped, ushort x, ushort y, sbyte z) {
	//        self.SysMessage("@ItemDropon_Ground: I am dropping item " + dropped + " at " + x + ", " + y + ", " + z);
	//        if (dropped.Amount == 100) {
	//            self.SysMessage("test: Dropping of amount 100 is cancelled");
	//            return 1;
	//        }
	//        return 0;
	//    }
	//    public int On_ItemPickup_Ground(Character self, Item picked, ushort amt) {
	//        self.SysMessage("@itemPickup_Ground: I am picking up " + amt + " of item " + picked + " from ground");
	//        if (amt == 50) {
	//            self.SysMessage("test: You shouldn't be able to pickup amount 50");
	//            return 1;
	//        }
	//        return 0;
	//    }
	//    public int On_ItemPickup_Pack(Character self, Item picked, Container pack, ushort amt) {
	//        self.SysMessage("@itemPickup_Pack: I am picking up " + amt + " of item " + picked + " from pack " + pack);
	//        if (amt == 50) {
	//            self.SysMessage("test: You shouldn't be able to pickup amount 50");
	//            return 1;
	//        }
	//        return 0;
	//    }

	//    public int On_ItemEquip(Character self, Equippable eq) {
	//        self.SysMessage("@itemEquip: I am Equipping item " + eq);
	//        if (eq.Color == 50) {
	//            self.SysMessage("test: You shouldn't be able to equip item with color 50");
	//            return 1;
	//        }
	//        return 0;
	//    }

	//    public void On_ItemUnEquip(Character self, Equippable eq) {
	//        self.SysMessage("@itemUnEquip: I am UnEquipping item " + eq);
	//    }

	//    public void On_ItemStep(Character self, Item i, bool repeated) {
	//        if (repeated) {
	//            self.SysMessage("@ItemStep: I am standing on item " + i);
	//        } else {
	//            self.SysMessage("@ItemStep: I am stepping on item " + i);
	//        }
	//    }
	//}

	public static class TestCommands {
		[SteamFunction]
		public static void LookAround(Character self) {
			self.SysMessage("Things:");
			foreach (Thing t in self.GetMap().GetThingsInRange(self.X, self.Y, self.UpdateRange)) {
				self.SysMessage(t.ToString());
			}
			//that includes players too.

			/*Map map = self.GetMap();
			PlayersEnumerator en = map.EnumPlayersInRange(self.X, self.Y);
			while (en.MoveNext()) {
				self.SysMessage(en.Current.ToString());
			}
			self.SysMessage("Things:");
			ThingsEnumerator enu = map.EnumThingsInRange(self.X, self.Y);
			while (enu.MoveNext()) {
				self.SysMessage(enu.Current.ToString());
			}*/
		}

	}

	//public class TestItem : Equippable {
	//	//here we test overriding of core Thing classes.
	//	//we must also make an ThingDef for this
	//	public TestItem(ThingDef myDef, ushort x, ushort y, sbyte z, byte m): base(myDef, x, y,z,m) {
	//	}
	//	public TestItem(ThingDef myDef, Container cont): base(myDef, cont) {
	//	}
	//	public TestItem(TestItem copyFrom, bool addInCont) : base(copyFrom, addInCont) { //copying constuctor
	//	}
	//
	//	public override Item Dupe(bool addInCont) {
	//		return new TestItem(this, addInCont);
	//	}
	//	
	//	public override void On_Step(bool repeated) {
	//		if (repeated) {
	//			OverheadMessage("On_Step: You are standing on me.");
	//		} else {
	//			OverheadMessage("On_Step: You are stepping on me.");
	//		}
	//	}
	//	
	//	//
	//	public override bool On_StackOn(Item i, int x, int y) {
	//		OverheadMessage("On_StackOn: Item "+i+" is being stacked with me");
	//		return false;
	//	}
	//	
	//	public override bool On_DropOn_Ground(ushort x, ushort y, sbyte z) {
	//		OverheadMessage("On_DropOn_Ground: I am being dropped on ground at "+x+", "+y+", "+z);
	//		return false;
	//	}
	//	//
	//	
	//	public override bool On_Pickup_Ground(ushort amt) {
	//		OverheadMessage("On_Pickup_Ground: "+amt+" of me being picked up from ground");
	//		return false;
	//	}
	//	
	//	public override bool On_Pickup_Pack(ushort amt) {
	//		OverheadMessage("On_Pickup_Pack: "+amt+" of me being picked up from pack"+cont);
	//		return false;
	//	}
	//	
	//	public override void On_Destroy() {
	//		if (Globals.src!=null) {
	//			OverheadMessage("On_Destroy: I am being destroyed");
	//		}
	//	}
	//	
	//	public override bool On_Equip() {
	//		OverheadMessage("On_Equip: I am being equipped to "+Globals.src);
	//		return false;
	//	}
	//	
	//	public override void On_UnEquip() {
	//		OverheadMessage("On_UnEquip: I am being unequipped from "+Globals.src);
	//	}
	//	
	//	public override void On_Create() {
	//		OverheadMessage("On_Create: I am being created");
	//	}
	//	
	//	public override void On_DClick() {
	//		OverheadMessage("On_DClick: I am being doubleclicked");
	//	}
	//	
	//	public override void On_Click() {
	//		OverheadMessage("On_Click: I am being clicked");
	//	}
	//}
}