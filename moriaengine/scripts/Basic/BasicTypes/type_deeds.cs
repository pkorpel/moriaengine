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

	public class t_deed : CompiledTriggerGroup {
		/// <summary>#884 - Sphere's deed: pick a spot and the stone stands there.</summary>
		public void On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if ((player != null) && (self.Def == ThingDef.GetByDefname("i_deed_GUILDSTONE"))) {
				player.Target(SingletonScript<Targ_PlaceGuildstone>.Instance, self);
			}
		}
	}

	public class t_stone_town : CompiledTriggerGroup {

	}

	public class t_normal : CompiledTriggerGroup {

	}
}