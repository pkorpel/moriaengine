using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1035 - coach stations, shown as "Travel" in the .settings dialog.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class TravelSettings : SettingsMetaCategory {

		[SavedMember]
		public static TravelSettings instance = new TravelSettings();

		[LoadingInitializer]
		public TravelSettings() {
		}

		/// <summary>Erebor's constprice: gold is tiles * 1000 / this, so 3500 is 1 gp per 3.5 tiles.</summary>
		[SaveableData]
		public int priceDivisor = 3500;

		/// <summary>Erebor's escortprice: leaving enemy land costs this many tenths of the price, 50 is
		/// five times. The script's comment says three, its number says five.</summary>
		[SaveableData]
		public int escortPrice = 50;

		/// <summary>Seconds counted above the head before the coach leaves.</summary>
		[SaveableData]
		public int rideSeconds = 15;

		/// <summary>How far from the stone a passenger may be when it does.</summary>
		[SaveableData]
		public int strayTiles = 6;
	}
}
