using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	public class ConsoleServerProtocol : IProtocol<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ConsoleServerProtocol instance = new ConsoleServerProtocol();

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> GetPacketImplementation(byte id, TcpConnection<ConsoleClient> conn, ConsoleClient state, out bool discardAfterReading) {
			discardAfterReading = false;

			switch (id) {
				case 0:
					return Pool<RequestLoginPacket>.Acquire();
				case 1:
					return Pool<RequestServersToStartPacket>.Acquire();
				case 2:
					return Pool<RequestStartGameServer>.Acquire();
				case 3:
					return Pool<CommandLinePacket>.Acquire();
			}
			return null;
		}
	}


	public abstract class ConsoleIncomingPacket : IncomingPacket<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint> {

	}

	public class RequestLoginPacket : ConsoleIncomingPacket {
		private string accName;
		private string password;

		protected override ReadPacketResult Read() {
			this.accName = this.DecodeUTF8String();
			this.password = this.DecodeUTF8String();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			state.SetLoginData(this.accName, this.password);

			bool failed = false;
			if (Settings.CheckUser(this.accName, this.password)) {
				state.SetLoggedInToAux(true);
			} else {
				failed = true;
			}

			if (GameServersManager.AllIdentifiedGameServers.Count > 0) {
				state.TryLoginToGameServers();
			} else if (failed) {
				state.SendLoginFailedAndClose("The username '"+this.accName+"' either isn't cached in the AuxServer or the password is wrong.");
			}
		}
	}

	public class RequestServersToStartPacket : ConsoleIncomingPacket {
		protected override ReadPacketResult Read() {
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			List<int> runningServerIniIDs = new List<int>();
			foreach (GameServer gsc in GameServersManager.AllIdentifiedGameServers) {
				runningServerIniIDs.Add(gsc.Setup.IniID);
			}

			SendServersToStartPacket packet = Pool<SendServersToStartPacket>.Acquire();
			packet.Prepare(Settings.KnownGameServersList, runningServerIniIDs);
			conn.SendSinglePacket(packet);
		}
	}

	public class RequestStartGameServer : ConsoleIncomingPacket {
		private byte iniID;
		private BuildType build;

		protected override ReadPacketResult Read() {
			this.iniID = this.DecodeByte();
			this.build = (BuildType) this.DecodeByte();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			GameServer cli = GameServersManager.GetInstanceByIniID(this.iniID);
			if (cli != null) {
				state.WriteLine(GameUid.AuxServer, "Server already online, ignoring start command.");
				//server online, we do nothing
			} else {
				IGameServerSetup sett = Settings.KnownGameServersList[this.iniID];
				Sanity.IfTrueThrow((this.iniID != sett.IniID), "Server ini ID number is different from it's index in list");

				sett.StartGameServerProcess(this.build);
			}
		}
	}

	public class CommandLinePacket : ConsoleIncomingPacket {
		private int id;
		private string command;

		protected override ReadPacketResult Read() {
			this.id = this.DecodeInt();
			this.command = this.DecodeUTF8String();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<ConsoleClient> conn, ConsoleClient state) {
			if (this.id == 0) {
				if (state.IsLoggedInAux) {
					Commands.HandleCommand(conn, state, this.command);
					return;
				}
			} else {
				GameServer cli = GameServersManager.GetInstanceByUid((GameUid) this.id);
				if (cli != null) {					
					if (GameServersManager.IsLoggedIn(state, cli)) {
						cli.SendCommand(state, this.command);
						return;
					}
				}
			}

			state.WriteLine(GameUid.AuxServer, "Invalid (not implemented/not logged in) id to command: " + this.id);
		}
	}
}