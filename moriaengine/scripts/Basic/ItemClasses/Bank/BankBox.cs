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


using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class BankBoxDef {
	}

	[ViewableClass]
	public partial class BankBox {
		/// <summary>#879 - a client ignores opening an item it doesn't know, and a bank box sits on a
		/// layer that's never sent. Sphere's addBankOpen sends it to the owner first.</summary>
		public void OpenBankTo(Character viewer) {
			GameState state = viewer.GameState;
			if (state == null) {
				return;
			}
			WornItemOutPacket packet = Pool<WornItemOutPacket>.Acquire();
			packet.PrepareItem(this.Cont.FlaggedUid, this);
			state.Conn.SendSinglePacket(packet);
			this.OpenTo(viewer);
		}

		/// <summary>Sphere doesn't count a bank towards what its owner carries.</summary>
		protected override bool IsWeighed {
			get {
				return false;
			}
		}

		/// <summary>The inside of the chest gump, from Sphere's table of them.</summary>
		public override void GetContainerGumpBoundaries(out int minX, out int minY, out int maxX, out int maxY) {
			minX = 18;
			minY = 105;
			maxX = 118;
			maxY = 169;
		}

		protected override bool IsReachableOnInvisibleLayer {
			get {
				return true;
			}
		}

		/// <summary>
		/// Real UO bank boxes are only accessible through a banker, not by double-clicking them
		/// directly (enhancements Phase 14.5, issue #36) - Container.On_DClick has no such gate,
		/// since it's shared by every other container. GMs are exempted so admin inspection via
		/// the normal container-open path still works.
		/// </summary>
		public override void On_DClick(AbstractCharacter from) {
			Character fromAsChar = from as Character;
			if ((fromAsChar != null) && fromAsChar.IsGM) {
				base.On_DClick(from);
				return;
			}
			from.SysMessage("Thou must speak with a banker to access thy bank box.");
		}
	}
}