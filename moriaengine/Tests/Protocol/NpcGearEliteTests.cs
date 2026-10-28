using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #793 - human NPCs' @NPCRestock ITEMNEWBIE clothing is worn and never lootable;
	/// #794 - Erebor's elite monsters: the transform, bosses refusing it, the spawn-gem roll,
	/// gold following the raised experience, regeneration and the escape through a gate.
	/// </summary>
	[Collection(NpcGearEliteCollection.Name)]
	public class NpcGearEliteTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcGearEliteTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void HumanNpcWearsItsRestockClothingAndItIsNotLootable() {
			string line = this.Run("GearBanker", null, ".TestNpcRestockGear", "NPC_GEAR:");
			Assert.True(Value(line, "worn=") >= 5, "the banker should be dressed: " + line);
			Assert.Equal(0, Value(line, "notNewbie="));
			Assert.Contains("shirt=True", line);     //ITEMNEWBIE=i_shirt_fancy
			Assert.Contains("light=True", line);     //ITEMNEWBIE=RANDOM_LIGHT, a pick list
			Assert.Equal(0, Value(line, "corpseItems="));
			Assert.Contains("namedRobe=True", line); //ITEMNEWBIE=.../NAME=Roba s kapi
		}

		[Fact]
		public void EliteTransformRaisesTheMonsterAndItsRewards() {
			string line = this.Run("EliteGolem", null, ".TestEliteMonster", "ELITE:");
			Assert.Contains("transformed=True", line);
			Assert.Contains("again=False", line);
			Assert.Equal(Value(line, "expectedPower="), Value(line, "power="));
			Assert.InRange(Value(line, "power="), 500, 900);
			Assert.Equal(Value(line, "str0=") * 125 / 100, Value(line, "str1="));
			Assert.Equal(Value(line, "expectedExp="), Value(line, "exp1="));
			Assert.Equal(Value(line, "expectedGold="), Value(line, "gold=")); //gold follows the raised experience
			Assert.True(Value(line, "diamonds=") >= Value(line, "power=") / 100, line); //random_JEWEL can add more
			Assert.True(Value(line, "reagents=") >= 2 * (Value(line, "power=") / 10), line);
			Assert.True(Value(line, "resistPoison=") >= Value(line, "power="), line);
			Assert.True(Value(line, "resistSummon=") >= 970, line);
			Assert.Contains("regenTimer=True", line);
			Assert.Contains("bossTransformed=False", line);
			Assert.Contains("spawnedElite=True", line);
			Assert.Contains("unspawnedElite=False", line);
			Assert.Contains("deniedElite=False", line);
		}

		[Fact]
		public void EliteRegeneratesAndLeavesThroughAGate() {
			//7 s = 3 regen ticks: more than one heal proves the timer keeps going
			string regen = this.Run("EliteRegen", new[] { ".TestEliteMonster", "ELITE:" }, ".TestEliteRegenCheck", "ELITE_REGEN:", 7000);
			Assert.True(Value(regen, "now=") - Value(regen, "before=") >= 2 * Value(regen, "perTick="), "the elite should keep regenerating: " + regen);
			Assert.Contains("timerAlive=True", regen); //a one-shot BoundTimer would be gone after its first tick

			int mark = this.fixture.GameOutputSnapshot().Length;
			string escape = this.Run("EliteEscape", new[] { ".TestEliteEscapeStart", "ELITE_ESCAPE_START:" }, ".TestEliteEscapeCheck", "ELITE_ESCAPE:", 7000);
			string start = LastLine(this.fixture.GameOutputSnapshot(), "ELITE_ESCAPE_START:");
			Assert.Contains("timer=True", start);
			Assert.Contains("interrupted=True", start);
			Assert.Contains("gone=True", escape);
			Assert.Contains("left through its gate", this.fixture.GameOutputSnapshot().Substring(mark));
		}

		private string Run(string charName, string[] first, string command, string marker, int waitMs = 0) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter(charName);
				if (first != null) {
					int firstMark = this.fixture.GameOutputSnapshot().Length;
					game.SendSpeech(first[0]);
					Assert.True(this.fixture.WaitForGameOutputSince(first[1], firstMark, TimeSpan.FromSeconds(20)),
						"No '" + first[1] + "' line. Game output:\n" + this.fixture.GameOutputSnapshot());
					Thread.Sleep(waitMs);
				}
				int mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(command);
				Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(20)),
					"No '" + marker + "' line. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
			return LastLine(this.fixture.GameOutputSnapshot(), marker);
		}

		private static string LastLine(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "No '" + marker + "' in the output.");
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
	public class NpcGearEliteCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcGearElite";
	}
}
