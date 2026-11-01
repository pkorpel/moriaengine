using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#924 - a player's hunger. 30 minutes is Sphere's own rate: Erebor's REGEN_4 line never reached it.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class HungerSettings : SettingsMetaCategory {
		[SavedMember]
		public static HungerSettings instance = new HungerSettings();

		[LoadingInitializer]
		public HungerSettings() {
		}

		[SaveableData]
		public double secondsPerFoodPoint = 1800;

		[SaveableData]
		public int starvingDamage = 10;
	}
}
