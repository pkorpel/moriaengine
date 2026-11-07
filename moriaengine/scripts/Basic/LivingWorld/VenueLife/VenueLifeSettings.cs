using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1256 - .settings "Lazne a divadlo": the bath house, the theatre and Mordor's fighting pits (design 9.2).</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class VenueLifeSettings : SettingsMetaCategory {
		[SavedMember]
		public static VenueLifeSettings instance = new VenueLifeSettings();

		[LoadingInitializer]
		public VenueLifeSettings() {
		}

		/// <summary>A bath, how long its freshness lasts in minutes of play, and the shops' discount while it does.</summary>
		[SaveableData]
		public int bathPrice = 20, freshMinutes = 30, freshDiscount = 5;

		/// <summary>The stoker pays this for a log, and takes this many logs a real day.</summary>
		[SaveableData]
		public int woodWage = 3, woodPerDay = 30;

		/// <summary>Seconds between a play's lines; how many lines' time an actor waits for a prompt.</summary>
		[SaveableData]
		public int playLineSeconds = 5, promptLines = 2;

		/// <summary>Real minutes between two plays in one theatre (a game day is 24).</summary>
		[SaveableData]
		public int playEveryMinutes = 20;

		/// <summary>An extra's wages and experience; the prompt's reward; the Musicianship an extra needs, in tenths.</summary>
		[SaveableData]
		public int extraWage = 40, extraExp = 60, promptReward = 25, extraMusicianship = 300;

		/// <summary>The pits' three bets and how many a player places in a real hour.</summary>
		[SaveableData]
		public int pitBetSmall = 10, pitBetMedium = 50, pitBetLarge = 100, pitBetsPerHour = 10;
	}
}
