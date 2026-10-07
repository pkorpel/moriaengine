using System;
using System.Reflection;
using System.Threading;
using SteamEngine.AuxiliaryServer.LoginServer;
using SteamEngine.AuxiliaryServer.SEGameServers;
using SteamEngine.AuxiliaryServer.SphereServers;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class MainClass {
		//public static ConsoleServer server = new ConsoleServer();

		private static readonly object globalLock = new object();

		private static readonly CancellationTokenSource exitTokenSource = new CancellationTokenSource();

		public static object GlobalLock {
			get { return globalLock; }
		}

		public static CancellationToken ExitSignalToken {
			get { return exitTokenSource.Token; }
		}

		static void Main() {
			//name the console window for better recognizability
			Console.Title = "SE Auxiliary Server - " + Assembly.GetExecutingAssembly().Location;

			Tools.ExitBinDirectory();

			try {
				Init();

				Thread t = new Thread(delegate() {
					Console.ReadLine();
					exitTokenSource.Cancel();
				});
				t.IsBackground = true;
				t.Start();

				exitTokenSource.Token.WaitHandle.WaitOne();

			} catch (Exception e) {
				Common.Logger.WriteFatal(e);
			} finally {
				Exit();
			}
		}

		private static void Init() {
			Logger.Init();
			Settings.Init();
			ServerUtils.Init();

			LoginServer.LoginServer.Init();
			ConsoleServer.ConsoleServer.Init();
			SEGameServerServer.Init();
			SphereServerClientFactory.Init();
		}

		private static void Exit() {
			LoginServer.LoginServer.Exit();
			ConsoleServer.ConsoleServer.Exit();
			SEGameServerServer.Exit();
		}

		internal static void CommandExit() {
			exitTokenSource.Cancel();
		}
	}
}
