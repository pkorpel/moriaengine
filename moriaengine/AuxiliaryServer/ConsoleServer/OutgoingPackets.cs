using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {

	public class RequestOpenCommandWindowPacket : OutgoingPacket {
		string name;
		int cmdWinUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "name"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "cmdWinUid")]
		public void Prepare(string name, GameUid cmdWinUid) {
			this.name = name;
			this.cmdWinUid = (int) cmdWinUid;
		}

		public override byte Id {
			get { return 1; }
		}

		protected override void Write() {
			this.EncodeInt(this.cmdWinUid);
			this.EncodeUTF8String(this.name);
		}
	}

	public class RequestEnableCommandLinePacket : OutgoingPacket {
		int uid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid")]
		public void Prepare(GameUid uid) {
			this.uid = (int) uid;
		}

		public override byte Id {
			get { return 3; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
		}
	}

	public class RequestCloseCommandWindowPacket : OutgoingPacket {
		int uid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid")]
		public void Prepare(GameUid uid) {
			this.uid = (int) uid;
		}

		public override byte Id {
			get { return 2; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
		}
	}

	public class SendStringPacket : OutgoingPacket {
		string str;
		int uid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "str"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid")]
		public void Prepare(GameUid uid, string str) {
			this.uid = (int) uid;
			this.str = str;
		}

		public override byte Id {
			get { return 4; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
			this.EncodeUTF8String(this.str);
		}
	}

	public class SendStringLinePacket : OutgoingPacket {
		string str;
		int uid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "uid"), 
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "str")]
		public void Prepare(GameUid uid, string str) {
			this.uid = (int) uid;
			this.str = str;
		}

		public override byte Id {
			get { return 5; }
		}

		protected override void Write() {
			this.EncodeInt(this.uid);
			this.EncodeUTF8String(this.str);
		}
	}

	public class SendServersToStartPacket : OutgoingPacket {
		private int count;
		private List<int> iniIDs = new List<int>();
		private List<string> iniPaths = new List<string>();
		private List<string> names = new List<string>();
		private List<ushort> ports = new List<ushort>();
		private List<bool> runnings = new List<bool>();

		public void Prepare(IList<IGameServerSetup> servers, IList<int> runningServerIniIDs) {
			this.iniIDs.Clear();
			this.iniPaths.Clear();
			this.names.Clear();
			this.ports.Clear();
			this.runnings.Clear();

			this.count = servers.Count;
			foreach (IGameServerSetup gsis in servers) {
				this.iniIDs.Add(gsis.IniID);
				this.iniPaths.Add(gsis.IniPath);
				this.names.Add(gsis.Name);
				this.ports.Add((ushort) gsis.Port);
				this.runnings.Add(runningServerIniIDs.Contains(gsis.IniID));
			}
		}

		public override byte Id {
			get { return 6; }
		}

		protected override void Write() {
			this.EncodeInt(this.count);
			for (int i = 0; i < this.count; i++) {
				this.EncodeInt(this.iniIDs[i]);
				this.EncodeUTF8String(this.iniPaths[i]);
				this.EncodeUTF8String(this.names[i]);
				this.EncodeUShort(this.ports[i]);
				this.EncodeBool(this.runnings[i]);
			}
		}
	}

	public class LoginFailedPacket : OutgoingPacket {
		string reason;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "reason")]
		public void Prepare(string reason) {
			this.reason = reason;
		}

		public override byte Id {
			get { return 7; }
		}

		protected override void Write() {
			this.EncodeUTF8String(this.reason);
		}
	}
}
