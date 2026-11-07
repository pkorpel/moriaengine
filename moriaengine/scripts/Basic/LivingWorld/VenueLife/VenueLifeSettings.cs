using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1256 - .settings "Podniky": the bath house, the theatre, Mordor's fighting pits (#1256) and the armoury, smithy,
	/// temple and Houses of Healing (#1258) (design 9.2).</summary>
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

		/// <summary>The smith pays this for a piece of iron ore, up to this many a real day.</summary>
		[SaveableData]
		public int oreWage = 4, orePerDay = 20;

		/// <summary>Working the bellows: the stamina it takes, the pay, and the real minutes before the same player works them again.</summary>
		[SaveableData]
		public int bellowsStamina = 10, bellowsWage = 5, bellowsMinutes = 5;

		/// <summary>A temple offering, once a real day: the gift and the karma it gives.</summary>
		[SaveableData]
		public int offeringPrice = 25, offeringKarma = 50;

		/// <summary>A night vigil in the temple, once a real day: how long to stay, the experience and karma at its end.</summary>
		[SaveableData]
		public int vigilSeconds = 120, vigilExp = 100, vigilKarma = 50;

		/// <summary>Dressing a patient's wounds: the Healing it needs in tenths, and the house's pay.</summary>
		[SaveableData]
		public int bandageHealing = 200, bandageWage = 10;
	}
}
