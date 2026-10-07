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

	public class t_light_lit : CompiledTriggerGroup {

	}

	public class t_telepad : CompiledTriggerGroup {	//Our script must extend 'CompiledTriggerGroup'

		public void On_Step(Item self, Character steppingChar, Item selfAgain, bool repeated, MovementType movementType) {
			//only tries once after walking on
			if ((movementType & MovementType.Teleporting) != MovementType.Teleporting) {
				if (!repeated) {
					//todo some checks? effects? sounds?
					Point4D point = self.MoreP;
					if (point != null) {
						steppingChar.Go(point);
					}
				}
			}
		}

		//static TagKey morexTag = TagKey.Get("morex");
		//static TagKey moreyTag = TagKey.Get("morey");
		//static TagKey morezTag = TagKey.Get("morez");
		//static TagKey moremTag = TagKey.Get("morem");

		//public void on_step(TagHolder self) {
		//    ushort morex= (ushort) TagMath.ConvertTo(typeof(ushort), self.GetTag(morexTag));
		//    ushort morey=(ushort) TagMath.ConvertTo(typeof(ushort), self.GetTag(moreyTag));
		//    sbyte morez=(sbyte) TagMath.ConvertTo(typeof(sbyte), self.GetTag(morezTag));
		//    byte morem=(byte) TagMath.ConvertTo(typeof(byte), self.GetTag(moremTag));
		//    if (Map.IsValidPos(morex, morey, morem)) {
		//        ((Character) Globals.src).Go(morex,morey,morez,morem);
		//    }
		//}
	}

	public class t_fire : CompiledTriggerGroup {

		public void on_step(TagHolder self, Character steppingChar) {
			//if (Globals.src.IsAlive) {
			//todo ;)
			steppingChar.Hits -= 2;
			//}
		}
	}

	public class t_spell : CompiledTriggerGroup {

	}

	public class t_trap_active : CompiledTriggerGroup {

	}
}