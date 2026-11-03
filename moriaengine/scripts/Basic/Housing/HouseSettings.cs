using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#941 - .settings "Houses". Erebor's GMs set each house's rent by hand and those are lost, so it comes from the
	/// size of the house: the median of 112 tiles costs about Erebor's default of 100k.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class HouseSettings : SettingsMetaCategory {

		[SavedMember]
		public static HouseSettings instance = new HouseSettings();

		[LoadingInitializer]
		public HouseSettings() {
		}

		[SaveableData]
		public int rentPerTile = 900;

		[SaveableData]
		public int minimumRent = 50000;

		/// <summary>#1017 - the user's rule for stalls and fields: about 300 gp a tile, at least 15000.</summary>
		[SaveableData]
		public int stallRentPerTile = 300;

		[SaveableData]
		public int stallMinimumRent = 15000;

		[SaveableData]
		public int fieldRentPerTile = 300;

		[SaveableData]
		public int fieldMinimumRent = 15000;

		[SaveableData]
		public int rentRounding = 1000;

		/// <summary>Days a new owner has to pay the first rent. Erebor had none.</summary>
		[SaveableData]
		public int claimGraceDays = 3;

		[SaveableData]
		public int bankActivationPrice = 100000;

		/// <summary>Seconds a character lingers after logging out in a house, stall or field, instead of the usual delay.</summary>
		[SaveableData]
		public double logoutDelaySeconds = 10;
	}
}
