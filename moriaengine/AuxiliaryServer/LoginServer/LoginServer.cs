
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	[SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
	public sealed class LoginServer : TcpServer<LoginClient> {
		private LoginServer()
			: base(LoginServerProtocol.instance, MainClass.GlobalLock, MainClass.ExitSignalToken) {

		}

		private static LoginServer instance = new LoginServer();

		internal static void Init() {
			instance.Bind(Settings.LoginServerEndpoint);
		}

		internal static void Exit() {
			instance.Dispose();
		}
	}
}
