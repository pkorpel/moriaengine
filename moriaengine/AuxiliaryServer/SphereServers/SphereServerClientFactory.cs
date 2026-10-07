using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SteamEngine.AuxiliaryServer.SphereServers {

	//simplified version of "Communication" framework, only communicates in cleartext - telnet style
	public static class SphereServerClientFactory {
		public static void Init() {
			foreach (IGameServerSetup setup in Settings.KnownGameServersList) {
				SphereServerSetup sphereSetup = setup as SphereServerSetup;
				if (sphereSetup != null) {
					Connect(sphereSetup, 0);
				}
			}
		}

		public static void Connect(SphereServerSetup setup, int ms) {
			IPEndPoint endpoint = new IPEndPoint(
				//Dns.GetHostAddresses("server.moria.cz")[0], 2593
				IPAddress.Loopback, setup.Port
				);

			Socket socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			ScheduleConnect(new object[] { socket, endpoint, setup }, ms);			
		}

		private static void ScheduleConnect(object state, int ms) {
			//Timer t = 
			new Timer(ScheduledBeginConnect, state, ms, Timeout.Infinite);
		}

		private static void ScheduledBeginConnect(object state) {
			Console.WriteLine("ScheduledBeginConnect in");
			try {
				object[] arr = (object[]) state;
				Socket socket = (Socket) arr[0];
				IPEndPoint endpoint = (IPEndPoint) arr[1];

				socket.BeginConnect(endpoint, BeginConnectCallBack, state);
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("ScheduledBeginConnect out");
		}

		private static void BeginConnectCallBack(IAsyncResult result) {
			object[] arr = (object[]) result.AsyncState;
			Socket socket = (Socket) arr[0];
			//IPEndPoint endpoint = (IPEndPoint) arr[1];
			SphereServerSetup setup = (SphereServerSetup) arr[2];

			try {
				socket.EndConnect(result);
			} catch (Exception e) {
				Console.WriteLine("Connecting to sphere at '" + setup.RamdiscIniPath + "' failed:" + e.Message);
				Common.Logger.WriteDebug(e);
				ScheduleConnect(arr, 5000);
				return;
			}

			//SphereServerConnection newConn = 
			new SphereServerConnection(socket, setup);
		}
	}
}
