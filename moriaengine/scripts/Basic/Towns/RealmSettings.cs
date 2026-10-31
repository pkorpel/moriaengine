using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#892 - the numbers of Erebor's General and Nazguls, the "Realm Offices" category of .settings.
	/// Defaults are Erebor's (systems/generals.scp, nazguls.scp, sphereworld's nastaveni_*).</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class RealmSettings : SettingsMetaCategory {

		[SavedMember]
		public static RealmSettings instance = new RealmSettings();

		[LoadingInitializer]
		public RealmSettings() {
		}

		/// <summary>nastaveni_generals_honor_distance</summary>
		[SaveableData]
		public int honorDistance = 8;

		[SaveableData]
		public int honorBonus = 5;

		/// <summary>nastaveni_honor_effect_counter</summary>
		[SaveableData]
		public int honorEffectSeconds = 240;

		/// <summary>nastaveni_honor_delay_counter</summary>
		[SaveableData]
		public int honorDelaySeconds = 300;

		public override string ToString() {
			return "Realm Offices";
		}
	}
}
