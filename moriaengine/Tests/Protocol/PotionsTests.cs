using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#927 - every potion, by what a double click does; bombs, shrink and the keg by a real client.</summary>
	[Collection(PotionsCollection.Name)]
	public class PotionsTests {
		private const string Account = "potionstest";
		private readonly GameServerPairFixture fixture;

		public PotionsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryPotionDoesWhatItDoesOnErebor() {
			using var client = this.Connect("Potion Drinker");
			this.Clean(this.Ask(client, ".TestPotionsRules", "POTIONS_RULES:", 60));
		}

		private void Clean(string result) {
			List<string> found = new List<string>();
			foreach (string line in this.fixture.GameOutputSnapshot().Split('\n')) {
				if (line.Contains("POTIONS_FAIL:") || line.Contains("ERROR: (<command>")) {
					found.Add(line);
				}
			}
			Assert.True(result.Contains("fails=0"), result + "\n" + string.Join("\n", found));
		}

		[Fact]
		public void ABombHurtsAroundAndIsNobodysCrime() {
			using var client = this.Connect("Bomb Setter");
			this.Clean(this.Ask(client, ".TestPotionsBomb", "POTIONS_BOMB:"));
			this.Clean(this.Ask(client, ".TestPotionsDispelBomb", "POTIONS_DISPEL:"));

			//perception shows the hitpoints of what's hit
			this.Ask(client, ".TestPotionsSight", "POTIONS_SIGHT:");
			Assert.NotNull(MasterQuestTests.TryReadMessageContaining(client, "%]", TimeSpan.FromSeconds(5)));
		}

		[Fact]
		public void AThrownBombLandsAndCountsDown() {
			using var client = this.Connect("Bomb Thrower");
			string given = this.Ask(client, ".TestPotionsGiveBomb", "POTIONS_GIVEN:");
			client.SendSpeech(".throwexplosion");
			Thread.Sleep(400);
			client.SendTargetGround((int) Number(given, "x="), (int) Number(given, "y="), (int) Number(given, "z="));
			Thread.Sleep(1500);
			Assert.Contains("left=1 onGround=1 armed=1", this.Ask(client, ".TestPotionsThrown", "POTIONS_THROWN:"));
			Thread.Sleep(3500);
			Assert.Contains("left=1 onGround=0 armed=0", this.Ask(client, ".TestPotionsThrown", "POTIONS_THROWN:"));
		}

		[Fact]
		public void APetShrinksAndComesBack() {
			using var client = this.Connect("Pet Shrinker");
			string start = this.Ask(client, ".TestPotionsShrinkStart", "POTIONS_SHRINK:");
			client.SendDoubleClick(Number(start, "pack="));
			Thread.Sleep(300);
			client.SendDoubleClick(Number(start, "potion="));
			Thread.Sleep(300);
			client.SendTargetUid(Number(start, "pet="));
			Thread.Sleep(300);
			string figurine = this.Ask(client, ".TestPotionsFigurine", "POTIONS_FIGURINE:");
			Assert.Contains("petGone=True", figurine);
			Assert.Contains("named=Micinka", figurine);
			client.SendDoubleClick(Number(figurine, "figurine="));
			Thread.Sleep(300);
			Assert.Contains("found=True name=Micinka", this.Ask(client, ".TestPotionsPetBack", "POTIONS_PETBACK:"));
		}

		[Fact]
		public void AKegHoldsOnePotionAndFillsBottles() {
			using var client = this.Connect("Keg Filler");
			string keg = this.Ask(client, ".TestPotionsKeg", "POTIONS_KEG:");
			client.SendDoubleClick(Number(keg, "pack="));
			Thread.Sleep(300);
			client.SendDoubleClick(Number(keg, "keg="));
			Thread.Sleep(300);
			client.SendTargetUid(Number(keg, "heals="));
			Thread.Sleep(300);
			Assert.Contains("count=5 heals=0 bottles=8", this.Ask(client, ".TestPotionsKegReport", "POTIONS_KEGSTATE:"));

			//no mixing
			client.SendDoubleClick(Number(keg, "keg="));
			Thread.Sleep(300);
			client.SendTargetUid(Number(keg, "other="));
			Thread.Sleep(300);
			Assert.Contains("count=5 ", this.Ask(client, ".TestPotionsKegReport", "POTIONS_KEGSTATE:"));

			//bottles draw it back
			uint bottles = Number(this.Ask(client, ".TestPotionsKegBottles", "POTIONS_BOTTLES:"), "uid=");
			client.SendDoubleClick(Number(keg, "keg="));
			Thread.Sleep(300);
			client.SendTargetUid(bottles);
			Thread.Sleep(300);
			Assert.Contains("count=0 heals=5 bottles=3", this.Ask(client, ".TestPotionsKegReport", "POTIONS_KEGSTATE:"));
		}

		private static uint Number(string line, string key) {
			int start = line.IndexOf(" " + key, StringComparison.Ordinal) + key.Length + 1;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private string Ask(TestGameClient client, string command, string marker, int seconds = 10) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(seconds)), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is IOException)) {
					game.Dispose();
					lastFailure = lastFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as '" + charName + "' after retrying.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PotionsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Potions";
	}

	/// <summary>#927 - a save with everything of theirs running.</summary>
	[Collection(PotionsRestartCollection.Name)]
	public class PotionsRestartTests {
		private const string Account = "potionsrestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void ASaveHoldsThemAll() {
			using (var first = new GameServerPairFixture()) {
				using var client = Connect("Potion Saver");
				Ask(first, client, ".TestPotionsForSave", "POTIONS_SAVED:");
				Ask(first, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using var client = Connect("Potion Loader");
				Assert.Contains("fuses=1 kegs=1 figurines=1 effects=3", Ask(second, client, ".TestPotionsAfterRestart", "POTIONS_LOADED:"));
			}
		}

		private static TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + speech + ":\n" + fixture.GameOutputSnapshot());
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PotionsRestartCollection {
		public const string Name = "PotionsRestart";
	}
}
