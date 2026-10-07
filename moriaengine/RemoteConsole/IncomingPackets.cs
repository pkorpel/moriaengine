using System.Collections.Generic;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.RemoteConsole {

	public class ConsoleProtocol : IProtocol<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		public static readonly ConsoleProtocol instance = new ConsoleProtocol();


		public IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id, TcpConnection<ConsoleClient> conn, ConsoleClient state, out bool discardAfterReading) {
			discardAfterReading = false;
			switch (id) {
				case 1:
					return Pool<RequestOpenGameServerWindowPacket>.Acquire();
				case 2:
					return Pool<RequestCloseGameServerWindowPacket>.Acquire();
				case 3:
					return Pool<RequestEnableCommandLinePacket>.Acquire();
				case 4:
					return Pool<WriteStringPacket>.Acquire();
				case 5:
					return Pool<WriteLinePacket>.Acquire();
				case 6:
					return Pool<SendServersToStartPacket>.Acquire();
				case 7:
					return Pool<LoginFailedPacket>.Acquire();
			}

			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		private static Queue<PacketQueuedForInvoking> primaryQueue = new Queue<PacketQueuedForInvoking>();
		private static Queue<PacketQueuedForInvoking> secondaryQueue = new Queue<PacketQueuedForInvoking>();

		public override void Dispose() {//called by AsyncCore
		}

		private void RealDispose() {
			base.Dispose();
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			PacketQueuedForInvoking helper = new PacketQueuedForInvoking(this, conn);
			lock (primaryQueue) {
				primaryQueue.Enqueue(helper);
			}
		}

		class PacketQueuedForInvoking {
			internal readonly ConsoleIncomingPacket packet;
			internal readonly TcpConnection<ConsoleClient> conn;

			internal PacketQueuedForInvoking(ConsoleIncomingPacket packet, TcpConnection<ConsoleClient> conn) {
				this.packet = packet;
				this.conn = conn;
			}
		}

		//called by Windows.Forms.Timer on the MainForm, so we need no Invoke (which would get called for every single packet otherwise), but we still are in the UI thread
		internal static void HandleQueuedPackets() {
			lock (primaryQueue) { //we switch the primary and secondary
				Queue<PacketQueuedForInvoking> tempQueue = primaryQueue;
				primaryQueue = secondaryQueue;
				secondaryQueue = tempQueue;
			}

			while (secondaryQueue.Count > 0) {
				PacketQueuedForInvoking helper = secondaryQueue.Dequeue();
				if (helper.conn.IsConnected) {
					helper.packet.HandleInUIThread(helper.conn, helper.conn.State);
					helper.packet.RealDispose();
				}
			}
		}

		protected virtual void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
		}
	}

	public sealed class RequestOpenGameServerWindowPacket : ConsoleIncomingPacket {
		int uid;
		string name;

		protected override void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.AddCmdLineDisplay((GameUid) this.uid, this.name);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.name = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}

	public sealed class RequestCloseGameServerWindowPacket : ConsoleIncomingPacket {
		int uid;

		protected override void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.RemoveCmdLineDisplay((GameUid) this.uid);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}
	}

	public sealed class RequestEnableCommandLinePacket : ConsoleIncomingPacket {
		int uid;

		protected override void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.EnableCommandLineOnDisplay((GameUid) this.uid);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}
	}

	public sealed class WriteStringPacket : ConsoleIncomingPacket {
		int uid;
		string str;

		protected override void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.Write((GameUid) this.uid, this.str);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.str = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}

	public sealed class WriteLinePacket : ConsoleIncomingPacket {
		int uid;
		string str;

		protected override void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			MainClass.mainForm.WriteLine((GameUid) this.uid, this.str);
		}

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.str = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}

	public sealed class SendServersToStartPacket : ConsoleIncomingPacket {
		GameServerEntry[] entries;

		protected override void HandleInUIThread(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			new StartGameForm(this.entries).ShowDialog();
		}

		protected override ReadPacketResult Read() {
			int count = this.DecodeInt();
			this.entries = new GameServerEntry[count];

			for (int i = 0; i < count; i++) {
				int number = this.DecodeInt();
				string iniPath = this.DecodeUTF8String();
				string name = this.DecodeUTF8String();
				ushort port = this.DecodeUShort();
				bool running = this.DecodeBool();

				this.entries[i] = new GameServerEntry(number, iniPath, name, port, running);
			}

			return ReadPacketResult.Success;
		}

		public class GameServerEntry {
			private readonly int number;
			private readonly string iniPath;
			private readonly string name;
			private readonly ushort port;
			private readonly bool running;

			internal GameServerEntry(int number, string iniPath, string name, ushort port, bool running) {
				this.iniPath = iniPath;
				this.number = number;
				this.name = name;
				this.port = port;
				this.running = running;
			}

			public string DisplayText {
				get
				{
					if (this.running) {
						return this.name + " (on)";
					}
					return this.name + " (off)";
				}
			}

			public int Number {
				get {
					return this.number;
				}
			}

			public string IniPath {
				get {
					return this.iniPath;
				}
			}

			public string Name {
				get {
					return this.name;
				}
			}

			public ushort Port {
				get {
					return this.port;
				}
			}

			public bool Running {
				get {
					return this.running;
				}
			}
		}
	}

	public sealed class LoginFailedPacket : ConsoleIncomingPacket {
		string reason;

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			ConsoleClient.Disconnect("Login failed: " + this.reason);
			this.Dispose();
		}

		protected override ReadPacketResult Read() {
			this.reason = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}
	}
}