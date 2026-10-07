using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public abstract class GameServer {
		protected GameUid uid;


		public GameUid ServerUid {
			get {
				return this.uid;
			}
		}

		public abstract IGameServerSetup Setup {
			get;
		}

		public abstract bool StartupFinished {
			get;
		}

		public abstract void SendCommand(ConsoleClient console, string cmd);

		public abstract void SendConsoleLogin(ConsoleId consoleId, string accName, string accPassword);
	}
}
