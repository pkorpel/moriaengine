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
			D_Craftmenu.Craftmenu(dclicker, SkillName.Alchemy);
			return TriggerResult.Continue;
		}
	}

	public class t_anvil : CompiledTriggerGroup {
		//anvil is for blacksmithing skill
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			D_Craftmenu.Craftmenu(dclicker, SkillName.Blacksmith);
			return TriggerResult.Continue;
		}
	}

	public class t_weapon_mace_smith : CompiledTriggerGroup {
		//the weapon smith hammer is primarily for fixing things
	}

	public class t_forge : CompiledTriggerGroup {
		//forge is for smelting ore (and training blacksmithy through it)
	}

	public class t_shaft : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			D_Craftmenu.Craftmenu(dclicker, SkillName.Fletching);
			return TriggerResult.Continue;
		}
	}

	public class t_carpentry : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			D_Craftmenu.Craftmenu(dclicker, SkillName.Carpentry);
			return TriggerResult.Continue;
		}
	}

	public class t_sewing_kit : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			D_Craftmenu.Craftmenu(dclicker, SkillName.Tailoring);
			return TriggerResult.Continue;
		}
	}

	public class t_tinker_tools : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			D_Craftmenu.Craftmenu(dclicker, SkillName.Tinkering);
			return TriggerResult.Continue;
		}
	}
}
