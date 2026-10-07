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
	[ViewableClass]
	public partial class EquippableDef {
	}

	[ViewableClass]
	public partial class Equippable : Item {
		public override int Layer {
			get {
				return this.TypeDef.Layer;
			}
		}

		public sealed override bool IsEquippable {
			get {
				return this.TypeDef.Layer != 0;
			}
		}

		//public void Equip() {
		//    AbstractCharacter src = Globals.SrcCharacter;
		//    src.Equip(src, this);
		//}

		public override void On_DClick(AbstractCharacter from) {
			if (this.IsEquippable) {
				//pick it up into dragging layer, then equip it. 
				//We want the same triggers fired as if the client dragged it manually (yes, a lot of triggers)
				
				if ((this.Cont != from) || (this.Z != this.Layer)) { //we only try the equipping if not already equipped
					DenyResult dr = from.TryPickupItem(this, 1);
					if (dr.Allow) {
						dr = from.TryEquipItemOnChar(from);
					}
					if (!dr.Allow) {
						dr.SendDenyMessage(from);
					}
				}
			}
		}

		public override bool IsTwoHanded {
			get {
				if (this.Def != null) return this.TypeDef.TwoHanded;
				return false;
			}
		}
	}
}