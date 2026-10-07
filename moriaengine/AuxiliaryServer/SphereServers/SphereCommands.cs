using System;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.SphereServers {
	public static class SphereCommands {
		public static void HandleCommand(ConsoleClient console, SphereServerClient sphere, string cmd) {
			string[] split = cmd.Split(Tools.whitespaceChars);

			switch (split[0].ToLowerInvariant()) {
				case "filter":
					FlipFilter(console, sphere);
					return;
				case "help":
					DisplayHelp(console, sphere);
					return;
				case "exitlater":
					ExitLater(console, sphere, split);
					return;
			}

			console.WriteLine(sphere.ServerUid, "Unknown command '" + cmd + "'.");
		}

		private static void ExitLater(ConsoleClient console, SphereServerClient sphere, string[] split) {
			double minutes = 10;
			if (split.Length > 1) {
				minutes = ConvertTools.ParseDouble(split[1]);
			}
			sphere.ExitLater(console, TimeSpan.FromMinutes(minutes));
		}

		private static void DisplayHelp(ConsoleClient console, SphereServerClient sphere) {
			console.WriteLine(sphere.ServerUid, "Available commands:"
				+ "filter" + Environment.NewLine
				+ "help");
		}

		private static void FlipFilter(ConsoleClient console, SphereServerClient sphere) {
			if (console.filteredGameServers.Contains(sphere.ServerUid)) {
				console.filteredGameServers.Remove(sphere.ServerUid);
				console.WriteLine(sphere.ServerUid, "Filter disabled");
			} else {
				console.filteredGameServers.Add(sphere.ServerUid);
				console.WriteLine(sphere.ServerUid, "Filter enabled");
			}
		}
	}
}
