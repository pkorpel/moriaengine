using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#962 - f_vyroba_latky's numbers. skillPerBatch and the two yields are mnozkoef, pocet_z_vlny and pocet_z_nite,
	/// which aren't in the archive: these are guesses, to tune in game.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class SpinningSettings : SettingsMetaCategory {
		[SavedMember]
		public static SpinningSettings instance = new SpinningSettings();

		[LoadingInitializer]
		public SpinningSettings() {
		}

		[SaveableData]
		public int skillPerBatch = 250;

		[SaveableData]
		public int yieldPerFibre = 3;

		[SaveableData]
		public int yieldPerThread = 1;

		[SaveableData]
		public double tailoringAdvRate = 700;

		[SaveableData]
		public double secondsToMake = 3;
	}
}
