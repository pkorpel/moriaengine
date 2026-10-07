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

namespace SteamEngine.CompiledScripts {

	public class t_bee_hive : CompiledTriggerGroup {

	}

	public class t_food : CompiledTriggerGroup {

	}

	public class t_food_raw : CompiledTriggerGroup {

	}

	public class t_container : CompiledTriggerGroup {

	}

	public class t_container_locked : CompiledTriggerGroup {
		//public int on_PickedUpFrom() {
		//    //src attempts to pick up something from the container. If it wasnt some kind of cheater, 
		//    //he would not have the container open.
		//    Globals.src.WriteLine("The container is locked, you cheater!");
		//    if (Globals.src.Character.IsPlevelAtLeast(Globals.plevelToMoveAll)) {	//if ((Globals.src.Account!=null)&&(Globals.src.Account.plevel>=Globals.plevelToMoveAll)) {
		//        return 0;
		//    }
		//    return 1;
		//}

		//public int On_DClick() {
		//    //src attempts to open this container
		//    Globals.SrcWriteLine("The container is locked.");
		//    if (Globals.src.Character.IsPlevelAtLeast(Globals.plevelToMoveAll)) {	//if ((Globals.src.Account!=null)&&(Globals.src.Account.plevel>=Globals.plevelToMoveAll)) {
		//        return 0;
		//    }
		//    return 1;
		//}

		//public int On_StackOn() {
		//    //src attempts to drop something in this container
		//    Globals.src.WriteLine("The container is locked.");
		//    if (Globals.src.Character.IsPlevelAtLeast(Globals.plevelToMoveAll)) {	//if ((Globals.src.Account!=null)&&(Globals.src.Account.plevel>=Globals.plevelToMoveAll)) {
		//        return 0;
		//    }
		//    return 1;
		//}
	}

	public class t_fruit : CompiledTriggerGroup {

	}

	public class t_drink : CompiledTriggerGroup {

	}

	public class t_meat_raw : CompiledTriggerGroup {

	}

	public class t_water_wash : CompiledTriggerGroup {

	}

	public class t_fish_pole : CompiledTriggerGroup {

	}

	public class t_fish : CompiledTriggerGroup {

	}

	public class t_scissors : CompiledTriggerGroup {

	}

	public class t_game_board : CompiledTriggerGroup {

	}

	public class t_yarn : CompiledTriggerGroup {

	}

	public class t_hair_dye : CompiledTriggerGroup {

	}

	public class t_crystal_ball : CompiledTriggerGroup {

	}

	//The functions of t_musical are implemented in the Musical item type.
	public class t_musical : CompiledTriggerGroup {

		//public void on_create(Item self) {
		//    throw new ScriptException("t_musical is obsolete and should not be used: Musical instruments' defs should be defined as [Musical i_foobar], and they don't need a type. (Musical is defined in scripts/defaults/classtemplates/professionItems.ct)");
		//}
	}

	public class t_shovel : CompiledTriggerGroup {

	}

	public class t_weapon_arrow : CompiledTriggerGroup {

	}

	public class t_cloth_bolt : CompiledTriggerGroup {

	}	

	public class t_thread : CompiledTriggerGroup {

	}

	public class t_dye : CompiledTriggerGroup {

	}

	public class t_dye_vat : CompiledTriggerGroup {

	}
		
	public class t_book : CompiledTriggerGroup {

	}

	public class t_archery_butte : CompiledTriggerGroup {

	}

	public class t_spinwheel : CompiledTriggerGroup {

	}

	public class t_feather : CompiledTriggerGroup {

	}		

	public class t_clock : CompiledTriggerGroup {

	}

	public class t_sextant : CompiledTriggerGroup {

	}

	public class t_loom : CompiledTriggerGroup {

	}

	public class t_leather : CompiledTriggerGroup {

	}

	public class t_train_dummy : CompiledTriggerGroup {

	}

	public class t_hide : CompiledTriggerGroup {

	}

	public class t_weapon_fence : CompiledTriggerGroup {

	}

	public class t_fur : CompiledTriggerGroup {

	}

	public class t_grain : CompiledTriggerGroup {

	}

	public class t_map : CompiledTriggerGroup {

	}

	public class t_chair : CompiledTriggerGroup {

	}

	public class t_cloth : CompiledTriggerGroup {

	}

	public class t_light_out : CompiledTriggerGroup {

	}

	public class t_reagent_raw : CompiledTriggerGroup {

	}

	public class t_crops : CompiledTriggerGroup {

	}

	public class t_weapon_bolt : CompiledTriggerGroup {

	}

	public class t_train_pickpocket : CompiledTriggerGroup {

	}

	public class t_arock : CompiledTriggerGroup {

	}
}
