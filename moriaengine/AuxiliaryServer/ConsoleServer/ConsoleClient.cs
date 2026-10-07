using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {

	public class ConsoleClient : //Disposable,
		IConnectionState<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

		TcpConnection<ConsoleClient> conn;

		static ConsoleId uids;
		ConsoleId uid = uids++;

		string accName;
		string accPass;

		private bool isLoggedInAux;

		public readonly HashSet<GameUid> filteredGameServers = new HashSet<GameUid>();

		public bool IsLoggedInAux {
			get {
				return this.isLoggedInAux;
			}
		}

		public TcpConnection<ConsoleClient> Conn {
			get {
				return this.conn;
			}
		}

		public ConsoleId ConsoleId {
			get {
				return this.uid;
			}
		}

		public IEncryption Encryption {
			get { return null; }
		}

		public ICompression Compression {
			get { return null; }
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "conn")]
		public void On_Init(TcpConnection<ConsoleClient> conn) {
			this.conn = conn;
			Console.WriteLine(this + " connected.");

			ConsoleServer.AddConnection(this);
		}

		public void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			ConsoleServer.RemoveConnection(this);
			GameServersManager.RemoveConsole(this);
		}

		public override string ToString() {
			return "ConsoleClient " + this.uid;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "accName"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "password")]
		internal void SetLoginData(string accName, string accPass) {
			this.accName = accName;
			this.accPass = accPass;
		}

		public string AccountName {
			get {
				return this.accName;
			}
		}

		internal string AccountPassword {
			get {
				return this.accPass;
			}
		}

		public void TryLoginToGameServers() {
			foreach (GameServer gameServer in GameServersManager.AllIdentifiedGameServers) {
				gameServer.SendConsoleLogin(this.uid, this.accName, this.accPass);
			}
		}

		public void TryLoginToGameServer(GameServer gameServer) {
			gameServer.SendConsoleLogin(this.uid, this.accName, this.accPass);
		}

		internal void SetLoggedInToAux(bool announce) {
			if (announce) {
				Console.WriteLine(this + " identified as " + this.accName);
			}

			this.isLoggedInAux = true;

			RequestOpenCommandWindowPacket openWindow = Pool<RequestOpenCommandWindowPacket>.Acquire();
			openWindow.Prepare("AuxiliaryServer", GameUid.AuxServer);
			this.Conn.SendSinglePacket(openWindow);

			this.EnableCommandLine(GameUid.AuxServer);
		}

		internal void EnableCommandLine(GameUid serverUid) {
			RequestEnableCommandLinePacket enableCmdLine = Pool<RequestEnableCommandLinePacket>.Acquire();
			enableCmdLine.Prepare(serverUid);
			this.Conn.SendSinglePacket(enableCmdLine);
		}

		internal void CloseCmdWindow(GameUid serverUid) {
			RequestCloseCommandWindowPacket packet = Pool<RequestCloseCommandWindowPacket>.Acquire();
			packet.Prepare(serverUid);
			this.Conn.SendSinglePacket(packet);
			this.filteredGameServers.Remove(serverUid);
		}

		internal void SetLoggedInTo(GameServer gameServer) {
			if (!this.isLoggedInAux) {
				this.SetLoggedInToAux(false);
			}

			Console.WriteLine(this + " identified as " + this.accName + " with " + gameServer.Setup.Name);

			Settings.RememberUser(this.accName, this.accPass);

			this.OpenCmdWindow(gameServer.Setup.Name, gameServer.ServerUid);
			this.EnableCommandLine(gameServer.ServerUid);

			GameServersManager.AddLoggedIn(this, gameServer);
		}

		internal void OpenCmdWindow(string name, GameUid serverUid) {
			this.filteredGameServers.Add(serverUid); //filtered by default
			RequestOpenCommandWindowPacket packet = Pool<RequestOpenCommandWindowPacket>.Acquire();
			packet.Prepare(name, serverUid);
			this.Conn.SendSinglePacket(packet);
		}

		public void Write(GameUid serverUid, string str) {
			SendStringPacket packet = Pool<SendStringPacket>.Acquire();
			packet.Prepare(serverUid, str);
			this.Conn.SendSinglePacket(packet);
		}

		public void WriteLine(GameUid serverUid, string str) {
			SendStringLinePacket packet = Pool<SendStringLinePacket>.Acquire();
			packet.Prepare(serverUid, str);
			this.Conn.SendSinglePacket(packet);
		}

		public void SendLoginFailedAndClose(string reason) {
			LoginFailedPacket packet = Pool<LoginFailedPacket>.Acquire();
			packet.Prepare(reason);
			this.conn.SendSinglePacket(packet);

			this.conn.Core.WaitForAllSent();
			this.conn.Close(reason);
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return true;
			}
		}


		public void On_PacketBeingHandled(IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> packet) {

		}
	}

	//just a renamed int
	public enum ConsoleId {
		FakeConsole = -1
	}
}
