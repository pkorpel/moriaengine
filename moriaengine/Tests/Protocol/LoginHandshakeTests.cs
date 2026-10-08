using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Exercises the real login->game-server->character-list handshake against a genuinely
	/// booted AuxiliaryServer+GameCore pair (GameServerPairFixture) - the full-boot tier from
	/// the test-harness plan, used here ahead of its originally-sketched Phase 12 because
	/// Phase 11 specifically needs a live pair to test the network path against.
	///
	/// Account creation here is in-memory only for the lifetime of the spawned GameCore
	/// process (killed, not gracefully shut down, at the end of the fixture) - it never writes
	/// back to the repo's tracked saves/ directory, so repeated runs don't accumulate test
	/// accounts in committed save data.
	/// </summary>
	[Collection(GameServerPairCollection.Name)]
	public class LoginHandshakeTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		public LoginHandshakeTests(GameServerPairFixture servers) {
			// Constructor parameter is required by xUnit's ICollectionFixture wiring even
			// though this class doesn't otherwise need to hold onto it.
		}

		[Fact]
		public void LoginThenCharacterListRoundTrip() {
			int redirectPort = WaitForServerListingAndGetRedirectPort();

			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);

				string[] characters = game.ReadCharacterList();
				Assert.Equal(5, characters.Length);
				// A fresh test account has no characters - this holds whether this run just
				// created the account or is re-logging into one from an earlier run, since
				// nothing in this test ever creates a character.
				Assert.All(characters, name => Assert.Null(name));
			}
		}

		/// <summary>
		/// GameCore announces itself to AuxiliaryServer (the thing that makes it show up in
		/// the server list) via a timer fired right as GameCore reaches "Init done." - not
		/// synchronously as part of that boot message - so immediately after the fixture's
		/// boot wait there's a brief real race before AuxiliaryServer's own server list
		/// reflects it. Retry a few times rather than guessing a fixed delay.
		/// </summary>
		private static int WaitForServerListingAndGetRedirectPort() {
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
}
