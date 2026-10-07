using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.AuxiliaryServer.SphereServers;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {

	public class SphereServerSetup : IGameServerSetup {
		public const string sphereExeName = "SphereSvr.exe";

		private int iniID;
		private readonly string ramdiscIniPath;
		private readonly string hardDiscIniPath;

		private readonly string name;
		private readonly int port;

		private readonly int exitLaterParam;
		private readonly DateTime exitLaterDailySchedule;

		private readonly string adminAccount;
		private readonly string adminPassword;


		//internal SphereServerSetup(string iniPath) {
		//    this.iniID = Settings.KnownGameServersList.Count;
		//    this.hardDiscIniPath = Path.GetFullPath(iniPath);

		//    ReadSphereIni(this.hardDiscIniPath, out this.name, out this.port);
		//}

		internal SphereServerSetup(IniFileSection section) {
			this.iniID = section.GetValue("number", 0, "Number to order the servers in shard list. Should be unique, starting with 0.");
			this.hardDiscIniPath = Path.GetFullPath(section.GetValue("hardDiscIniPath", Path.GetFullPath("."), "path to sphere.ini of this instance"));
			this.ramdiscIniPath = Path.GetFullPath(section.GetValue("ramdiscIniPath", this.hardDiscIniPath, "path to sphere.ini of this instance on ramdisc. Optional."));

			this.exitLaterParam = section.GetValue("exitLaterParam", -1, "param of the automatic daily exitlater - if negative, disable the function.");
			this.exitLaterDailySchedule = section.GetValue("exitLaterDailySchedule", DateTime.MinValue.AddHours(4), "time to daily exitlater sphereserver.");

			this.adminAccount = section.GetValue("adminAccount", "administrator", "Sphere account to login to via it's telnet capability.");
			this.adminPassword = section.GetValue("adminPassword", "123456", "Sphere account password.");

			ReadSphereIni(this.hardDiscIniPath, out this.name, out this.port);

			this.StartExitlaterScheduler();
		}

		internal void WriteToIniSection(IniFileSection section) {
			section.SetValue("number", this.iniID, "Number to order the servers in shard list. Should be unique, starting with 0.");
			section.SetValue("hardDiscIniPath", this.hardDiscIniPath, "path to sphere.ini of this instance");

			section.SetValue("exitLaterParam", this.exitLaterParam, "param of the automatic daily exitlater - if negative, disable the function");
			section.SetValue("exitLaterDailySchedule", this.exitLaterDailySchedule, "time to daily exitlater sphereserver");

			section.GetValue("adminAccount", this.adminAccount, "Sphere account to login to via it's telnet capability.");
			section.GetValue("adminPassword", this.adminPassword, "Sphere account password.");
		}

		private static void ReadSphereIni(string iniPath, out string name, out int port) {
			IniFile gameIni;

			iniPath = Path.Combine(iniPath, "sphere.ini");
			if (File.Exists(iniPath)) {
				gameIni = new IniFile(iniPath);
			} else {
				throw new SEException("Can't find sphere.ini on the path " + iniPath + ". Restore it or delete the entry from steamaux.ini.");
			}

			IniFileSection sphereSection = gameIni.GetSection("SPHERE");
			name = sphereSection.GetValue<string>("SERVNAME");
			port = sphereSection.GetValue<int>("SERVPORT");
		}

		private static string ReadScriptsPathFromSphereIni(string iniPath) {
			IniFile gameIni = new IniFile(Path.Combine(iniPath, "sphere.ini"));

			string scriptsPath = gameIni.GetSection("SPHERE").GetValue<string>("ScpFiles");

			if (Path.IsPathRooted(scriptsPath)) {
				return scriptsPath;
			}
			return Path.Combine(iniPath, scriptsPath);
		}

		public int IniID {
			get {
				return this.iniID;
			}
		}

		void IGameServerSetup.InternalSetIniID(int iniID) {
			this.iniID = iniID;
		}

		public string IniPath {
			get {
				return this.hardDiscIniPath;
			}
		}

		public string Name {
			get {
				return this.name;
			}
		}

		public int Port {
			get {
				return this.port;
			}
		}

		public string RamdiscIniPath {
			get {
				return this.hardDiscIniPath;
			}
		}

		public string HardDiscIniPath {
			get {
				return this.hardDiscIniPath;
			}
		}

		public string RamdiscScriptsPath {
			get {
				return ReadScriptsPathFromSphereIni(this.ramdiscIniPath);
			}
		}

		public string HardDiscScriptsPath {
			get {
				return ReadScriptsPathFromSphereIni(this.hardDiscIniPath);
			}
		}

		public string AdminAccount {
			get {
				return this.adminAccount;
			}
		}

		public string AdminPassword {
			get {
				return this.adminPassword;
			}
		} 

		//if negative, disable
		public int ExitLaterParam {
			get {
				return this.exitLaterParam;
			}
		}

		//if negative, disable
		public DateTime ExitLaterDailySchedule {
			get {
				return this.exitLaterDailySchedule;
			}
		}

		public override bool Equals(object obj) {
			SphereServerSetup gsis = obj as SphereServerSetup;
			if (gsis != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(this.IniPath, gsis.IniPath)) {
					return true;
				}
			}
			return false;
		}

		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(this.hardDiscIniPath);
		}

		public void StartGameServerProcess(BuildType build) {
			string path = Path.Combine(this.ramdiscIniPath, sphereExeName);
			Console.WriteLine("Starting Sphereserver: " + this.IniPath);
			Process.Start(path);
		}

		public void SvnUpdate(ConsoleClient console) {
			GameServer sphere = GameServersManager.GetInstanceByIniID(this.iniID);
			if (sphere != null) {
				sphere.SendCommand(console, "r");
			}

			VersionControl.SvnUpdateProject(this.HardDiscScriptsPath);
			if (!StringComparer.OrdinalIgnoreCase.Equals(this.HardDiscIniPath, this.RamdiscIniPath)) {
				VersionControl.SvnUpdateProject(this.RamdiscScriptsPath);
			}

			if (sphere != null) {
				sphere.SendCommand(console, "r");
			}
		}

		public void SvnCleanup(ConsoleClient console) {
			VersionControl.SvnCleanUpProject(this.HardDiscScriptsPath);
			if (!StringComparer.OrdinalIgnoreCase.Equals(this.HardDiscIniPath, this.RamdiscIniPath)) {
				VersionControl.SvnCleanUpProject(this.RamdiscScriptsPath);
			}
		}

		private void StartExitlaterScheduler() {
			if (this.exitLaterParam >= 0) {
				DateTime now = DateTime.Now;
				DateTime schedule = now.Date + this.exitLaterDailySchedule.TimeOfDay;
				if (now > schedule) { //too late today
					schedule = schedule.AddDays(1); //schedule for tomorrow
				}
				TimeSpan span = schedule - now;

				Console.WriteLine(string.Concat(
					"Automatic exitlater scheduled for sphereserver at ", this.HardDiscIniPath, " for ",
					this.exitLaterDailySchedule.ToShortTimeString(),
					" which is in "+span));

				new Timer(this.ScheduledExitLater, "", 1000, //(int) (span.TotalMilliseconds + 1), //+1 ms so it should't need to reschedule, just a failsafe
					Timeout.Infinite
					//1000
					);
			}
		}

		private void ScheduledExitLater(object ignored) {
			Console.WriteLine("ScheduledExitLater in");
			try {
				DateTime now = DateTime.Now;
				DateTime schedule = now.Date + this.exitLaterDailySchedule.TimeOfDay;
				if (now > schedule) { //so it doesn't happen too early today, reschedules if needed
					Console.WriteLine(string.Concat(
						"Invoking exitlater ", this.exitLaterParam.ToString(), " for sphereserver at ", this.HardDiscIniPath));

					SphereServerClient sphere = GameServersManager.GetInstanceByIniID(this.iniID) as SphereServerClient;
					if (sphere != null) {
						sphere.ExitLater(null, TimeSpan.FromMinutes(this.exitLaterParam));
					} else {
						Console.WriteLine(string.Concat(
							"Sphereserver at ", this.HardDiscIniPath, " offline, exitlater not invoked."));
					}
				}

				this.StartExitlaterScheduler();
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("ScheduledExitLater out");
		}
	}
}
