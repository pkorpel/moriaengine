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
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	public class LocDenyResult : DenyResult {
		private string entryName;
		LocStringCollection collection;

		public LocDenyResult(LocStringCollection collection, string entryName) {
			this.collection = collection;
			this.entryName = entryName;
		}

		public override void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn) {
			if (ch != null) {
				string msg = this.collection.GetEntry(this.entryName);
				if (!string.IsNullOrEmpty(msg)) {
					PacketSequences.SendSystemMessage(conn, msg, -1);
				}
			}
		}
	}

	public class CompiledLocDenyResult<T> : DenyResult where T : CompiledLocStringCollection<T> {
		private readonly string entryName;

		public CompiledLocDenyResult(string entryName) {
			this.entryName = entryName;
		}

		public override void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn) {
			if (ch != null) {
				CompiledLocStringCollection<T> loc = Loc<T>.Get(ch.Language);
				string msg = loc.GetEntry(this.entryName);
				if (!string.IsNullOrEmpty(msg)) {
					PacketSequences.SendSystemMessage(conn, msg, -1);
				}
			}
		}
	}
}