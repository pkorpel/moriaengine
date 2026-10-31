using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#882 - Erebor's rules for spending skill and stat points.</summary>
	[Collection(StatSkillPortCollection.Name)]
	public class StatSkillPortTests {
		private const string Account = "statskillporttest";
		private const string Password = "statskillporttest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public StatSkillPortTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SkillClicksCostAndStopAsOnErebor() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillClickChecker")) {
				this.Ask(client, ".TestSkillPortProfession", "SKILLPORT_READY:");
				this.AssertClick(client, 300, 5, "value=350 points=4 refusal=none"); //under 50.0: 1 point
				this.AssertClick(client, 263, 5, "value=250 points=5 refusal=Na_dalsi_uroven"); //rounds to 25.0, a master's step
				this.AssertClick(client, 500, 5, "value=500 points=5 refusal=Na_dalsi_uroven");
				this.AssertClick(client, 550, 5, "value=600 points=3 refusal=none"); //50.0 to 75.0: 2 points
				this.AssertClick(client, 550, 1, "value=550 points=1 refusal=Na_zvyseni");
				this.AssertClick(client, 800, 5, "value=850 points=2 refusal=none"); //over 75.0: 3 points
				this.AssertClick(client, 1000, 5, "value=1000 points=5 refusal=V_teto_dovednosti");
			}
		}

		private void AssertClick(TestGameClient client, int value, int points, string expected) {
			this.Ask(client, ".TestSkillPortRaise \"" + value + " " + points + "\"", "SKILLPORT_RAISE:");
			Assert.Contains(expected, this.Line("SKILLPORT_RAISE:"));
		}

		[Fact]
		public void StatsAreLimitedByBudgetMaximumAndMagic() {
			using (var client = this.ConnectLoginAndCreateCharacter("StatRuleChecker")) {
				this.Ask(client, ".TestSkillPortProfession", "SKILLPORT_READY:");
				this.Ask(client, ".TestSkillPortStat \"0 100\"", "SKILLPORT_STAT:");
				Assert.Contains("spent=False", this.Line("SKILLPORT_STAT:"));

				this.Ask(client, ".TestSkillPortStat \"40 100\"", "SKILLPORT_STAT:");
				Assert.Contains("spent=True", this.Line("SKILLPORT_STAT:"));

				this.Ask(client, ".TestSkillPortStat \"40 180\"", "SKILLPORT_STAT:");
				Assert.Contains("refusal=Dosahl_jsi_maximalni_hodnoty.", this.Line("SKILLPORT_STAT:"));

				this.Ask(client, ".TestSkillPortStat \"40 bless\"", "SKILLPORT_STAT:");
				Assert.Contains("refusal=Jsi_pod_vlivem_magie", this.Line("SKILLPORT_STAT:"));
			}
		}

		[Fact]
		public void DeathRoundsSkillsBoughtWithPointsOnly() {
			using (var client = this.ConnectLoginAndCreateCharacter("DeathRoundChecker")) {
				this.Ask(client, ".TestSkillPortProfession", "SKILLPORT_READY:");
				this.Ask(client, ".TestSkillPortDeath", "SKILLPORT_DEATH:");
				Assert.Contains("tactics=400 mining=412", this.Line("SKILLPORT_DEATH:"));
			}
		}

		private void Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
		}

		private string Line(string marker) {
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(index >= 0, "Could not find '" + marker + "'");
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}

						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						Assert.True(redirectPort > 0, "Expected a real port in the login redirect.");
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
	public class StatSkillPortCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "StatSkillPortPair";
	}
}
