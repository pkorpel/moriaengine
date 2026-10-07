using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.LoginServer {

	public class ServersListPacket : OutgoingPacket {
		private byte[] ip;
		private List<string> names = new List<string>();
		private sbyte timezone;

		public override byte Id {
			get { return 0xA8; }
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "ip")]
		public void Prepare(byte[] ip) {
			this.names.Clear();
			foreach (GameServer server in GameServersManager.AllIdentifiedGameServers) {
				if (server.StartupFinished) {
					this.names.Add(server.Setup.Name);
				}
			}
			this.ip = ip;
			this.timezone = (sbyte) Settings.TimeZone;
		}

		protected override void Write() {
			int serverCount = this.names.Count;
			this.EncodeUShort((ushort) ((serverCount * 40) + 6)); //length

			this.EncodeByte(0x5d);	//0x13; //unknown //0x5d on RunUo

			this.EncodeUShort((ushort) serverCount);

			for (int i = 0; i < serverCount; i++) {
				this.EncodeUShort((ushort) i);
				this.EncodeASCIIString(this.names[i], 32);
				this.EncodeByte(0);
				this.EncodeSByte(this.timezone);
				this.EncodeBytesReversed(this.ip);
			}
		}
	}

	public class LoginToServerPacket : OutgoingPacket {
		private byte[] ip;
		private ushort port;

		public override byte Id {
			get { return 0x8c; }
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "ip"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "port")]
		public void Prepare(byte[] ip, int port) {
			Common.Logger.WriteDebug("Sending shard IP: " + new IPEndPoint(new IPAddress(ip), port));
			this.ip = ip;
			this.port = (ushort) port;
		}

		protected override void Write() {
			this.EncodeBytes(this.ip);
			this.EncodeUShort(this.port);
			this.EncodeZeros(4); //new key. could be random, but who cares...
		}
	}

	//public class ConsoleServerPacketGroup : PacketGroup {
	//    public ConsoleServerPacketGroup() {
	//        base.SetType(PacketGroupType.SingleUse);
	//    }
	//}
}
