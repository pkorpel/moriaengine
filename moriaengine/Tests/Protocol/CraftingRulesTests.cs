using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - how hard an item is, and what failing costs, as Sphere's Skill_MakeItem.</summary>
	[Collection(CraftingRulesCollection.Name)]
	public class CraftingRulesTests {
		private readonly GameServerPairFixture fixture;

		public CraftingRulesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void DifficultyAndLossAreSpheres() {
			using var client = CommandPermissionTests.Connect("craftingrules", "Craft Tester");
			Assert.Contains("dagger=150 boots=750", this.Ask(client, ".TestCraftDifficulty", "CRAFT_DIFFICULTY:"));

			//a recipe of 2: 49% of 2 rounds down to nothing, 50% takes one
			Assert.Contains("left=19", this.Ask(client, ".TestCraftFailLoss \"0\"", "CRAFT_LOSS:"));
			Assert.Contains("left=19", this.Ask(client, ".TestCraftFailLoss \"49\"", "CRAFT_LOSS:"));
			Assert.Contains("left=18", this.Ask(client, ".TestCraftFailLoss \"50\"", "CRAFT_LOSS:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CraftingRulesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CraftingRules";
	}
}
