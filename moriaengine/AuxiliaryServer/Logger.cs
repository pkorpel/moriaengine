using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace SteamEngine.AuxiliaryServer {
	public class Logger : Common.Logger {

		[SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "SteamEngine.AuxiliaryServer.Logger")]
		public static void Init() {
			new Logger();

			OpenFile();

			OnConsoleWrite += ConsoleServer.ConsoleServer.WriteAsAux;
			OnConsoleWriteLine += ConsoleServer.ConsoleServer.WriteLineAsAux;
		}


		protected override string GetFilepath() {
			//DateTime.Now.GetDateTimeFormats()[4]
			DateTime dtnow = DateTime.Now;
			CultureInfo ci = CultureInfo.InvariantCulture;
			string filename = string.Concat("SteamEngine.AuxiliaryServer.",
				dtnow.Year.ToString("0000", ci), "-", dtnow.Month.ToString("00", ci), "-", dtnow.Day.ToString("00", ci), ".log");
			return Path.Combine(Settings.LogPath, filename);
		}
	}
}
