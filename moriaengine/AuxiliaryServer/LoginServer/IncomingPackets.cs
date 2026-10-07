using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginServerProtocol : IProtocol<TcpConnection<LoginClient>, LoginClient, IPEndPoint> {
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly LoginServerProtocol instance = new LoginServerProtocol();


		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IncomingPacket<TcpConnection<LoginClient>, LoginClient, IPEndPoint> GetPacketImplementation(byte id, TcpConnection<LoginClient> conn, LoginClient state, out bool discardAfterReading) {
			discardAfterReading = false;
			switch (id) {
				case 0x80:
					return Pool<GameLoginPacket>.Acquire();

				case 0xcf:
					return Pool<IgrLoginPacket>.Acquire();

				case 0xa4:
					return Pool<GameSpyPacket>.Acquire();

				case 0xa0:
					return Pool<ServerSelectPacket>.Acquire();
			}

			return null;
		}
	}

	public abstract class LoginIncomingPacket : IncomingPacket<TcpConnection<LoginClient>, LoginClient, IPEndPoint> {

	}

	public class GameSpyPacket : LoginIncomingPacket {
		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(148);
			return ReadPacketResult.DiscardSingle;
		}

		protected override void Handle(TcpConnection<LoginClient> conn, LoginClient state) {
			throw new SEException("The method or operation is not implemented.");
		}
	}

	public class GameLoginPacket : LoginIncomingPacket {
		string accname;

		protected override ReadPacketResult Read() {
			this.accname = this.DecodeAsciiString(30);
			this.SeekFromCurrent(31);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<LoginClient> conn, LoginClient state) {
			Console.WriteLine(state + " identified as " + this.accname);

			ServersListPacket serverList = Pool<ServersListPacket>.Acquire();
			byte[] remoteAddress = conn.EndPoint.Address.GetAddressBytes();

			serverList.Prepare(ServerUtils.GetMatchingInterfaceAddress(remoteAddress));

			conn.SendSinglePacket(serverList);
		}

		//internal static bool ByteArraysEquals(byte[] a, byte[] b) {
		//    int len = a.Length;
		//    if (len != b.Length) {
		//        return false;
		//    }
		//    for (int i = 0; i < len; i++) {
		//        if (a[i] != b[i]) {
		//            return false;
		//        }
		//    }
		//    return true;
		//}
	}

	//dunno exactly what it means, but I think we can ignore it and just treat it as 0x80
	//IGR=Internetgame room. I know no further details about thismechanismthough.
	public class IgrLoginPacket : GameLoginPacket {
		protected override ReadPacketResult Read() {
			base.Read();
			this.SeekFromCurrent(16);
			return ReadPacketResult.Success;
		}
	}

	public class ServerSelectPacket : LoginIncomingPacket {
		int chosenServer;

		protected override ReadPacketResult Read() {
			this.chosenServer = this.DecodeUShort();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<LoginClient> conn, LoginClient state) {
			LoginToServerPacket packet = Pool<LoginToServerPacket>.Acquire();
			byte[] remoteAddress = conn.EndPoint.Address.GetAddressBytes();

			byte[] localAddress = ServerUtils.GetMatchingInterfaceAddress(remoteAddress);

			packet.Prepare(localAddress, Settings.KnownGameServersList[this.chosenServer].Port);

			conn.SendSinglePacket(packet);
		}
	}
}
