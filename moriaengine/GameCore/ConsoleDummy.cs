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

using SteamEngine.AuxServerPipe;
using SteamEngine.Common;

namespace SteamEngine {
	public class ConsoleDummy : ISrc {
		private AbstractAccount account;
		private int consoleId;
		private readonly int uid;

		private static int uids;

		internal ConsoleDummy(AbstractAccount account, int consoleId) {
			this.account = account;
			this.consoleId = consoleId;
			this.uid = uids++;
		}

		public override int GetHashCode() {
			return this.uid;
		}

		public override bool Equals(object obj) {
			return this == obj;
		}

		public override string ToString() {
			return string.Concat("Console dummy(acc='", this.Account, "')");
		}

		public byte Plevel {
			get { return this.account.PLevel; }
		}

		public byte MaxPlevel {
			get { return this.account.MaxPLevel; }
		}

		public void WriteLine(string line) {
			AuxServerPipeClient aspc = AuxServerPipeClient.ConnectedInstance;
			if (aspc != null) {
				ConsoleWriteLinePacket p = Pool<ConsoleWriteLinePacket>.Acquire();
				p.Prepare(this.consoleId, line);
				aspc.PipeConnection.SendSinglePacket(p);
			}
		}

		public AbstractAccount Account {
			get { return this.account; }
		}

		public Language Language {
			get {
				return Language.Default;
			}
		}
	}
}