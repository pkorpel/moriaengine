using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;

namespace SteamEngine.AuxiliaryServer {
	public static class Commands {
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"),
		SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "conn"),
		SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "cmd")]
		public static void HandleCommand(TcpConnection<ConsoleClient> conn, ConsoleClient state, string cmd) {
			cmd = cmd.ToLowerInvariant();
			switch (cmd) {
				case "restart":
					CmdRestart(state);
					return;
				case "help":
					DisplayHelp(state);
					return;
				case "processes":
					DisplayProcesses(state);
					return;
			}

			state.WriteLine(GameUid.AuxServer, "Unknown command '" + cmd + "'.");
		}

		private static void DisplayHelp(ConsoleClient state) {
			state.WriteLine(GameUid.AuxServer, "Available commands:" + Environment.NewLine
				+ "restart (says how to restart)" + Environment.NewLine
				+ "processes" + Environment.NewLine
				+ "help");
		}

		private static void DisplayProcesses(ConsoleClient state) {
			StringBuilder message = new StringBuilder("Relevant running processes on ").AppendLine(Environment.MachineName);
			foreach (Process prc in Process.GetProcesses()) {
				try {
					string file = prc.MainModule.FileName;

					if (file.Contains(SphereServerSetup.sphereExeName) ||
							file.ToLowerInvariant().Contains("steamengine")) {

						message.Append(file)
							.Append(" - running since ").Append(prc.StartTime.ToString(CultureInfo.InvariantCulture))
							.Append(", PID ").AppendLine(prc.Id.ToString(CultureInfo.InvariantCulture));
					}
				} catch { }
			}

			state.WriteLine(GameUid.AuxServer, message.ToString());
		}

		//#1185 - "restart" built a restarter (a "buildRestarter" project and Windows .bat files that don't exist) and never
		//started it, so it never did anything; its #error kept Release from building. The server is restarted from outside.
		public static void CmdRestart(ConsoleClient state) {
			state.WriteLine(GameUid.AuxServer, "'restart' isn't available here: stop and start the servers with scripts/run-local.sh, or restart the container.");
		}
	}

	//Nant logger class and a helper threading class combined
}
