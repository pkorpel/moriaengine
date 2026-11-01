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

	/// <summary>#962 - a skill's stages: what smithing wants at hand, how long making takes.</summary>
	[Collection(CraftStagesCollection.Name)]
	public class CraftStagesTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);
		private readonly GameServerPairFixture fixture;

		public CraftStagesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static uint Uid(string report, string name) {
			return uint.Parse(Regex.Match(report, name + @"=(\d+)").Groups[1].Value);
		}

		private static string WaitForMessage(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (true) {
				string message = client.ReadSystemMessage(deadline - DateTime.UtcNow);
				if (Regex.IsMatch(message, pattern)) {
					return message;
				}
			}
		}

		private const string NoForge = "forge|vyhne";
		private const string NoHammer = "hammer|kladivo";

		[Fact]
		public void StagesAreSpheres() {
			using var client = CommandPermissionTests.Connect("craftstage", "Smith Tester");
			string strokes = this.Ask(client, ".TestStageStrokes", "STAGE_STROKES:");
			Assert.Contains("Blacksmith=3-6@1/1", strokes);
			Assert.Contains("Carpentry=3-5@1/1", strokes);
			Assert.Contains("Fletching=1-1@2/2", strokes);
			Assert.Contains("Tailoring=1-1@2.5/2.5", strokes);
			Assert.Contains("Tinkering=1-1@2.5/2.5", strokes);
			Assert.Contains("Alchemy=1-1@2/1", strokes);

			string setup = this.Ask(client, ".TestStageSetup", "STAGE_SETUP:");
			uint ingots = Uid(setup, "ingots"), hammer = Uid(setup, "hammer");
			client.SendDoubleClick(Uid(setup, "pack")); //a client can only reach into what it has open
			Thread.Sleep(300);

			client.SendDoubleClick(ingots);
			WaitForMessage(client, NoHammer);

			//the hammer goes in the hand and asks what for
			client.SendDoubleClick(hammer);
			Thread.Sleep(300);
			client.SendTargetUid(ingots);
			WaitForMessage(client, NoForge);

			this.Ask(client, ".TestStageForge \"4\"", "STAGE_FORGE:");
			client.SendDoubleClick(ingots);
			WaitForMessage(client, NoForge);

			this.Ask(client, ".TestStageForge \"3\"", "STAGE_FORGE:");
			client.SendDoubleClick(ingots);
			Assert.Contains(client.ReadGumpDialogWithStrings(Wait).Strings, line => line.Contains("Blacksmithing", StringComparison.Ordinal));

			client.SendDoubleClick(hammer);
			Thread.Sleep(300);
			client.SendTargetUid(ingots);
			Assert.Contains(client.ReadGumpDialogWithStrings(Wait).Strings, line => line.Contains("Blacksmithing", StringComparison.Ordinal));

			//at least 3 strokes of 1 s, whether it fails or not
			var watch = System.Diagnostics.Stopwatch.StartNew();
			this.Ask(client, ".TestStageMake", "STAGE_STARTED:");
			WaitForMessage(client, "You create|You have failed");
			Assert.True(watch.Elapsed.TotalSeconds > 2.8, "took " + watch.Elapsed);

			//the forge goes mid work: it stops and nothing's made
			string before = this.Ask(client, ".TestCraftMade", "CRAFT_MADE:");
			before = before.Substring(before.IndexOf("daggers=", StringComparison.Ordinal));
			this.Ask(client, ".TestStageMake", "STAGE_STARTED:");
			Thread.Sleep(300);
			this.Ask(client, ".TestStageForge \"0\"", "STAGE_FORGE:");
			WaitForMessage(client, NoForge);
			Thread.Sleep(1500);
			Assert.Contains(before, this.Ask(client, ".TestCraftMade", "CRAFT_MADE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CraftStagesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CraftStages";
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
