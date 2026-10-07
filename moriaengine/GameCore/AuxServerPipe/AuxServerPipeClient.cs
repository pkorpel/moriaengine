using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.NamedPipes;

namespace SteamEngine.AuxServerPipe {
	public class AuxServerPipeClient : //Disposable,
#if MSWIN
		IConnectionState<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> {
#else
		IConnectionState<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, System.Net.IPEndPoint> {
#endif


		private static NamedPipeClientFactory<AuxServerPipeClient> clientFactory;
		private static AuxServerPipeClient connectedInstance;


		private NamedPipeConnection<AuxServerPipeClient> pipe;
		internal bool sendLogStrings;

		private StringToSendEventHandler onConsoleWrite;
		private StringToSendEventHandler onConsoleWriteLine;

		public AuxServerPipeClient() {
			this.onConsoleWrite = this.Logger_OnConsoleWrite;
			this.onConsoleWriteLine = this.Logger_OnConsoleWriteLine;
		}

		internal static void Init() {
			clientFactory = new NamedPipeClientFactory<AuxServerPipeClient>(
				AuxServerPipeProtocol.instance,
				MainClass.globalLock,
				MainClass.ExitToken);

			connectingTimer.Change(TimeSpan.Zero, TimeSpan.Zero);

		}

		internal static void Exit() {
			try {
				connectedInstance.PipeConnection.Close("Exiting");
			} catch { }
		}


		public static AuxServerPipeClient ConnectedInstance {
			get {
				return connectedInstance;
			}
		}

		static Timer connectingTimer = new Timer(delegate
		{
			NamedPipeConnection<AuxServerPipeClient> c = null;
			try {

#if MSWIN
				c = clientFactory.Connect(Tools.commonPipeName);
#else
				c = clientFactory.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, Common.Tools.commonPort));
#endif
				//} catch (Exception e) {
				//Logger.WriteError("Unexpected error in timer callback method", e);
			} catch { }

			if (c == null) {
				StartTryingToConnect();
			}
		});

		private static void StartTryingToConnect() {
			connectingTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.Zero);
		}

		[SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "SteamEngine.AuxServerPipe.AuxServerPipeClient+AnnounceStartupFinishedTimer")]
		public void On_Init(NamedPipeConnection<AuxServerPipeClient> conn) {
			connectedInstance = this;
			this.pipe = conn;
			Logger.OnConsoleWrite += this.onConsoleWrite;
			Logger.OnConsoleWriteLine += this.onConsoleWriteLine;

			IdentifyGameServerPacket packet = Pool<IdentifyGameServerPacket>.Acquire();
			packet.Prepare();
			conn.SendSinglePacket(packet);

			new AnnounceStartupFinishedTimer(conn);
		}

		private class AnnounceStartupFinishedTimer : Timers.Timer {
			NamedPipeConnection<AuxServerPipeClient> conn;

			public AnnounceStartupFinishedTimer(NamedPipeConnection<AuxServerPipeClient> conn) {
				this.conn = conn;
				this.DueInSeconds = 0;
			}

			protected override void OnTimeout() {
				if (this.conn.IsConnected) {
					this.conn.SendPacketGroup(StartupFinishedPacket.group);
				}
				this.Delete();
			}
		}

		private void Logger_OnConsoleWriteLine(string data) {
			this.SendLogString(data + Environment.NewLine);
		}

		private void Logger_OnConsoleWrite(string data) {
			this.SendLogString(data);
		}

		private void SendLogString(string data) {
			if (this.sendLogStrings && this.pipe.IsConnected) {
				LogStringPacket packet = Pool<LogStringPacket>.Acquire();
				packet.Prepare(data);
				this.pipe.SendSinglePacket(packet);
			}
		}

		public void On_Close(string reason) {
			connectedInstance = null;
			Logger.OnConsoleWrite -= this.onConsoleWrite;
			Logger.OnConsoleWriteLine -= this.onConsoleWriteLine;
			this.sendLogStrings = false;

			StartTryingToConnect();
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

		public NamedPipeConnection<AuxServerPipeClient> PipeConnection {
			get {
				return this.pipe;
			}
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return true;
			}
		}

		public void On_PacketBeingHandled(IncomingPacket<NamedPipeConnection<AuxServerPipeClient>, AuxServerPipeClient, string> packet) {

		}
	}
}