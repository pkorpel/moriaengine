using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.RemoteConsole {

	public class RequestLoginPacket : OutgoingPacket {
		private string accName;
		private string password;

		public void Prepare(string accName, string password) {
			this.accName = accName;
			this.password = password;
		}

		public override byte Id {
			get { return 0; }
		}

		protected override void Write() {
			this.EncodeUTF8String(this.accName);
			this.EncodeUTF8String(this.password);
		}
	}


	public class RequestServersToStartPacket : OutgoingPacket {
		public static readonly PacketGroup group;

		static RequestServersToStartPacket() {
			group = PacketGroup.CreateFreePG();
			group.AddPacket(new RequestServersToStartPacket());
		}

		public override byte Id {
			get { return 1; }
		}

		protected override void Write() {
		}
	}

	public class RequestStartGameServer : OutgoingPacket {
		private byte serverNum;
		private BuildType build;

		public void Prepare(int serverNum, BuildType build) {
			this.serverNum = (byte) serverNum;
			this.build = build;
		}

		public override byte Id {
			get { return 2; }
		}

		protected override void Write() {
			this.EncodeByte(this.serverNum);
			this.EncodeByte((byte) this.build);
		}
	}

	public class CommandLinePacket : OutgoingPacket {
		private int id;
		private string command;

		public void Prepare(GameUid id, string command) {
			this.id = (int) id;
			this.command = command;
		}

		public override byte Id {
			get { return 3; }
		}

		protected override void Write() {
			this.EncodeInt(this.id);
			this.EncodeUTF8String(this.command);
		}
	}
}