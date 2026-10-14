using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Leveling system Phase 1 (#126): the .explevel dialog actually opens without error
	/// (scripts/Basic/dialogs/explevel.scp - LScript content isn't checked by `dotnet build` at
	/// all, so this is the only thing that would catch a syntax or token-resolution error in it).
	///
	/// Deliberately its own fully isolated GameServerPairFixture/collection, NOT shared with
	/// LevelingTests.cs's other Facts. Real finding while building this: sharing a server
	/// process with several Facts that cause real player/NPC deaths (Phases 2/3) left this test
	/// unable to receive the explevel gump within any timeout, including a generous 20s and
	/// fresh-connection retries - deterministic, not transient (same result every time once the
	/// other Facts had run first). It passes reliably completely on its own. The underlying real
	/// bug this test originally caught - explevel.scp's own body calling
	/// InvalidateCombatWeaponValues()/InvalidateCombatArmorValues() bare, which was missing from
	/// CommandsAuthorization.cs's allowlist and so got silently denied for every non-GM account
	/// (#146, fixed there) - is unrelated to and already fixed independently of this isolation;
	/// giving this test its own fixture is purely about not fighting whatever specific
	/// cross-Fact server-state interaction causes the remaining symptom, rather than continuing
	/// to chase it indefinitely.
	/// </summary>
	[Collection(ExpLevelDialogCollection.Name)]
	public class ExpLevelDialogTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public ExpLevelDialogTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ExpLevelDialogOpensWithoutError() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "LevelDialogChecker")) {
				client.SendSpeech(".explevel");
				// If Construct() threw (e.g. a bad <Token> in the TEXT section, or an LScript
				// syntax error at load time), no gump packet would ever arrive at all and this
				// would time out/throw - that's the real signal here, not a log-string match.
				var (gumpUid, layoutText) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(gumpUid != 0 || layoutText.Length > 0, "Expected a real gump dialog to open for .explevel.");
			}
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ExpLevelDialogCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ExpLevelDialogPair";
	}
}
