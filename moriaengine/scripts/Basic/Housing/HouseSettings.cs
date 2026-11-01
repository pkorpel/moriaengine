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

		[SaveableData]
		public int rentRounding = 1000;

		/// <summary>Days a new owner has to pay the first rent. Erebor had none.</summary>
		[SaveableData]
		public int claimGraceDays = 3;
	}
}
