using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#866 - the thief's rebound and stance, and the magic reflex and block.</summary>
	[Collection(ThiefDefenceCollection.Name)]
	public class ThiefDefenceTests {
		private const string Account = "thiefdefencetest";
		private const string Password = "thiefdefencetest";

		private readonly GameServerPairFixture fixture;

		public ThiefDefenceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>#866 - rebound with a cutlass and all points: 30%, the stance doubles it, a
		/// blow on a monster loses 30%, and the magic reflex and block stop blows.</summary>
		[Fact]
		public void ReboundVigilanceAndMagicDefence() {
			using (var client = this.ConnectLoginAndCreateCharacter("ThiefDefenceChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestThiefDefenceReport");
				Assert.True(this.fixture.WaitForGameOutputSince("THIEF_DEFENCE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "THIEF_DEFENCE:");

				Assert.InRange(ExtractDouble(line, "plain"), 2700, 3300);    //of 10000
				Assert.InRange(ExtractDouble(line, "vigilant"), 5600, 6400);
				Assert.Equal(1, ExtractDouble(line, "weakens"));
				Assert.Equal(0, ExtractDouble(line, "fromPlayer"));
				Assert.InRange(ExtractDouble(line, "reflected"), 4000, 6000); //50% of 10000
				Assert.Equal(1, ExtractDouble(line, "blocked"));
			}
		}

		private static double ExtractDouble(string line, string key) {
			string marker = key + "=";
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = line.IndexOf(' ', start);
			if (end < 0) {
				end = line.Length;
			}
			return double.Parse(line.Substring(start, end - start), CultureInfo.InvariantCulture);
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
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
	public class ThiefDefenceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ThiefDefencePair";
	}
}
