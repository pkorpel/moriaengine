using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - recipes are the shard's live ones, nastaveni_cm_*, and all of them parse.</summary>
	[Collection(CraftRecipeCollection.Name)]
	public class CraftRecipeTests {
		private readonly GameServerPairFixture fixture;

		public CraftRecipeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RecipesAreTheLiveOnes() {
			using var client = CommandPermissionTests.Connect("craftrecipes", "Recipe Tester");
			string all = CommandPermissionTests.Ask(this.fixture, client, ".TestRecipesParse", "RECIPES:");
			Assert.Contains("broken=0", all);

			string dagger = CommandPermissionTests.Ask(this.fixture, client, ".TestRecipeOf \"i_dagger_copper\"", "RECIPE_OF:");
			Assert.Contains("2 ", dagger);
			Assert.Contains("ingot", dagger);
			string boots = CommandPermissionTests.Ask(this.fixture, client, ".TestRecipeOf \"i_boots_calf\"", "RECIPE_OF:");
			Assert.Contains("8 ", boots);
			Assert.Contains("2 ", boots);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CraftRecipeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CraftRecipe";
	}
}
