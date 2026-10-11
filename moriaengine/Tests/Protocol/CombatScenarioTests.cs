using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// First "in-game" scenario test (as opposed to protocol-handshake-only tests like
	/// LoginHandshakeTests): goes all the way from login through character creation and into a
	/// real speech command, which triggers scripts/Tests/CombatScenarioTests.cs's
	/// TestTitanCombat server-side - spawning two titans and ordering one to kill the other via
	/// the real NPCAiTimer/WeaponSkillTargetQueuePlugin path. Asserts on the server's own log
	/// output (GameServerPairFixture.WaitForGameOutput) rather than visual in-game inspection,
	/// since the server runs as a separate OS process this test has no direct object access to.
	///
	/// Uses its own collection (and so its own GameServerPairFixture instance / server-process
	/// pair / "protocoltest" account) rather than GameServerPairCollection's shared one, because
	/// this test creates a character on that account - which would break
	/// LoginHandshakeTests.LoginThenCharacterListRoundTrip's "fresh account has no characters"
	/// assumption if the two ever shared state. AssemblyInfo.cs disables cross-collection test
	/// parallelism so this fixture's fixed ports (2593/2595) are free by the time it starts.
	/// </summary>
	[Collection(CombatScenarioCollection.Name)]
	public class CombatScenarioTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";
		private const string TestCharName = "Tester";

		public CombatScenarioTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void TitanOrderedToKillAnotherTitanDealsDamage() {
			int redirectPort = WaitForServerListingAndGetRedirectPort();

			bool sawResult;
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				string[] characters = game.ReadCharacterList();
				Assert.All(characters, name => Assert.Null(name)); // this fixture's account is used by nothing else

				game.SendCreateCharacter(TestCharName);
				game.SendSpeech(".TestTitanCombat");

				// Keep the connection open while combat plays out server-side - disconnecting
				// right after SendSpeech doesn't stop the scenario (the titans are independent
				// server-side NPCs), but it does make the server log benign-but-noisy
				// "send after disconnect" errors for the queued world-entry packets, which only
				// gets in the way of reading the output when a real assertion failure happens.
				sawResult = this.fixture.WaitForGameOutput("TITAN_COMBAT_TEST: PASS", TimeSpan.FromSeconds(45));
			}

			Assert.True(sawResult, "Expected the titan to deal damage within 45s of the kill order. Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private readonly GameServerPairFixture fixture;

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 20; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);

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
					Thread.Sleep(250);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CombatScenarioCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CombatScenarioPair";
	}
}
