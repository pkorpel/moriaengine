using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #711 - proves c_titan_ranger's computed attack now beats c_titan's flat armor (was a
	/// deterministic 0-damage wall before NPCs got the same Tactics/Anatomy/ArmsLore/Str attack
	/// scaling the player branch already had) - see
	/// scripts/Tests/NpcAttackStatScalingTests.cs's own doc comment for the full writeup.
	/// </summary>
	[Collection(NpcAttackStatScalingCollection.Name)]
	public class NpcAttackStatScalingTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcAttackStatScalingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RangerAttackNowBeatsTitanArmor() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "SwingArgsChecker")) {
				client.SendSpeech(".TestReportRangerVsTitanSwingArgs");
				Assert.True(this.fixture.WaitForGameOutput("RANGER_VS_TITAN_SWING_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RANGER_VS_TITAN_SWING_TEST:");

				double damageAfterAC = ParseDoubleField(line, "damageAfterAC=");
				Assert.True(damageAfterAC > 0, "Expected real damage to get through titan's armor, got: " + line);
			}
		}

		private static double ParseDoubleField(string line, string prefix) {
			int start = line.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
			int end = start;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '.') || (line[end] == '-'))) {
				end++;
			}
			return double.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
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

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);
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

	[CollectionDefinition(NpcAttackStatScalingCollection.Name, DisableParallelization = true)]
	public class NpcAttackStatScalingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcAttackStatScalingPair";
	}
}
