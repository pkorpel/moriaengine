using System;
using System.Text.RegularExpressions;
using System.Threading;
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

	/// <summary>#962 - the menus hold what Erebor's scripts put in them, and a real player makes something from one.</summary>
	[Collection(CraftMenuCollection.Name)]
	public class CraftMenuTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);
		private readonly GameServerPairFixture fixture;

		public CraftMenuTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void APlayerMakesSomethingFromTheMenu() {
			using var client = CommandPermissionTests.Connect("craftmenu", "Menu Tester");
			//every item of every menu can be made, and metal and wood look it. Spruce is undyed on Erebor, as plain iron things are
			Assert.Contains("coloured=", this.Ask(client, ".TestCraftColours", "CRAFT_COLOURS:"));
			Assert.Contains("CRAFT_UNCREATABLE: 0 ", this.fixture.GameOutputSnapshot());

			//a bundle unpacks, keeping who made it
			string bundles = this.Ask(client, ".TestBundleMake \"i_arrow_shafts_copper_2 3\"", "BUNDLE_MADE:");
			client.SendDoubleClick(uint.Parse(Regex.Match(bundles, @"pack=(\d+)").Groups[1].Value)); //a client can only reach into what it has open
			Thread.Sleep(200);
			uint shafts = uint.Parse(Regex.Match(bundles, @"uid=(\d+)").Groups[1].Value);
			client.SendDoubleClick(shafts);
			Thread.Sleep(400);
			Assert.Contains("amount=6 signed=True", this.Ask(client, ".TestBundleReport \"i_arrow_shaft_copper\"", "BUNDLE_LOOSE:"));
			uint piercing = uint.Parse(Regex.Match(this.Ask(client, ".TestBundleMake \"i_arrow_piercing_multi_copper 1\"", "BUNDLE_MADE:"), @"uid=(\d+)").Groups[1].Value);
			client.SendDoubleClick(piercing);
			Thread.Sleep(400);
			Assert.Contains("amount=20", this.Ask(client, ".TestBundleReport \"i_arrow_piercing_copper\"", "BUNDLE_LOOSE:"));

			string menus = this.Ask(client, ".TestCraftMenus", "CRAFT_MENUS:");
			Assert.Contains("first=Copper/Spruce", menus); //Sphere shows the last in first
			Assert.Contains("last=Ingots", menus);
			Assert.DoesNotContain("=0", menus);

			//only what they could make shows
			string smith = this.Ask(client, ".TestCraftSmith", "CRAFT_SMITH:");
			Assert.Contains("gm=False", smith);
			Assert.Contains("Copper/Spruce;", smith);
			Assert.DoesNotContain("Mithril", smith);

			client.SendSpeech(".TestCraftOpenMenu");
			var gump = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains(gump.Strings, line => line.Contains("Copper/Spruce", StringComparison.Ordinal));
			Assert.DoesNotContain(gump.Strings, line => line.Contains("Mithril", StringComparison.Ordinal));

			//5 tries at 2 ingots each: something comes of it and ingots go
			this.Ask(client, ".TestCraftDaggers", "CRAFT_STARTED:");
			Thread.Sleep(14000);
			string made = this.Ask(client, ".TestCraftMade", "CRAFT_MADE:");
			Assert.DoesNotContain("daggers=0", made);
			Assert.DoesNotContain("ingots=40", made);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CraftMenuCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CraftMenu";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CraftingRulesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CraftingRules";
	}
}
