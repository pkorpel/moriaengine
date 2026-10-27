using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #734 - see scripts/Tests/NpcMultiSpellTests.cs's own doc comment for why this replaced
	/// #564's original "uniform random distribution" assertions: NPCAiTimer.ChooseSpellToCast
	/// (which replaced PickSpell) is cooldown-aware, so "ignores all state, picks uniformly" is
	/// no longer the real contract. Proves instead that once a spell is actually cast, it's
	/// excluded from the next choice (its own per-spell cooldown, not the old single shared one)
	/// and the remaining ready spell is picked both validly and stably (no flip-flopping with no
	/// state change in between).
	/// </summary>
	[Collection(NpcMultiSpellCollection.Name)]
	public class NpcMultiSpellTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcMultiSpellTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ExcludesWhicheverSpellWasJustCastAndStaysStable() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MultiSpellCooldownChecker")) {
				client.SendSpeech(".TestMultiOffensiveSpellCooldownExclusion");
				Assert.True(this.fixture.WaitForGameOutput("MULTI_SPELL_COOLDOWN_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MULTI_SPELL_COOLDOWN_TEST:");

				Assert.Contains("beforeCastValid=True", line);
				Assert.Contains("castAttempts=1", line);
				Assert.Contains("afterCastValid=True", line);
				Assert.Contains("stableAcrossCalls=True", line);
			}
		}

		[Fact]
		public void SingleSpellCasterGoesOnCooldownAfterCasting() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "SingleSpellCooldownChecker")) {
				client.SendSpeech(".TestSingleSpellGoesOnCooldown");
				Assert.True(this.fixture.WaitForGameOutput("SINGLE_SPELL_COOLDOWN_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SINGLE_SPELL_COOLDOWN_TEST:");

				Assert.Contains("readyBeforeIsSpell=True", line);
				Assert.Contains("castAttempts=1", line);
				Assert.Contains("readyAfterIsNull=True", line);
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

	[CollectionDefinition(NpcMultiSpellCollection.Name, DisableParallelization = true)]
	public class NpcMultiSpellCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcMultiSpellPair";
	}
}
