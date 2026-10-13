using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Leveling system Phase 1 (#126): the level * coefficient threshold formula (with
	/// overflow carry across multiple levels in one gain), and the .explevel dialog actually
	/// opening without error (scripts/Basic/dialogs/explevel.scp - LScript content isn't
	/// checked by `dotnet build` at all, so this is the only thing that would catch a syntax
	/// or token-resolution error in it).
	/// </summary>
	[Collection(LevelingCollection.Name)]
	public class LevelingTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LevelingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SingleLevelUpCarriesOverflowExperience() {
			using (var client = this.ConnectLoginAndCreateCharacter("LevelOneUpper")) {
				client.SendSpeech(".TestSetLevelAndExperience 1 600");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 100");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_TEST_RESULT:");
				// level 1, threshold to level 2 is 660 - 600+100=700 crosses it, 40 carries over.
				Assert.Contains("level=2", line);
				Assert.Contains("experience=40", line);
			}
		}

		[Fact]
		public void OneLargeGainCascadesThroughMultipleLevels() {
			using (var client = this.ConnectLoginAndCreateCharacter("LevelMultiUpper")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_TEST_RESULT:");
				// level 0->1 (330, 1670 left) ->2 (660, 1010 left) ->3 (990, 20 left) -> stop, 20 < 1320.
				Assert.Contains("level=3", line);
				Assert.Contains("experience=20", line);
			}
		}

		[Fact]
		public void KillingNpcGrantsExperienceToKiller() {
			using (var client = this.ConnectLoginAndCreateCharacter("LevelNpcKiller")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestKillNpcGrantsExperience 50 50");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_KILL_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_KILL_TEST_RESULT:");
				Assert.Contains("level=0", line);
				Assert.Contains("experience=50", line);
			}
		}

		[Fact]
		public void ExpLevelDialogOpensWithoutError() {
			using (var client = this.ConnectLoginAndCreateCharacter("LevelDialogChecker")) {
				client.SendSpeech(".explevel");
				// If Construct() threw (e.g. a bad <Token> in the TEXT section, or an LScript
				// syntax error at load time), no gump packet would ever arrive at all and this
				// would time out/throw - that's the real signal here, not a log-string match.
				var (gumpUid, layoutText) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(gumpUid != 0 || layoutText.Length > 0, "Expected a real gump dialog to open for .explevel.");
			}
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

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
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
	public class LevelingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LevelingPair";
	}
}
