using System;
using System.Net;
using System.Net.Sockets;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {
	public class ConsoleClient : //Disposable,
		IConnectionState<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

		static TcpClientFactory<ConsoleClient> factory = new TcpClientFactory<ConsoleClient>(
			ConsoleProtocol.instance, MainClass.globalLock, MainClass.exitTokenSource.Token);


		private static ConsoleClient connectedInstance;
		private TcpConnection<ConsoleClient> conn;

		internal EndPointSetting endPointSetting;

		public static ConsoleClient ConnectedInstance {
			get {
				return connectedInstance;
			}
		}

		public static bool IsConnected {
			get {
				return ((connectedInstance != null) && (connectedInstance.Conn.IsConnected));
			}
		}

		public TcpConnection<ConsoleClient> Conn {
			get {
				return this.conn;
			}
		}

		public IEncryption Encryption {
			get {
				return null;
			}
		}

		public ICompression Compression {
			get {
				return null;
			}
		}

		public void On_Init(TcpConnection<ConsoleClient> conn) {
			connectedInstance = this;
			this.conn = conn;
			MainClass.mainForm.SetConnected(true);

			Console.WriteLine("Connected to " + conn.EndPoint);
			//IdentifyGameServerPacket packet = Pool<IdentifyGameServerPacket>.Acquire();
			//packet.Prepare();
			//conn.SendSinglePacket(packet);
		}

		private delegate void NoParamDeleg();

		public void On_Close(string reason) {
			IPEndPoint ep = this.conn.EndPoint;
			Console.WriteLine("Disconnected from " + ep + ": " + reason);

			connectedInstance = null;
			MainClass.mainForm.Invoke(new NoParamDeleg(MainClass.mainForm.SetConnectedFalse));
			MainClass.mainForm.Invoke(new NoParamDeleg(MainClass.mainForm.ClearCmdLineDisplays));

			if (this.endPointSetting.KeepReconnecting) {
				MainClass.mainForm.DelayReconnect(this.endPointSetting);
			}
		}

		public static void SendCommand(GameUid id, string command) {
			if (connectedInstance != null) {
				CommandLinePacket p = Pool<CommandLinePacket>.Acquire();
				p.Prepare(id, command);
				connectedInstance.conn.SendSinglePacket(p);
			}
		}

		public static void Connect(EndPointSetting eps) {
			if (IsConnected) {
				return;
			}
			try {
				IPAddress[] ips = Dns.GetHostAddresses(eps.Address);

				bool compatibleAddressPresent = false;
				foreach (IPAddress ip in ips) {
					if (ip.AddressFamily == AddressFamily.InterNetwork) {
						compatibleAddressPresent = true;
						TcpConnection<ConsoleClient> createdConn = factory.Connect(new IPEndPoint(ip, eps.Port));

						Settings.Save();
						createdConn.State.endPointSetting = eps;
						RequestLoginPacket packet = Pool<RequestLoginPacket>.Acquire();
						packet.Prepare(eps.UserName, eps.Password);
						createdConn.SendSinglePacket(packet);
						return;
					}
				}
				if (!compatibleAddressPresent) {
					Common.Logger.WriteError("Incompatible Address: '" + eps.Address + "'. Only IPv4 addresses supported");
					return;
				}
			} catch (Exception e) {
				Common.Logger.WriteError(e);
			}

			if (eps.KeepReconnecting) {
				MainClass.mainForm.DelayReconnect(eps);
			}
		}

		public static void Disconnect(string reason) {
			if (connectedInstance != null) {
				connectedInstance.endPointSetting.KeepReconnecting = false;
				connectedInstance.conn.Close(reason);
			}
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return false;
			}
		}


		public void On_PacketBeingHandled(IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> packet) {

		}
	}
}

