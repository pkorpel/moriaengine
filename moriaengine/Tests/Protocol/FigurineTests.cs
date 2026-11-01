using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>A shrunk pet looks like the pet: Sphere's ICON.</summary>
	[Collection(FigurineCollection.Name)]
	public class FigurineTests {
		private const string Account = "figurinetest";

		private readonly GameServerPairFixture fixture;

		public FigurineTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void AShrunkPetLooksLikeThePet() {
			using var client = CommandPermissionTests.Connect(Account, "Figurine Tester");
			Assert.Contains("broken=2 ", this.Ask(client, ".TestFigurineIcons", "FIGURINE_ICONS:"));
			foreach (string pet in new[] { "c_ostard_zostrich i_pet_ostard_zostrich", "c_horse_tan i_pet_horse_tan", "c_ostard_forest i_pet_ostard_forest" }) {
				string figurine = this.Ask(client, ".TestFigurineOf \"" + pet + "\"", "FIGURINE_OF:");
				Assert.Contains("matches=True notLlama=True color=33", figurine);
			}
			Assert.Contains("matches=True", this.Ask(client, ".TestFigurineOf \"c_llama i_pet_llama\"", "FIGURINE_OF:"));
			Assert.Contains("model=8438", this.Ask(client, ".TestFigurineNoIcon", "FIGURINE_NOICON:"));
		}
	}

	/// <summary>A figurine's look through a real save and restart.</summary>
	[Collection(FigurineRestartCollection.Name)]
	public class FigurineRestartTests {
		private const string Account = "figurinerestart";

		[Fact]
		public void AFigurineKeepsItsLook() {
			using (var first = new GameServerPairFixture()) {
				using (var client = CommandPermissionTests.Connect(Account, "Figurine Saver")) {
					CommandPermissionTests.Ask(first, client, ".TestFigurineOf \"c_ostard_zostrich i_pet_ostard_zostrich\"", "FIGURINE_OF:");
					CommandPermissionTests.Ask(first, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = CommandPermissionTests.Connect(Account, "Figurine Loader")) {
					Assert.Contains("notLlama=True", CommandPermissionTests.Ask(second, client, ".TestFigurineLoaded", "FIGURINE_LOADED:"));
				}
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class FigurineCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Figurine";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class FigurineRestartCollection {
		public const string Name = "FigurineRestart";
	}
}
