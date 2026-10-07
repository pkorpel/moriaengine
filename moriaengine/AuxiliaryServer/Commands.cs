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
					CmdRestart();
					return;
				case "svnupdate":
					VersionControl.SvnUpdateProject(".");
					foreach (IGameServerSetup game in Settings.KnownGameServersList) {
						game.SvnUpdate(state);
					}
					return;
				case "svncleanup":
					VersionControl.SvnCleanUpProject(".");
					foreach (IGameServerSetup game in Settings.KnownGameServersList) {
						game.SvnCleanup(state);
					}
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
			state.WriteLine(GameUid.AuxServer, "Available commands:"
				+ "restart" + Environment.NewLine
				+ "svnupdate" + Environment.NewLine
				+ "svncleanup" + Environment.NewLine
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

		public static void CmdRestart() {
			new MsBuildProjectReStarter();
		}

		private class MsBuildProjectReStarter : AuxServMsBuildProjectStarter {
			internal MsBuildProjectReStarter()
				: base(BuildType.Release, ".", "buildRestarter") {
			}

			public override void StartProcess(string file) {
				ProcessStartInfo psi = new ProcessStartInfo(file);
#if DEBUG
				psi.Arguments = string.Concat("\"", Process.GetCurrentProcess().MainModule.FileName, "\" \"", "Debug_AuxiliaryServer.bat", "\"");
#elif SANE
				psi.Arguments = string.Concat("\"", Process.GetCurrentProcess().MainModule.FileName, "\" \"", "Sane_AuxiliaryServer.bat", "\"");
#else
#error Optimized_AuxiliaryServer.bat not defined (?)
#endif
				Process.Start(psi);

				MainClass.CommandExit();
			}
		}
	}

	//Nant logger class and a helper threading class combined
}
