using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.AuxiliaryServer.SEGameServers;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer.ConsoleServer {
	[SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
	public class ConsoleServer : TcpServer<ConsoleClient> {
		public ConsoleServer()
			: base(ConsoleServerProtocol.instance, MainClass.GlobalLock, MainClass.ExitSignalToken) {
		}

		private static ConsoleServer instance = new ConsoleServer();

		private static Dictionary<ConsoleId, ConsoleClient> consoles = new Dictionary<ConsoleId, ConsoleClient>();


		internal static void Init() {
			instance.Bind(Settings.ConsoleServerEndpoint);
		}

		internal static void Exit() {
			instance.Dispose();
		}

		internal static void AddConnection(ConsoleClient client) {
			consoles.Add(client.ConsoleId, client);

			if (consoles.Count == 1) {
				SEGameServerServer.StartSendingLogStr();
			}
		}

		internal static void RemoveConnection(ConsoleClient client) {
			consoles.Remove(client.ConsoleId);

			if (consoles.Count == 0) {
				SEGameServerServer.StopSendingLogStr();

				//memory cleanup
				//PoolBase.ClearAll();
				GC.Collect();

			}
		}

		public static ConsoleClient GetClientByUid(ConsoleId consoleId) {
			ConsoleClient retVal;
			if (consoles.TryGetValue(consoleId, out retVal)) {
				return retVal;
			}
			return null;
		}

		public static void WriteLineAsAux(string msg) {
			foreach (ConsoleClient cc in consoles.Values) {
				if (cc.IsLoggedInAux) {
					cc.WriteLine(GameUid.AuxServer, msg);
				}
			}
		}

		public static void WriteAsAux(string msg) {
			foreach (ConsoleClient cc in consoles.Values) {
				if (cc.IsLoggedInAux) {
					cc.Write(GameUid.AuxServer, msg);
				}
			}
		}

		public static IEnumerable<ConsoleClient> AllConsoles {
			get {
				return consoles.Values;
			}
		}

		public static int AllConsolesCount {
			get {
				return consoles.Count;
			}
		}
	}
}