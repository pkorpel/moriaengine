using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#892 - the numbers of Erebor's towns (systems/starostove.scp), the "Towns" category of
	/// .settings. Defaults are Erebor's, and a quiet shard will want some of them lower.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class TownSettings : SettingsMetaCategory {

		[SavedMember]
		public static TownSettings instance = new TownSettings();

		[LoadingInitializer]
		public TownSettings() {
		}

		/// <summary>What it costs to become a citizen.</summary>
		[SaveableData]
		public int citizenshipFee = 100000;

		[SaveableData]
		public int citizenMinLevel = 20;

		/// <summary>Player.SkillRank a citizen needs: 3 is Grandmaster.</summary>
		[SaveableData]
		public int citizenMinRank = 3;

		/// <summary>The tax of a town nobody has set one for yet.</summary>
		[SaveableData]
		public int defaultTax = 10000;

		/// <summary>The tax can't go below this much per citizen.</summary>
		[SaveableData]
		public int minTaxPerCitizen = 1500;

		[SaveableData]
		public int maxTax = 500000;

		/// <summary>How often the tax falls due.</summary>
		[SaveableData]
		public int taxPeriodDays = 30;

		/// <summary>How long a mayor must wait to change the tax again.</summary>
		[SaveableData]
		public int taxChangeDays = 7;

		/// <summary>What moving one's home to a town costs anyone but its citizens. It goes to the treasury.</summary>
		[SaveableData]
		public int homeChangeFee = 100000;

		public override string ToString() {
			return "Towns";
		}
	}
}
