using System;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;

namespace SteamEngine.AuxiliaryServer.SEGameServers {
	public class SEGameServerClient : GameServer,
#if MSWIN
 IConnectionState<NamedPipeConnection<SEGameServerClient>, SEGameServerClient, string> {
#else
		IConnectionState<NamedPipeConnection<SEGameServerClient>, SEGameServerClient, System.Net.IPEndPoint> {
#endif


		NamedPipeConnection<SEGameServerClient> conn;

		SEGameServerSetup settings;

		private bool startupFinished;

		static GameUid uids = GameUid.FirstSEGameServer;

		public NamedPipeConnection<SEGameServerClient> Conn {
			get {
				return this.conn;
			}
		}

		internal void SetStartupFinished(bool p) {
			this.startupFinished = p;
		}

		public override bool StartupFinished {
			get {
				return this.startupFinished;
			}
		}

		public override IGameServerSetup Setup {
			get {
				return this.settings;
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

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "conn")]
		public void On_Init(NamedPipeConnection<SEGameServerClient> conn) {
			this.uid = uids++;
			Console.WriteLine(this + " connected.");
			this.conn = conn;

			GameServersManager.AddGameServer(this);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			GameServersManager.RemoveGameServer(this);
		}

		public override string ToString() {
			return "SEGame 0x" + ((int) this.ServerUid).ToString("X");
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "settings")]
		internal void SetIdentificationData(SEGameServerSetup settings) {
			this.settings = settings;
		}

		internal void WriteToMyConsoles(string str) {
			if (this.startupFinished) {
				foreach (ConsoleClient console in GameServersManager.AllConsolesIn(this)) {
					console.Write(this.ServerUid, str);
				}
			} else {
				foreach (ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
					console.Write(this.ServerUid, str);
				}
			}
		}

		internal void RequestSendingLogStr(bool state) {
			RequestSendingLogStringsPacket packet = Pool<RequestSendingLogStringsPacket>.Acquire();
			packet.Prepare(state);
			this.conn.SendSinglePacket(packet);
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return false;
			}
		}

		public override void SendCommand(ConsoleClient console, string cmd) {
			ConsoleCommandLinePacket p = Pool<ConsoleCommandLinePacket>.Acquire();
			p.Prepare(console.ConsoleId, console.AccountName, console.AccountPassword, cmd);
			this.Conn.SendSinglePacket(p);
		}

		public override void SendConsoleLogin(ConsoleId consoleId, string accName, string accPassword) {
			ConsoleLoginRequestPacket loginRequest = Pool<ConsoleLoginRequestPacket>.Acquire();
			loginRequest.Prepare(consoleId, accName, accPassword);
			this.Conn.SendSinglePacket(loginRequest);
		}


		public void On_PacketBeingHandled(IncomingPacket<NamedPipeConnection<SEGameServerClient>, SEGameServerClient, string> packet) {

		}
	}
}
