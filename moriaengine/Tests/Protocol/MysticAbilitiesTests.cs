using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#868 - Mystic's Astral Power and attack switch, on an account of their own
	/// (an account holds 5 characters).</summary>
	[Collection(MysticAbilitiesCollection.Name)]
	public class MysticAbilitiesTests {
		private const string Account = "mysticabilitiestest";
		private const string Password = "mysticabilitiestest";

		private readonly GameServerPairFixture fixture;

		public MysticAbilitiesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>#868 - Astral Power: on and off, its strikes do less but carry the effects;
		/// the switch to lightning. Level 60 is a 95% chance, so 50 tries always see one.</summary>
		[Fact]
		public void AstralPowerChangesTheStrike() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticAstralChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMysticAbilitiesReport");
				Assert.True(this.fixture.WaitForGameOutputSince("MYSTIC_ABILITIES:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MYSTIC_ABILITIES:");

				Assert.Contains("inactive=False", line);
				Assert.Contains("active=True", line);
				Assert.Contains("off=False", line);
				Assert.Contains("lightning=True", line);
				double plain = ExtractDouble(line, "plain");
				Assert.True(plain > 0, line);
				Assert.Equal(13.0 / 16.0, ExtractDouble(line, "special") / plain, 1); //the 0-13 range, not 0-16
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
	public class MysticAbilitiesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MysticAbilitiesPair";
	}
}
