using System;
using System.Text.RegularExpressions;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's systems/mistri.scp.</summary>
	[Collection(CraftMasterCollection.Name)]
	public class CraftMasterTests {
		private readonly GameServerPairFixture fixture;

		public CraftMasterTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void OnlyWhatYouMadeCountsAndLogsPayBySkill() {
			using var client = CommandPermissionTests.Connect("mastertest", "Master Tester");
			string line = CommandPermissionTests.Ask(this.fixture, client, ".TestMasterReady", "MASTER_READY:");
			Assert.Contains("foreign=0", line);
			Assert.Contains("mine=118", line); //700 + 500 skill at 100 and 130, 38 ingots at 230, quality 100
			Assert.Contains("logs=460", line); //10 at nastaveni_craftexp_i_log_obsidian 46
			Assert.Contains("value=1900", line);
			Assert.Contains("factor=1000", line);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CraftMasterCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CraftMasters";
	}
}
