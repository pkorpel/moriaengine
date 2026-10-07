using System;
using System.Net;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginClient : //Disposable,
		IConnectionState<TcpConnection<LoginClient>, LoginClient, IPEndPoint> {

		static int uids;

		int uid = uids++;

		IEncryption encryption = new LoginEncryption();

		public IEncryption Encryption {
			get {
				return this.encryption;
			}
		}

		public ICompression Compression {
			get {
				return null;
			}
		}

		public void On_Init(TcpConnection<LoginClient> conn) {
			Console.WriteLine(this + " connected from " + conn.EndPoint);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);
		}

		public override string ToString() {
			return "LoginClient " + this.uid;
		}

		//public override void Handle(IncomingPacket packet) {
		//    ConsoleServerPacketGroup pg = Pool<ConsoleServerPacketGroup>.Acquire();

		//    pg.AddPacket(Pool<ConsoleServerOutgoingPacket>.Acquire());

		//    //MainClass.server.SendPacketGroup(this, pg);
		//}

		public bool PacketGroupsJoiningAllowed {
			get {
				return false;
			}
		}

		public void On_PacketBeingHandled(IncomingPacket<TcpConnection<LoginClient>, LoginClient, IPEndPoint> packet) {

		}
	}
}
