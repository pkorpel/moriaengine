using System.ComponentModel;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	// Reference models for the current INI, not registrations for services or their complete settings.
	[IniSection("setup")]
	internal sealed class SetupConfiguration {
		[Description("The name of your shard")]
		public string name { get; init; } = "Unnamed SteamEngine Shard";

		[Description("Your Email to be displayed in status web, etc.")]
		public string adminEmail { get; init; } = "admin@email.com";

		[Description("This hashes passwords (for accounts) using SHA512, which isn't reversable. Instead of writing passwords to the save files, the hash is written. If you disable this, then passwords will be recorded instead of the hashes, and will be stored instead of the hashes, which means that if someone obtains access to your accounts file, they will be able to read the passwords. It's recommended to leave this on. Note: If you switch this off after it's been on, passwords that are hashed will stay hashed, because hashing is one-way, until that account logs in, at which point the password (if it matches) will be recorded again in place of the hash. If you use text saves, you should be able to write password=whatever in the account save, and the password will be changed to that when that save is loaded, even if you're using hashed passwords.")]
		public bool hashPasswords { get; init; } = false;

		[Description("Allow clients with no encryption to connect. There's no problem with that, except for lower security.")]
		public bool allowUnencryptedClients { get; init; } = true;
	}

	[IniSection("files")]
	internal sealed class FilesConfiguration {
		[Description("Path to the log files")]
		public string logPath { get; init; } = "./logs/";

		[Description("Whether to log console output to a file")]
		public bool logToFiles { get; init; } = true;

		[Description("Path to the save files")]
		public string savePath { get; init; } = "./saves/";

		[Description("Path to the docs (Used when writing out some information from MUL files, like map tile info)")]
		public string docsPath { get; init; } = "./docs/";

		[Description("Whether to load map0.mul and statics0.mul and use them or not.")]
		public bool useMap { get; init; } = true;

		[Description("Whether to generate missing scripts based on tiledata.")]
		public bool generateMissingDefs { get; init; } = false;

		[Description("Whether to use multi items...")]
		public bool useMultiItems { get; init; } = true;

		[Description("Whether an item dropped on the ground turns to the next model of its flip group, as Sphere's FlipDroppedItems. Off, only items with FLIP=1 do.")]
		public bool flipDroppedItems { get; init; } = true;

		[Description("Minutes between automatic world saves, 0 turns them off. A GM's .nextsave says when the next one is due. Shutting down with .exit saves too, .exitnosave doesn't.")]
		public int autosaveMinutes { get; init; } = 60;

		[Description("Whether to read Bodyconv.def (a client file) in order to determine what character models lack defs (and then to write new ones for them to 'scripts/defaults/chardefs/newCharDefsFromMuls.def').")]
		public bool readBodyDefs { get; init; } = true;

		[Description("If this is true/on/1, then SteamEngine will write out some files with general information gathered from various MUL files into the 'docs/MUL file docs' folder. These should be distributed with SteamEngine anyways, but this is useful sometimes (like when a new UO expansion is released).")]
		public bool writeMulDocsFiles { get; init; } = false;

		[Description("Path to the mul files")]
		public string mulPath { get; init; } = "../uo-files";
	}

	[IniSection("login")]
	internal sealed class LoginConfiguration {
		[Description("The cap on # of connections. Affects the percentage-full number sent to UO client.")]
		public int maxConnections { get; init; } = 100;

		[Description("Automatically create accounts when someone attempts to log in")]
		public bool autoAccountCreation { get; init; } = false;

		[Description("Block the OSI 3D client from connecting. Said client is not supported, since it tends to do things in a stupid manner.")]
		public bool blockOSI3DClient { get; init; } = true;
	}

	[IniSection("ports")]
	internal sealed class PortsConfiguration {
		[Description("The port to listen on for client connections")]
		public ushort game { get; init; } = 2595;
	}

	[IniSection("text")]
	internal sealed class TextConfiguration {
		[Description("The command prefix. You can make it 'Computer, ' if you really want.")]
		public string commandPrefix { get; init; } = ".";

		[Description("The command prefix. Defaults to [. In the god-client, . is treated as an internal client command, and anything starting with . is NOT sent to the server.")]
		public string alternateCommandPrefix { get; init; } = "[";

		[Description("The default language for a connection, used until (and unless) the client sends its own language choice. Valid values: English, Czech.")]
		public Language defaultLanguage { get; init; } = Language.English;

		[Description("The color to use for server messages (Welcome to **, pause for worldsave, etc). Can be in hex, but it doesn't have to be.")]
		public ushort serverMessageColor { get; init; } = 0x0000;

		[Description("The color to use for unicode messages with no specified color (or a specified color of 0, which is not really valid for unicode messages).")]
		public ushort defaultUnicodeMessageColor { get; init; } = 0x0394;
	}

	[IniSection("ranges")]
	internal sealed class RangesConfiguration {
		[Description("The distance (in spaces) a character can reach.")]
		public ushort reachRange { get; init; } = 5;

		[Description("The maximum distance from which normal speech can be heard.")]
		public int speechDistance { get; init; } = 10;

		[Description("The maximum distance from which an emote can be heard/seen.")]
		public int emoteDistance { get; init; } = 10;

		[Description("The maximum distance from which a whisper can be heard.")]
		public int whisperDistance { get; init; } = 2;

		[Description("The maximum distance from which a yell can be heard.")]
		public int yellDistance { get; init; } = 20;
	}

	[IniSection("plevels")]
	internal sealed class PlevelsConfiguration {
		[Description("Maximal plevel - the highest possible plevel (the owner's plevel)")]
		public byte maximalPlevel { get; init; } = 7;

		[Description("Plevel needed to do all the cool stuff GM do. See invis, walk thru walls, ignore line of sight, own all animals, etc.")]
		public int plevelOfGM { get; init; } = 4;

		[Description("With this (or higher) plevel, the client's commands are parsed and executed as LScript statements. Otherwise, much simpler parser is used, for speed and security.")]
		public int plevelForLscriptCommands { get; init; } = 2;
	}

	[IniSection("scripts")]
	internal sealed class ScriptsConfiguration {
		[Description("If this is false, Constants and fields of scripted defs (ThingDef,Skilldef, etc.) will be resolved from the text on demand (and probably at the first save). Otherwise, everything is resolved on the start. Leave this to false on your development server, but set it to true for a live shard, because it's more secure.")]
		public bool resolveEverythingAtStart { get; init; } = false;

		[Description("The item model # to use when an itemdef has no model specified.")]
		public ushort defaultItemModel { get; init; } = 0xeed;

		[Description("The character body/model # to use when a chardef has no model specified.")]
		public ushort defaultCharModel { get; init; } = 0x0190;

		[Description("If this is off, dividing/comparing 2 numbers in Lscript is treated as if they were 2 integers (rounds before the computing if needed), effectively making the scripting engine it backward compatible to old spheres. Otherwise, the precision of the .NET Double type is used in scripts.")]
		public bool scriptFloats { get; init; } = true;
	}

	[IniSection("features")]
	internal sealed class FeaturesConfiguration {
		[Description("If this is on, AOS tooltips (onmouseover little windows instead of onclick texts) are enabled. Applies for clients > 3.0.8o")]
		public bool useAosToolTips { get; init; } = true;

		[Description("NPCs sleep where no player is near: the world is cut into areas of 64x64 tiles, and an area is awake while a player is in it or in a neighbouring one, and for this many seconds after the last of them left. A sleeping NPC doesn't think at all until its area wakes. 0 turns sleeping off.")]
		public double npcAreaAwakeSeconds { get; init; } = 300.0;
	}

	[IniSection("diagnostics")]
	internal sealed class DiagnosticsConfiguration {
		[Description("Whether to count from the start where the game's time goes: time spent holding the global lock, by what held it, and time spent waiting for it. An admin's .perfon/.perfoff switch it while running, .perf shows the counts. Costs a little while on, nothing while off.")]
		public bool perfCounters { get; init; } = false;
	}

	[IniSection("temporary")]
	internal sealed class TemporaryConfiguration {
		[Description("If set to true, some time consuming steps in the server init phase will be skipped (like loading of defs and scripts), for faster testing of other functions. In this mode, the server will be of course not usable for game serving.")]
		public bool fastStartUp { get; init; } = false;

		[Description("If set to true, some parts of startup will run multi-threaded. Not recommended for production.")]
		public bool parallelStartUp { get; init; } = false;

		[Description("Set this to true, and you'll be sent lots of spam when you walk. Yeah, this is temporary. I need it for testing tiledata stuff. -SL")]
		public bool sendTileDataSpam { get; init; } = false;

		[Description("Networking.SyncQueue info messages")]
		public bool netSyncingTracingOn { get; init; } = false;

		[Description("Regions.Map info messages")]
		public bool mapTracingOn { get; init; } = false;
	}
}
