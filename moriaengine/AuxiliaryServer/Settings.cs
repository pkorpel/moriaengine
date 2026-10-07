using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {

	public interface IGameServerSetup {
		int IniID {
			get;
		}
		string IniPath {
			get;
		}
		string Name {
			get;
		}
		int Port {
			get;
		}
		void InternalSetIniID(int iniID);

		void StartGameServerProcess(BuildType build);

		void SvnUpdate(ConsoleClient console);

		void SvnCleanup(ConsoleClient console);
	}

	public static class Settings {
		private static readonly string logPath;
		private static readonly int timeZone;
		private static readonly IPEndPoint loginServerEndpoint;
		private static readonly IPEndPoint consoleServerEndpoint;

		private static readonly HashSet<IGameServerSetup> knownGameServersSet;
		private static readonly List<IGameServerSetup> knownGameServersList;
		private static readonly ReadOnlyCollection<IGameServerSetup> knownGameServersListWrapper;

		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const string iniFileName = "steamaux.ini";

		public static int TimeZone {
			get { return timeZone; }
		}

		public static string LogPath {
			get { return logPath; }
		}

		public static ReadOnlyCollection<IGameServerSetup> KnownGameServersList {
			get {
				return knownGameServersListWrapper;
			}
		}

		public static IPEndPoint LoginServerEndpoint {
			get { return loginServerEndpoint; }
		}

		public static IPEndPoint ConsoleServerEndpoint {
			get { return consoleServerEndpoint; }
		}

		static Settings() {
			knownGameServersSet = new HashSet<IGameServerSetup>();
			knownGameServersList = new List<IGameServerSetup>();
			knownGameServersListWrapper = new ReadOnlyCollection<IGameServerSetup>(knownGameServersList);

			IniFile ini = new IniFile(iniFileName);

			IniFileSection files = ini.GetNewOrParsedSection("Files");

			logPath = Path.GetFullPath(files.GetValue("logPath", "logs", "Path to the log files"));


			IniFileSection loginServer = ini.GetNewOrParsedSection("LoginServer");

			timeZone = loginServer.GetValue<sbyte>("timeZone", 5, "What time-zone you're in. 0 is GMT, 5 is EST, etc.");

			loginServerEndpoint = new IPEndPoint(IPAddress.Any,
				loginServer.GetValue("port", 2593, "The port to listen on for game client connections"));


			IniFileSection consoleServer = ini.GetNewOrParsedSection("ConsoleServer");

			consoleServerEndpoint = new IPEndPoint(IPAddress.Any,
				consoleServer.GetValue("port", 2594, "The port to listen on for remote console connections"));


			//SE gameservers
			foreach (IniFileSection section in ini.GetSections("GameServer")) {
				TryAddKnownGameServer(new SEGameServerSetup(section));
			}

			if (knownGameServersSet.Count == 0) {
				TryAddKnownGameServer(new SEGameServerSetup(ini.GetNewOrParsedSection("GameServer")));
			}

			//Sphereservers
			foreach (IniFileSection section in ini.GetSections("SphereServer")) {
				TryAddKnownGameServer(new SphereServerSetup(section));
			}

			knownGameServersList.Sort(delegate(IGameServerSetup a, IGameServerSetup b) {
				return a.IniID.CompareTo(b.IniID);
			});

			for (int i = 0, n = knownGameServersList.Count; i < n; i++) {
				knownGameServersList[i].InternalSetIniID(i);
			}

			ini.WriteToFile();

			Console.WriteLine(iniFileName + " loaded and written.");
		}

		public static bool TryAddKnownGameServer(IGameServerSetup gsig) {
			int prevCount = knownGameServersSet.Count;
			knownGameServersSet.Add(gsig);
			if (knownGameServersSet.Count > prevCount) {
				knownGameServersList.Add(gsig);
				return true;
			}
			return false;
		}

		//SE server specific - sphere doesn't "call" by itself :)
		public static SEGameServerSetup RememberGameServer(string iniPath) {
			foreach (IGameServerSetup game in knownGameServersSet) {
				if (string.Equals(game.IniPath, Path.GetFullPath(iniPath), StringComparison.OrdinalIgnoreCase)) {
					Sanity.IfTrueThrow(game.GetType() != typeof(SEGameServerSetup), "SE gameserver running on a path that belongs to a sphereserver, according to steamaux.ini. Wtf?");
					return (SEGameServerSetup) game;
				}
			}

			IniFile ini = new IniFile(iniFileName);
			IniFileSection section = ini.GetNewSection("GameServer");

			SEGameServerSetup newGameServer = new SEGameServerSetup(iniPath);
			if (TryAddKnownGameServer(newGameServer)) {
				newGameServer.WriteToIniSection(section);
			}

			ini.WriteToFile();

			return newGameServer;
		}

		public static void ForgetUser(string user) {
			IniFile ini = new IniFile(iniFileName);
			IniFileSection usersSection = ini.GetNewOrParsedSection("Users");
			usersSection.RemoveValue(user);
			ini.WriteToFile();
		}

		public static void RememberUser(string user, string password) {
			IniFile ini = new IniFile(iniFileName);
			IniFileSection usersSection = ini.GetNewOrParsedSection("Users");
			usersSection.SetValue(user, password, null);
			ini.WriteToFile();
		}

		public static bool CheckUser(string user, string password) {
			IniFile ini = new IniFile(iniFileName);
			IniFileSection usersSection = ini.GetNewOrParsedSection("Users");
			string passToCompare;
			if (usersSection.TryGetValue(user, out passToCompare)) {
				if (string.Equals(password, passToCompare, StringComparison.Ordinal)) {
					return true;
				}
			}
			return false;
		}

		internal static void Init() {

		}
	}
}
