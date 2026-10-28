using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #791 - monster loot from real converted content: "Loot =" lines (Erebor's @NPCRestock) end
	/// up in the backpack and the corpse; "Equip = i_monster_create" sets the gold from the
	/// monster's experience (Erebor's creaturestart) instead of creating a do-nothing item; nested
	/// loot templates don't delete the backpack; "{0 1}" amounts never leave empty stacks.
	/// </summary>
	[Collection(MonsterLootCollection.Name)]
	public class MonsterLootTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public MonsterLootTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void OrcGetsGoldFromExperienceAndItsLootFallsToTheCorpse() {
			string line = this.Run("LootOrc", ".TestOrcLoot", "MONSTER_LOOT:");
			int exp = Value(line, "exp=");
			Assert.True(exp > 0, "c_orc should roll an experience reward: " + line);
			Assert.Equal(Value(line, "expectedGold="), Value(line, "gold="));
			Assert.Contains("boots=True", line);              //loot_ORC
			Assert.Contains("monsterCreateItem=False", line); //a script marker, not equipment
			Assert.Equal(Value(line, "gold="), Value(line, "corpseGold="));
			Assert.Contains("corpseBoots=True", line);
		}

		[Fact]
		public void NestedLootTemplatesKeepTheBackpack() {
			string line = this.Run("LootCyclops", ".TestCyclopsLoot", "CYCLOPS_LOOT:");
			Assert.Contains("backpackAlive=True", line);
			Assert.True(Value(line, "items=") > 0, line);
		}

		[Fact]
		public void ZeroRolledAmountsCreateNoItems() {
			string line = this.Run("LootOgres", ".TestNoZeroAmountLoot", "ZERO_LOOT:");
			Assert.True(Value(line, "items=") > 0, line);
			Assert.Equal(0, Value(line, "zeroAmount="));
		}

		private string Run(string charName, string command, string marker) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			int mark = this.fixture.GameOutputSnapshot().Length;
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter(charName);
				game.SendSpeech(command);
				Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(20)),
					"No '" + marker + "' line. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
			string output = this.fixture.GameOutputSnapshot();
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', idx);
			return output.Substring(idx, (end < 0 ? output.Length : end) - idx);
		}

		private static int Value(string line, string key) {
			int idx = line.IndexOf(key, StringComparison.Ordinal);
			Assert.True(idx >= 0, "No '" + key + "' in: " + line);
			int start = idx + key.Length, end = start;
			while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '-')) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MonsterLootCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterLoot";
	}
}
