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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	public class t_mortar : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			CraftingSkillDef.OpenMenu(dclicker, SkillName.Alchemy);
			return TriggerResult.Continue;
		}
	}

	public class t_anvil : CompiledTriggerGroup {
		//nothing to click on: repairing needs one near, smithing needs a forge
	}

	public class t_weapon_mace_smith : CompiledTriggerGroup {
		//the weapon smith hammer is primarily for fixing things
	}

	public class t_forge : CompiledTriggerGroup {
		//forge is for smelting ore (and training blacksmithy through it)
	}

	public class t_shaft : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			CraftingSkillDef.OpenMenu(dclicker, SkillName.Fletching);
			return TriggerResult.Continue;
		}
	}

	public class t_carpentry : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			CraftingSkillDef.OpenMenu(dclicker, SkillName.Carpentry);
			return TriggerResult.Continue;
		}
	}

	public class t_sewing_kit : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			if (dclicker is Player) {
				((Player) dclicker).Target(SingletonScript<Targ_SewingKit>.Instance);
			}
			return TriggerResult.Continue;
		}
	}

	//#962 - marks for what a recipe needs at hand, as Erebor's empty typedefs: questy/noe.scp
	public class t_bottle_form : CompiledTriggerGroup {
	}

	public class t_book_bottle : CompiledTriggerGroup {
	}

	public class t_book_shrink : CompiledTriggerGroup {
	}

	public class t_tinker_tools : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			CraftingSkillDef.OpenMenu(dclicker, SkillName.Tinkering);
			return TriggerResult.Continue;
		}
	}
}
