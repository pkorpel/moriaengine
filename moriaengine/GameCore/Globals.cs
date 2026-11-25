/*
	This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SharpSvn;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Persistence;
#if MSWIN
using Microsoft.Win32;	//for RegistryKey
#endif

namespace SteamEngine {
	public interface ISrc {
		byte Plevel { get; }
		byte MaxPlevel { get; }
		void WriteLine(string line);
		AbstractAccount Account { get; }
		Language Language { get; }
	}

	public class Globals : PluginHolder {
		//The minimum and maximum ranges that a client can get packets within
		public const int MaxUpdateRange = 18;
		public const int MaxUpdateRangeSquared = MaxUpdateRange * MaxUpdateRange;
		public const int MinUpdateRange = 5;
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const int defaultWarningLevel = 4;

		private static int port;
		public static int Port {
			get { return port; }
		}

		private static string serverName;
		public static string ServerName {
			get { return serverName; }
		}

		public override string Name {
			get {
				return serverName;
			}
			set {
				//serverName = value;
				throw new SEException("Can't set server name directly. It's read from steamengine.ini");
			}
		}

		private static string adminEmail;
		public static string AdminEmail {
			get { return adminEmail; }
		}

		private static string commandPrefix;
		public static string CommandPrefix {
			get { return commandPrefix; }
		}

		private static string alternateCommandPrefix;
		public static string AlternateCommandPrefix {
			get { return alternateCommandPrefix; }
		}

		private static string logPath;
		public static string LogPath {
			get { return logPath; }
		}

		private static string savePath;
		public static string SavePath {
			get { return savePath; }
		}

		private static string mulPath;
		public static string MulPath {
			get { return mulPath; }
		}

		private static readonly string scriptsPath = Path.GetFullPath("./scripts/");
		public static string ScriptsPath {
			get { return scriptsPath; }
		}

		private static string docsPath;
		public static string DocsPath {
			get { return docsPath; }
		}

#if MSWIN
		private static string ndocExe;
		public static string NdocExe {
			get { return ndocExe; }
		}
#endif

		private static bool logToFiles;
		public static bool LogToFiles {
			get { return logToFiles; }
		}

		private static bool logToConsole;
		public static bool LogToConsole {
			get { return logToConsole; }
			set { logToConsole = value; }
		}

		private static byte maximalPlevel;
		public static byte MaximalPlevel {
			get { return maximalPlevel; }
		}

		private static int plevelOfGM;
		public static int PlevelOfGM {
			get { return plevelOfGM; }
		}

		private static int plevelForLscriptCommands;
		public static int PlevelForLscriptCommands {
			get { return plevelForLscriptCommands; }
		}

		private static bool allowUnencryptedClients;
		public static bool AllowUnencryptedClients {
			get { return allowUnencryptedClients; }
		}

		private static int reachRange;
		public static int ReachRange {
			get { return reachRange; }
		}

		private static int squaredReachRange;
		public static int SquaredReachRange {
			get { return squaredReachRange; }
		}

		private static int defaultAsciiMessageColor;
		public static int DefaultAsciiMessageColor {
			get { return defaultAsciiMessageColor; }
		}

		private static int defaultUnicodeMessageColor;
		public static int DefaultUnicodeMessageColor {
			get { return defaultUnicodeMessageColor; }
		}

		internal static bool useMap;
		public static bool UseMap {
			get {
				return useMap;
			}
		}

		private static bool generateMissingDefs;
		public static bool GenerateMissingDefs {
			get { return generateMissingDefs; }
		}

		private static bool useMultiItems;
		public static bool UseMultiItems {
			get { return useMultiItems; }
		}

		private static bool readBodyDefs;
		public static bool ReadBodyDefs {
			get {
				return readBodyDefs;
			}
			internal set {
				readBodyDefs = value;
			}
		}

		private static bool sendTileDataSpam;
		public static bool SendTileDataSpam {
			get { return sendTileDataSpam; }
		}

		private static bool fastStartUp;
		public static bool FastStartUp {
			get { return fastStartUp; }
		}

		private static bool parallelStartUp;
		public static bool ParallelStartUp {
			get { return parallelStartUp; }
		}

		private static bool netSyncingTracingOn;
		public static bool NetSyncingTracingOn {
			get { return netSyncingTracingOn; }
		}

		private static bool mapTracingOn;
		public static bool MapTracingOn {
			get {
				return mapTracingOn;
			}
		}

		private static bool writeMulDocsFiles;
		public static bool WriteMulDocsFiles {
			get { return writeMulDocsFiles; }
		}


		private static bool resolveEverythingAtStart;
		public static bool ResolveEverythingAtStart {
			get { return resolveEverythingAtStart; }
		}

		private static bool autoAccountCreation;
		public static bool AutoAccountCreation {
			get { return autoAccountCreation; }
		}

		private static bool blockOSI3DClient;
		public static bool BlockOSI3DClient {
			get { return blockOSI3DClient; }
		}

		private static int maxConnections;
		public static int MaxConnections {
			get { return maxConnections; }
		}

		private static int speechDistance;
		public static int SpeechDistance {
			get { return speechDistance; }
		}

		private static int emoteDistance;
		public static int EmoteDistance {
			get { return emoteDistance; }
		}

		private static int whisperDistance;
		public static int WhisperDistance {
			get { return whisperDistance; }
		}

		private static int yellDistance;
		public static int YellDistance {
			get { return yellDistance; }
		}

		private static int loginFlags;
		public static int LoginFlags {
			get { return loginFlags; }
		}

		private static int featuresFlags;
		public static int FeaturesFlags {
			get { return featuresFlags; }
		}

		private static int defaultItemModel;
		public static int DefaultItemModel {
			get { return defaultItemModel; }
		}

		private static int defaultCharModel;
		public static int DefaultCharModel {
			get { return defaultCharModel; }
		}

		private static bool hashPasswords;
		public static bool HashPasswords {
			get { return hashPasswords; }
		}

		private static bool scriptFloats;
		public static bool ScriptFloats {
			get { return scriptFloats; }
		}


		private static bool useAosToolTips;
		public static bool UseAosToolTips {
			get { return useAosToolTips; }
		}

		private static TagHolder lastNew;
		/// <summary>The last new item or character or memory or whatever created.</summary>
		public static TagHolder LastNew {
			get { return lastNew; }
			internal set { lastNew = value; }
		}

		private static AbstractCharacter lastNewChar;
		/// <summary>The last new Character created.</summary>
		public static AbstractCharacter LastNewChar {
			get { return lastNewChar; }
			internal set { lastNewChar = value; }
		}

		private static AbstractItem lastNewItem;
		/// <summary>The last new Item created.</summary>
		public static AbstractItem LastNewItem {
			get { return lastNewItem; }
			internal set { lastNewItem = value; }
		}

		/// <summary>The source of the current action.</summary>
		private static ISrc src;
		public static ISrc Src {
			get {
				return src;
			}
		}

		public static AbstractCharacter SrcCharacter {
			get {
				return src as AbstractCharacter;
			}
		}

		public static AbstractAccount SrcAccount {
			get {
				if (src != null) {
					return src.Account;
				}
				return null;
			}
		}

		public static Language SrcLanguage {
			get {
				if (src != null) {
					return src.Language;
				}
				return Language.Default;
			}
		}

		public static GameState SrcGameState {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.GameState;
				}
				return null;
			}
		}

		public static TcpConnection<GameState> SrcTcpConnection {
			get {
				AbstractCharacter ch = src as AbstractCharacter;
				if (ch != null) {
					return ch.GameState.Conn;
				}
				return null;
			}
		}

		public static void SrcWriteLine(string str) {
			if (src != null) {
				src.WriteLine(str);
			}
		}

		internal static void SetSrc(ISrc newSrc) {
			src = newSrc;
		}

		//public static Conn srcConn = null;


		/*
		 * Field: dice
		 * Call dice.Next(int min, int max) to generate a random number.
		 */
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly Random dice = new Random();

		private static Globals instance = new Globals();
		public static Globals Instance {
			get {
				return instance;
			}
		}

#if MSWIN
		private static Process ndocProcess;
#endif

		private Globals() {
			LoadIni();
		}

		internal static void Init() {
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void LoadIni() {
			try {
				Logger.WriteDebug("Loading steamengine.ini");
				IniFile iniH = new IniFile("steamengine.ini");
				IniFileSection setup = iniH.GetNewOrParsedSection("setup");

				serverName = setup.GetValue("name", "Unnamed SteamEngine Shard", "The name of your shard");
				adminEmail = setup.GetValue("adminEmail", "admin@email.com", "Your Email to be displayed in status web, etc.");
				hashPasswords = setup.GetValue("hashPasswords", false, "This hashes passwords (for accounts) using SHA512, which isn't reversable. Instead of writing passwords to the save files, the hash is written. If you disable this, then passwords will be recorded instead of the hashes, and will be stored instead of the hashes, which means that if someone obtains access to your accounts file, they will be able to read the passwords. It's recommended to leave this on. Note: If you switch this off after it's been on, passwords that are hashed will stay hashed, because hashing is one-way, until that account logs in, at which point the password (if it matches) will be recorded again in place of the hash. If you use text saves, you should be able to write password=whatever in the account save, and the password will be changed to that when that save is loaded, even if you're using hashed passwords.");

				//kickOnSuspiciousErrors = setup.GetValue<bool>("kickOnSuspiciousErrors", true, "Kicks the user if a suspiciously erroneous value is recieved in a packet from their client.");

				allowUnencryptedClients = setup.GetValue("allowUnencryptedClients", true, "Allow clients with no encryption to connect. There's no problem with that, except for lower security.");

				IniFileSection files = iniH.GetNewOrParsedSection("files");
				logPath = Path.GetFullPath(files.GetValue("logPath", "./logs/", "Path to the log files"));
				logToFiles = files.GetValue("logToFiles", true, "Whether to log console output to a file");
				CoreLogger.Init();
				savePath = Path.GetFullPath(files.GetValue("savePath", "./saves/", "Path to the save files"));
#if MSWIN
				ndocExe = Path.GetFullPath(files.GetValue("ndocExe", "C:\\Program Files\\NDoc\\bin\\.net-1.1\\NDocConsole.exe", "Command for NDoc invocation (leave it blank, if you don't want use NDoc)."));
#endif
				docsPath = Path.GetFullPath(files.GetValue("docsPath", "./docs/", "Path to the docs (Used when writing out some information from MUL files, like map tile info)"));
				useMap = files.GetValue("useMap", true, "Whether to load map0.mul and statics0.mul and use them or not.");
				generateMissingDefs = files.GetValue("generateMissingDefs", false, "Whether to generate missing scripts based on tiledata.");
				useMultiItems = files.GetValue("useMultiItems", true, "Whether to use multi items...");
				readBodyDefs = files.GetValue("readBodyDefs", true, "Whether to read Bodyconv.def (a client file) in order to determine what character models lack defs (and then to write new ones for them to 'scripts/defaults/chardefs/newCharDefsFromMuls.def').");
				writeMulDocsFiles = files.GetValue("writeMulDocsFiles", false, "If this is true/on/1, then SteamEngine will write out some files with general information gathered from various MUL files into the 'docs/MUL file docs' folder. These should be distributed with SteamEngine anyways, but this is useful sometimes (like when a new UO expansion is released).");

				IniFileSection login = iniH.GetNewOrParsedSection("login");
				//alwaysUpdateRouterIPOnStartup = (bool) login.GetValue<bool>("alwaysUpdateRouterIPOnStartup", false, "Automagically determine the routerIP every time SteamEngine is run, instead of using the setting for it in steamengine.ini.");
				//string omit = login.GetValue<string>("omitip", "5.0.0.0", "IP to omit from server lists, for example, omitIP=5.0.0.0. You can have multiple omitIP values, separated by comma.");
				//omitip = new List<IPAddress>();
				//foreach (string ip in omit.Split(',')) {
				//    Server.AddOmitIP(IPAddress.Parse(ip));
				//    omitip.Add(IPAddress.Parse(ip));
				//}
				//omitip = new System.Collections.ObjectModel.ReadOnlyCollection<IPAddress>(omitip);

				bool exists;
				string msgBox = "";
				if (iniH.FileExists) {
					exists = true;
					//string routerIP = login.GetValue<string>("routerIP", "", "The IP to show to people who are outside your LAN");
					////if (routerIP.Length>0) {
					//Server.SetRouterIP(routerIP);
					////}
					mulPath = files.GetValue("mulPath", "muls", "Path to the mul files");
				} else {
					string mulsPath = GetMulsPath();
					if (mulsPath == null) {
						msgBox += "Unable to locate the UO MUL files. Please either place them in the 'muls' folder or specify the proper path in steamengine.ini (change mulPath=muls to the proper path)\n\n";
						mulsPath = files.GetValue("mulPath", "muls", "Path to the mul files");
					} else {
						mulsPath = files.GetValue("mulPath", mulsPath, "Path to the mul files");
					}
					exists = false;
					//string[] ret = Server.FindMyIP();
					//string routerIP = ret[1];
					//msgBox += ret[0];
					//if (routerIP == null) {
					//    login.SetValue<string>("routerIP", "", "The IP to show to people who are outside your LAN");
					//} else {
					//    login.SetValue<string>("routerIP", routerIP, "The IP to show to people who are outside your LAN");
					//}
				}
				//timeZone = login.GetValue<sbyte>("timeZone", 5, "What time-zone you're in. 0 is GMT, 5 is EST, etc.");
				maxConnections = login.GetValue("maxConnections", 100, "The cap on # of connections. Affects the percentage-full number sent to UO client.");
				autoAccountCreation = login.GetValue("autoAccountCreation", false, "Automatically create accounts when someone attempts to log in");
				blockOSI3DClient = login.GetValue("blockOSI3DClient", true, "Block the OSI 3D client from connecting. Said client is not supported, since it tends to do things in a stupid manner.");

				IniFileSection ports = iniH.GetNewOrParsedSection("ports");
				port = ports.GetValue<ushort>("game", 2595, "The port to listen on for client connections");

				IniFileSection text = iniH.GetNewOrParsedSection("text");
				commandPrefix = text.GetValue("commandPrefix", ".", "The command prefix. You can make it 'Computer, ' if you really want.");
				alternateCommandPrefix = text.GetValue("alternateCommandPrefix", "[", "The command prefix. Defaults to [. In the god-client, . is treated as an internal client command, and anything starting with . is NOT sent to the server.");
				//supportUnicode = text.GetValue<bool>("supportUnicode", true, "If you turn this off, all messages, speech, etc sent TO clients will take less bandwidth, but nobody'll be able to speak in unicode (I.E. They can only speak using normal english characters, not russian, chinese, etc.)");
				//asciiForNames = text.GetValue<bool>("asciiForNames", false, "If this is on, names are always sent in ASCII regardless of what supportUnicode is set to. NOTE: Names in paperdolls and status bars can only be shown in ASCII, and this ensures that name colors come out right.");
				defaultAsciiMessageColor = text.GetValue<ushort>("serverMessageColor", 0x0000, "The color to use for server messages (Welcome to **, pause for worldsave, etc). Can be in hex, but it doesn't have to be.");
				defaultUnicodeMessageColor = text.GetValue<ushort>("defaultUnicodeMessageColor", 0x0394, "The color to use for unicode messages with no specified color (or a specified color of 0, which is not really valid for unicode messages).");

				IniFileSection ranges = iniH.GetNewOrParsedSection("ranges");
				reachRange = ranges.GetValue<ushort>("reachRange", 5, "The distance (in spaces) a character can reach.");
				squaredReachRange = reachRange * reachRange;
				//sightRange = ranges.GetValue<ushort>("sightRange", 15, "The distance (in spaces) a character can see.");

				speechDistance = ranges.GetValue("speechDistance", 10, "The maximum distance from which normal speech can be heard.");
				emoteDistance = ranges.GetValue("emoteDistance", 10, "The maximum distance from which an emote can be heard/seen.");
				whisperDistance = ranges.GetValue("whisperDistance", 2, "The maximum distance from which a whisper can be heard.");
				yellDistance = ranges.GetValue("yellDistance", 20, "The maximum distance from which a yell can be heard.");

				IniFileSection plevels = iniH.GetNewOrParsedSection("plevels");
				maximalPlevel = plevels.GetValue<byte>("maximalPlevel", 7, "Maximal plevel - the highest possible plevel (the owner's plevel)");
				plevelOfGM = plevels.GetValue("plevelOfGM", 4, "Plevel needed to do all the cool stuff GM do. See invis, walk thru walls, ignore line of sight, own all animals, etc.");
				plevelForLscriptCommands = plevels.GetValue("plevelForLscriptCommands", 2, "With this (or higher) plevel, the client's commands are parsed and executed as LScript statements. Otherwise, much simpler parser is used, for speed and security.");

				IniFileSection scripts = iniH.GetNewOrParsedSection("scripts");
				resolveEverythingAtStart = scripts.GetValue("resolveEverythingAtStart", false, "If this is false, Constants and fields of scripted defs (ThingDef,Skilldef, etc.) will be resolved from the text on demand (and probably at the first save). Otherwise, everything is resolved on the start. Leave this to false on your development server, but set it to true for a live shard, because it's more secure.");
				defaultItemModel = scripts.GetValue<ushort>("defaultItemModel", 0xeed, "The item model # to use when an itemdef has no model specified.");
				defaultCharModel = scripts.GetValue<ushort>("defaultCharModel", 0x0190, "The character body/model # to use when a chardef has no model specified.");
				scriptFloats = scripts.GetValue("scriptFloats", true, "If this is off, dividing/comparing 2 numbers in Lscript is treated as if they were 2 integers (rounds before the computing if needed), effectively making the scripting engine it backward compatible to old spheres. Otherwise, the precision of the .NET Double type is used in scripts.");
				//Logger.showCoreExceptions = scripts.GetValue<bool>("showCoreExceptions", true, "If this is off, only the part of Exception stacktrace that occurs in the scripts is shown. If you're debugging core, have it on. If you're debugging scripts, have it off to save some space on console.");

				//loginFlags = 0;
				//featuresFlags = 0;

				IniFileSection features = iniH.GetNewOrParsedSection("features");
				//features.Comment("These are features which can be toggled on or off.");
				useAosToolTips = features.GetValue("useAosToolTips", true, "If this is on, AOS tooltips (onmouseover little windows instead of onclick texts) are enabled. Applies for clients > 3.0.8o");
				//OneCharacterOnly = (bool) features.IniEntry("OneCharacterOnly", (bool)false, "Limits accounts to one character each (except GMs)).");

				featuresFlags |= 0x2;
				if (useAosToolTips) {
					loginFlags |= 0x20;
					featuresFlags |= 0x0008 | 0x8000;
				}
				//for now we only set whether tooltips work.

				//TODO?:
				//OneCharacterOnly
				//SixCharacters
				//RightClickMenus
				//Chat? (now disabled)
				//LBR? (now enabled)

				//'loginFlags' variable:
				//0x14 = One character only
				//0x08 = Right-click menus
				//0x20 = AOS features
				//0x40 = Six characters instead of five

				//'featuresFlags' variable
				//0x0001 = Chat for pre-AOS clients
				//0x0002 = LBR for pre-AOS clients
				//0x0004 = Chat for AOS clients
				//0x0008 = LBR for AOS clients
				//0x0010 = Allow creating Paladin/Necromancer.
				//0x0020 = Six characters instead of five.
				//0x8000 = AOS Feature Enable (Allow features 0x04 - 0x20)

				//So normally that would be 0x800f, 0x801f to allow pal/nec, and also include 0x20 for six characters.
				//So this can't quite be a constant. Oh well!

				//Variables:
				//One character only	: LF 0x14 FF 0x0000
				//Chat					: LF 0x00 FF 0x8005
				//LBR					: LF 0x00 FF 0x800a
				//Right-click menus		: LF 0x08 FF 0x0000
				//AOS					: LF 0x20 FF 0x8010
				//Six Characters		: LF 0x40 FF 0x8020


				IniFileSection temporary = iniH.GetNewOrParsedSection("temporary");
				temporary.AddComment("These are temporary INI settings, which will be going away in future versions.");
				fastStartUp = temporary.GetValue("fastStartUp", false, "If set to true, some time consuming steps in the server init phase will be skipped (like loading of defs and scripts), for faster testing of other functions. In this mode, the server will be of course not usable for game serving.");
				parallelStartUp = temporary.GetValue("parallelStartUp", false, "If set to true, some parts of startup will run multi-threaded. Not recommended for production.");
				sendTileDataSpam = temporary.GetValue("sendTileDataSpam", false, "Set this to true, and you'll be sent lots of spam when you walk. Yeah, this is temporary. I need it for testing tiledata stuff. -SL");
				netSyncingTracingOn = temporary.GetValue("netSyncingTracingOn", false, "Networking.SyncQueue info messages");
				mapTracingOn = temporary.GetValue("mapTracingOn", false, "Regions.Map info messages");

				temporary.AddComment("");
				temporary.AddComment("SteamEngine determines if someone is on your LAN by comparing their IP with all of yours. If the first three parts of the IPs match, they're considered to be on your LAN. Note that this will probably not work for people on very big LANs. If you're on one, please post a feature request on our SourceForge site.");
				temporary.AddComment("http://steamengine.sf.net/");


				if (!exists) {
					iniH.WriteToFile();
					MainClass.CommandExit();
					throw new ShowMessageAndExitException(msgBox + "SteamEngine has written a default 'steamengine.ini' for you. Please take a look at it, change whatever you want, and then run SteamEngine again to get started.", "Getting started");
				}

			} catch (ShowMessageAndExitException smaee) {
				Logger.Show(smaee.Message);
				smaee.Show();
				MainClass.CommandExit();
			} catch (Exception globalexp) {
				Console.WriteLine();
				Logger.WriteFatal(globalexp);
				MainClass.CommandExit();
			}
		}

		/**
			Looks in the registry to find the path to the MUL files. If both 2d and 3d are installed,
			it isn't specified which this will find.
		*/
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public static string GetMulsPath() {
#if MSWIN
			RegistryKey soft = Registry.LocalMachine.OpenSubKey("SOFTWARE");
			if (soft != null) {
				RegistryKey rk = soft.OpenSubKey("Origin Worlds Online");
				if (rk == null) {
					rk = soft.OpenSubKey("Wow6432Node"); //32bit stuff in 64bit Windows? Bizarre node name anyway
					if (rk != null) {
						rk = rk.OpenSubKey("Origin Worlds Online");
					}
				}

				if (rk != null) {
					string[] names = rk.GetSubKeyNames();
					foreach (string name in names) {
						RegistryKey uoKey = rk.OpenSubKey(name);
						if (uoKey != null) {
							string[] names2 = uoKey.GetSubKeyNames();
							foreach (string name2 in names2) {
								RegistryKey verKey = uoKey.OpenSubKey(name2);
								string s = verKey.GetValue("InstCDPath") as string;
								if (s != null) {
									return s;
								}
							}
						} else {
							Logger.WriteWarning("Unable to open 'uoKeys' in registry");
						}
					}
				} else {
					Logger.WriteWarning("Unable to open 'Origin Worlds Online' in registry");
				}
			} else {
				Logger.WriteWarning("Unable to open 'SOFTWARE' in registry");
			}
#else
			Logger.WriteWarning("TODO: Implement some way to find the muls when running from MONO?");
#endif
			return null;
		}

		//public static readonly string version="1.0.0"; 
		private static string version;
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static string Version {
			get {
				if (version == null) {
					try {
						version = GetVersion();
					} catch (Exception e) {
						Logger.WriteError("While obtaining SVN revision info", e);
						version = "<SVN revision number unknown>";
					}
				}
				return version;
			}
		}

		private static string GetVersion() {
			using (SvnClient client = new SvnClient()) {
				SvnInfoEventArgs info;
				client.GetInfo(SvnTarget.FromString(Path.GetFullPath("."), true), out info);

				return "SVN revision " + info.Revision;
			}
		}

		private static readonly DateTime startedAt = DateTime.Now;
		public static TimeSpan TimeUp {
			get {
				return DateTime.Now - startedAt;
			}
		}

		//public static bool ShowCoreExceptions {
		//    get { return Logger.showCoreExceptions; }
		//    set { Logger.showCoreExceptions = value; }
		//}

		public static int StatClients {
			get {
				return GameServer.AllClients.Count;
			}
		}

		public static int StatItems {
			get {
				return AbstractItem.Instances;
			}
		}

		public static int StatChars {
			get {
				return AbstractCharacter.Instances;
			}
		}

		//internal static IPAddress[] ips;

		//public string IP {
		//    get {
		//        return Tools.ObjToString(ips);
		//    }
		//}

		public static void Exit() {
			MainClass.CommandExit();
		}

		public static void SetConsoleTitle(string title) {
			Console.WriteLine("Reseting console title to '" + title + "'" + LogStr.Title(title));
		}

		public static void Save() {
			Sanity.IfTrueThrow(!RunLevelManager.IsRunning, "!RunLevelManager.IsRunning @ Save()");

			//Packets.NetState.ProcessAll();
			SyncQueue.ProcessAll();

			PauseServerTime();
			WorldSaver.Save();
			UnPauseServerTime();
		}

		public static void G() {
			MainClass.CollectGarbage();
		}

		public static void Recompile() {
			//forced complete recompiling
			MainClass.RecompileScripts();//can block for some time
		}

		public static void Resync() {
			if (!MainClass.TryResyncCompiledScripts()) {//can block for some time
				ScriptLoader.Resync();
				MainClass.CollectGarbage();
			}
		}

		public static void R() {
			Resync();
		}

		public static void SvnUpdate() {
			VersionControl.SvnUpdateProject(Path.GetFullPath("."));
		}

		public static void SvnCleanUp() {
			VersionControl.SvnCleanUpProject(Path.GetFullPath("."));
		}

		public static Type Type(string typename) {
			//global type, you can actually get any type with this
			return System.Type.GetType(typename, true, true);//true for case insensitive
		}

		public static Type SE(string typename) {
			//Console.WriteLine("type: "+Type.GetType("SteamEngine."+typename));
			return System.Type.GetType("SteamEngine." + typename, true, true);
		}

		//sphere compatibility
		public static Type AccMgr() {
			return typeof(AbstractAccount);
		}

		public static AbstractAccount FindAccount(string name) {
			return AbstractAccount.GetByName(name);
		}

		public static string Hex(int numb) {
			return "0x" + Convert.ToString(numb, 16);
		}

		public static string Dec(int numb) {
			return Convert.ToString(numb, 10);
		}

		public static void B(string msg) {
			PacketSequences.BroadCast(msg);
		}

		public static void SysMessage(object o) {
			Console.WriteLine(Tools.ObjToString(o));
		}

		public static void Log(object o) {
			Console.WriteLine(Tools.ObjToString(o));
		}

		public override string ToString() {
			return serverName;
		}

		internal static void ClearAll() {
			instance = new Globals();
		}

		internal static void SaveGlobals(SaveStream output) {
			Logger.WriteDebug("Saving globals.");
			output.WriteComment("globals");
			output.WriteLine();
			output.WriteSection("Globals", serverName.Replace(' ', '_'));//the header name is in fact ignored
			output.WriteValue("time", TimeInTicks);
			instance.Save(output);
			output.WriteLine();
			ObjectSaver.FlushCache(output);
			AbstractDef.SaveAll(output);
		}

		internal static void LoadGlobals(PropsSection input) {
			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			PropsLine timeLine = input.TryPopPropsLine("time");
			long value;
			if ((timeLine != null) && (ConvertTools.TryParseInt64(timeLine.Value, out value))) {
				lastMarkServerTime = TimeSpan.FromTicks(value);
			} else {
				Logger.WriteWarning("The Globals section of save is missing the " + LogStr.Ident("Time") + " value or the value is invalid, setting server time to 0");
				lastMarkServerTime = TimeSpan.Zero;
			}
			lastMarkRealTime = DateTime.Now;

			instance.LoadSectionLines(input);
		}

#if MSWIN
		private static void ndocExited(object source, EventArgs e) {
			if (ndocProcess.ExitCode != 0) {
				Logger.WriteError("NDOC was not finished correctly (exit code: " + ndocProcess.ExitCode + ").");
			} else {
				Console.WriteLine("NDoc finished successfuly");
			}
			ndocProcess = null;
		}

		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static void CompileDocs() {
			if (ndocProcess != null) {
				Logger.WriteError("NDoc is already running.");
				return;
			}
			//try {
			//    Console.WriteLine("Generating Common XML documentation file");
			//    DocScanner scanner = new DocScanner();
			//    Assembly asm = ClassManager.CommonAssembly;
			//    scanner.ScanAssembly(asm);
			//    scanner.WriteToFile("bin\\" + asm.GetName().Name + ".xml");

			//    Console.WriteLine("Generating Core XML documentation file");
			//    asm = ClassManager.CoreAssembly;
			//    scanner = new DocScanner();
			//    scanner.ScanAssembly(asm);
			//    scanner.WriteToFile("bin\\" + asm.GetName().Name + ".xml");

			//    Console.WriteLine("Generating Scripts XML documentation file");
			//    asm = ClassManager.ScriptsAssembly;
			//    scanner = new DocScanner();
			//    scanner.ScanAssembly(asm);
			//    scanner.WriteToFile("bin\\" + asm.GetName().Name + ".xml");

			//} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			if (ndocExe.Length != 0) {
				Console.WriteLine("Invoking NDOC, documentation will be generated to docs/sourceDoc");
				try {
#if DEBUG
					string project = "debug.ndoc";
#elif SANE
					string project="sane.ndoc";
#elif OPTIMIZED
					string project="optimized.ndoc";
#endif
					ProcessStartInfo info = new ProcessStartInfo(ndocExe, "-project=" + project);
					info.WorkingDirectory = "./distrib/";
					info.WindowStyle = ProcessWindowStyle.Normal;
					info.UseShellExecute = true;
					ndocProcess = new Process();
					ndocProcess.StartInfo = info;
					ndocProcess.Exited += ndocExited;
					ndocProcess.EnableRaisingEvents = true;
					ndocProcess.Start();
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}
#endif

		public static void Load(string filename) {
			ScriptLoader.LoadNewFile(filename);
			Src.WriteLine("Script file loaded.");
		}

		//public static void NetStats() {
		//    GameConn.NetStats();
		//}

		public static string GetMulDocPathFor(string filename) {
			string docPath = Path.Combine(docsPath, "defaults");
			Tools.EnsureDirectory(docPath, true);

			return Path.Combine(docPath, filename);
		}

		//public static void CompressionStats() {
		//    PacketStats.CompressionStats();
		//}

		//public static void Logout() {
		//    ConsoleDummy conn = Globals.Src as ConsoleDummy;
		//    if (conn!=null) {
		//        if (!conn.IsNativeConsole) {
		//            conn.Close("Commanded to log out.");
		//        } else {
		//            conn.WriteLine("Native console cannot be logged out.");
		//        }
		//    }
		//}

		//public static void ConTest() {
		//	Globals.src.WriteLine(">>>>>> Text Console Test"+Environment.NewLine);
		//	Globals.src.WriteLine(LogStr.Warning("Warning: ")+LogStr.WarningData("this is warning message"+Environment.NewLine));
		//	Globals.src.WriteLine(LogStr.Error("Error: ")+LogStr.ErrorData("this is error message"+Environment.NewLine));
		//	Globals.src.WriteLine(LogStr.Fatal("Fatal: ")+LogStr.FatalData("this is fatal message"+Environment.NewLine));
		//	Globals.src.WriteLine(LogStr.Critical("Critical: ")+LogStr.CriticalData("this is critical message"+Environment.NewLine));
		//	Globals.src.WriteLine("FileLine: "+LogStr.FileLine("Globals.cs",540)+" (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Highlight: "+LogStr.Highlight("highlighted text")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Ident: "+LogStr.Ident("identifiers")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("FilePos: "+LogStr.FilePos("position in file")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("File: "+LogStr.File("file and directory names")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Number: "+LogStr.Number("numbers")+ " (and this is default style)"+Environment.NewLine);
		//	Globals.src.WriteLine("Global style test..."+Environment.NewLine);
		//	Globals.src.WriteLine(LogStr.SetStyle(LogStyles.Highlight)+LogStr.Number("LogStr.SetStyle(LogStyles.Highlight)")+" called, this text is produced by "+LogStr.Ident("LogStr.Raw()")+" (and highlighted style again)"+Environment.NewLine);
		//	Globals.src.WriteLine("style selected by "+LogStr.Ident("LogStr.SetStyle()")+" is reseted to default style on next line"+Environment.NewLine);
		//	Globals.src.WriteLine("<<<<<< End of Console Test"+Environment.NewLine);
		//}

		public static void RunTests() {
			TestSuite.RunAllTests();
			//PacketSender.DiscardAll();
		}

		//public static string CurrentFile { get {
		//	return WorldSaver.CurrentFile; 
		//} }

		private static DateTime lastMarkRealTime = DateTime.Now;
		private static TimeSpan lastMarkServerTime = TimeSpan.Zero;
		private static int paused;


		public static long TimeInTicks {
			get {
				return TimeAsSpan.Ticks;
			}
		}

		public static double TimeInSeconds {
			get {
				return TimeAsSpan.TotalSeconds;
			}
		}

		public static TimeSpan TimeAsSpan {
			get {
				if (paused > 0) {
					return lastMarkServerTime;
				}
				DateTime current = DateTime.Now;
				return lastMarkServerTime + (current - lastMarkRealTime);
			}
		}

		/// <summary>For sphere compatibility, this returns servertime in tenths of second</summary>
		public long Time {
			get {
				return (long) (TimeAsSpan.TotalSeconds / 10d);
			}
		}

		internal static void PauseServerTime() {
			paused++;
			//Logger.WriteDebug("paused level raised to " + paused);
			//Logger.WriteDebug(new StackTrace());
			if (paused == 1) {
				DateTime current = DateTime.Now;

				lastMarkServerTime = lastMarkServerTime + (current - lastMarkRealTime);
				lastMarkRealTime = current;

				RunLevelManager.SetPaused();
			}
		}

		internal static void UnPauseServerTime() {
			paused--;
			//Logger.WriteDebug("paused level sank to " + paused);
			//Logger.WriteDebug(new StackTrace());
			Sanity.IfTrueThrow(paused < 0, "Can't UnPause when not paused");
			if (paused == 0) {
				lastMarkRealTime = DateTime.Now;
				RunLevelManager.UnsetPaused();
			}
		}
	}
}