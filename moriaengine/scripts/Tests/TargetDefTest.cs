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

	public class Targ_Test_Compiled : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Target whatever you want...");
			base.On_Start(self, parameter);
		}

		//protected override bool On_TargonPoint(Character self, IPoint3D targetted, object parameter) {
		//    self.SysMessage("You targetted "+targetted+", parameter "+parameter);
		//    return true;
		//}

		//protected override bool On_TargonThing(Character self, Thing targetted, object parameter) {
		//    self.SysMessage("You targetted thing "+targetted+", parameter "+parameter);
		//    return true;
		//}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("You targetted char " + targetted + ", parameter " + parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("You targetted item " + targetted + ", parameter " + parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonStatic(Player self, AbstractInternalItem targetted, object parameter) {
			self.SysMessage("You targetted static item " + targetted + ", parameter " + parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("You targetted ground at " + targetted + ", parameter " + parameter);
			return TargetResult.Done;
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			self.SysMessage("You cancelled the target, parameter " + parameter);
		}
	}
}