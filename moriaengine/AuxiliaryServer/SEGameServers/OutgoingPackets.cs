using System.Diagnostics.CodeAnalysis;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.SEGameServers {
	public class RequestSendingLogStringsPacket : OutgoingPacket {
		bool sendLogStrings;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sendLogStrings")]
		public void Prepare(bool sendLogStrings) {
			this.sendLogStrings = sendLogStrings;
		}

		public override byte Id {
			get {
				return 0x01;
			}
		}

		protected override void Write() {
			this.EncodeBool(this.sendLogStrings);
		}
	}

	public class ConsoleLoginRequestPacket : OutgoingPacket {
		int consoleId;
		string accName, password;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "accName"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "password"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "consoleId")]
		public void Prepare(ConsoleId consoleId, string accName, string password) {
			this.consoleId = (int) consoleId;
			this.accName = accName;
			this.password = password;
		}

		public override byte Id {
			get {
				return 0x02;
			}
		}

		protected override void Write() {
			this.EncodeInt(this.consoleId);
			this.EncodeUTF8String(this.accName);
			this.EncodeUTF8String(this.password);
		}
	}

	public class ConsoleCommandLinePacket : OutgoingPacket {
		int consoleId;
		string accName, password;
		private string command;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "command"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "password"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "consoleId"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "accName")]
		public void Prepare(ConsoleId consoleId, string accName, string password, string command) {
			this.consoleId = (int) consoleId;
			this.accName = accName;
			this.password = password;
			this.command = command;
		}

		public override byte Id {
			get { return 3; }
		}

		protected override void Write() {
			this.EncodeInt(this.consoleId);
			this.EncodeUTF8String(this.accName);
			this.EncodeUTF8String(this.password);
			this.EncodeUTF8String(this.command);
		}
	}
}
