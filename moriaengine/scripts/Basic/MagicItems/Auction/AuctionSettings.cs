using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - the magic item auction's numbers (.settings -> "Auction"). See AuctionHouse.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class AuctionSettings : SettingsMetaCategory {

		[SavedMember]
		public static AuctionSettings instance = new AuctionSettings();

		[LoadingInitializer]
		public AuctionSettings() {
		}

		/// <summary>How long a listing runs.</summary>
		[SaveableData]
		public int durationHours = 72;

		/// <summary>A new bid beats the one before by at least this much, in percent.</summary>
		[SaveableData]
		public int minRaisePercent = 5;

		/// <summary>Listings one player may have running at once.</summary>
		[SaveableData]
		public int maxListings = 10;

		/// <summary>The house's cut of a sale, in percent; none by the user's call.</summary>
		[SaveableData]
		public int houseCutPercent = 0;
	}
}
