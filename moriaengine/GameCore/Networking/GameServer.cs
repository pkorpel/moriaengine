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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.Networking {
	public class GameServer : TcpServer<GameState> {

		private static GameServer instance = new GameServer();

		private static HashSet<GameState> clients = new HashSet<GameState>();
		private static ReadOnlyCollectionWrapper<GameState> clientsReadOnly = CreateClientsWrapper();

		static ReadOnlyCollectionWrapper<GameState> CreateClientsWrapper() {
			return new ReadOnlyCollectionWrapper<GameState>(clients);
		}

		private GameServer()
			: base(GameServerProtocol.instance, MainClass.globalLock, MainClass.ExitToken) {

		}

		internal static void Init() {
			instance.Bind(new IPEndPoint(IPAddress.Any, Globals.Port));
		}

		internal static void On_ClientInit(GameState gc) {
			clients.Add(gc);

			SyncQueue.Enable();
		}

		internal static void On_ClientClose(GameState gc) {
			clients.Remove(gc);

			if (clients.Count == 0) {
				SyncQueue.Disable();
			}
		}

		internal static void Exit() {
			instance.Dispose();
		}

		public static ReadOnlyCollectionWrapper<GameState> AllClients {
			get {
				return clientsReadOnly;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public static IEnumerable<AbstractCharacter> GetAllPlayers() {
#if NOLINQ
			foreach (GameState state in clients) {
			    AbstractCharacter ch = state.Character;
			    if (ch != null) {
			        yield return ch;
			    }
			}
#else
			return from GameState state in clients
				   let ch = state.Character
				   where ch != null
				   select ch;
#endif
		}

		public static void SendToClientsWhoCanSee(Thing thing, OutgoingPacket outPacket) {
			PacketGroup pg = null;

			AbstractItem asItem = thing as AbstractItem;
			if (asItem != null) {
				AbstractItem contAsItem = asItem.Cont as AbstractItem;
				if (contAsItem != null) {
					foreach (AbstractCharacter viewer in OpenedContainers.GetViewers(contAsItem)) {
						GameState state = viewer.GameState;
						if (state != null) {
							if (pg == null) {
								pg = PacketGroup.AcquireMultiUsePG();
								pg.AddPacket(outPacket);
							}
							state.Conn.SendPacketGroup(pg);
						}
					}
					return;
				}
			}

			foreach (TcpConnection<GameState> conn in thing.TopObj().GetMap().GetConnectionsWhoCanSee(thing)) {
				if (pg == null) {
					pg = PacketGroup.AcquireMultiUsePG();
					pg.AddPacket(outPacket);
				}
				conn.SendPacketGroup(pg);
			}

			if (pg == null) {//wasn't sent
				outPacket.Dispose();
			} else {
				pg.Dispose();
			}
		}

		public static void SendToClientsWhoCanSee(AbstractCharacter ch, OutgoingPacket outPacket) {
			PacketGroup pg = null;

			foreach (TcpConnection<GameState> conn in ch.GetMap().GetConnectionsWhoCanSee(ch)) {
				if (pg == null) {
					pg = PacketGroup.AcquireMultiUsePG();
					pg.AddPacket(outPacket);
				}
				conn.SendPacketGroup(pg);
			}
			if (pg == null) {//wasn't sent
				outPacket.Dispose();
			} else {
				pg.Dispose();
			}
		}

		public static void SendToClientsWhoCanSee(AbstractItem item, OutgoingPacket outPacket) {
			PacketGroup pg = null;

			AbstractItem contAsItem = item.Cont as AbstractItem;
			if (contAsItem != null) {
				foreach (AbstractCharacter viewer in OpenedContainers.GetViewers(contAsItem)) {
					GameState state = viewer.GameState;
					if (state != null) {
						if (pg == null) {
							pg = PacketGroup.AcquireMultiUsePG();
							pg.AddPacket(outPacket);
						}
						state.Conn.SendPacketGroup(pg);
					}
				}
				return;
			}
			foreach (TcpConnection<GameState> conn in item.GetMap().GetConnectionsWhoCanSee(item)) {
				if (pg == null) {
					pg = PacketGroup.AcquireMultiUsePG();
					pg.AddPacket(outPacket);
				}
				conn.SendPacketGroup(pg);
			}

			if (pg == null) {//wasn't sent
				outPacket.Dispose();
			} else {
				pg.Dispose();
			}
		}

		public static void SendToClientsInRange(IPoint4D point, int range, OutgoingPacket outPacket) {
			point = point.TopPoint;
			PacketGroup pg = null;

			foreach (TcpConnection<GameState> conn in point.GetMap().GetConnectionsInRange(point.X, point.Y, range)) {
				if (pg == null) {
					pg = PacketGroup.AcquireMultiUsePG();
					pg.AddPacket(outPacket);
				}
				conn.SendPacketGroup(pg);
			}

			if (pg == null) {//wasn't sent
				outPacket.Dispose();
			} else {
				pg.Dispose();
			}
		}

		internal static void BackupLinksToCharacters() {
			foreach (GameState state in clients) {
				state.BackupLinksToCharacters();
			}
		}

		internal static void ReLinkCharacters() {
			foreach (GameState state in clients) {
				state.RelinkCharacter();
			}
		}

		internal static void RemoveBackupLinks() {
			foreach (GameState state in clients) {
				state.RemoveBackupLinks();
			}
		}
	}
}