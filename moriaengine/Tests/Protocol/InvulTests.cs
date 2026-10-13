using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// GM-only ".invul" command: while on, nothing can damage the character at all (see
	/// DamageManager.CauseDamage's Flag_Invulnerable short-circuit). Exercises the real speech
	/// command end to end; the "attack" itself is a direct DamageManager.CauseDamage call via
	/// InvulTests.TestApplyDamage (see that file's own remarks) rather than a full weapon-swing
	/// simulation, since weapon-swing mechanics already have dedicated coverage elsewhere
	/// (CombatDepthTests) - what's under test here is the damage choke point, not swings.
	/// </summary>
	[Collection(InvulCollection.Name)]
	public class InvulTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";
		private const int DamageAmount = 15;

		private readonly GameServerPairFixture fixture;

		public InvulTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void InvulOnBlocksDamageThenOffAllowsItAgain() {
			using (var client = this.ConnectLoginAndCreateCharacter("InvulUser")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(300);

				client.SendSpeech(".Invul");
				Thread.Sleep(300);

				int markBlocked = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestApplyDamage " + DamageAmount);
				Assert.True(this.fixture.WaitForGameOutputSince("DAMAGE_TEST:", markBlocked, TimeSpan.FromSeconds(10)));
				string blockedLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "DAMAGE_TEST:");
				Assert.Contains("actualDamage=0", blockedLine);

				client.SendSpeech(".Invul");
				Thread.Sleep(300);

				int markLands = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestApplyDamage " + DamageAmount);
				Assert.True(this.fixture.WaitForGameOutputSince("DAMAGE_TEST:", markLands, TimeSpan.FromSeconds(10)));
				string landsLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "DAMAGE_TEST:");
				Assert.Contains("actualDamage=" + DamageAmount, landsLine);
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
	public class InvulCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "InvulPair";
	}
}
