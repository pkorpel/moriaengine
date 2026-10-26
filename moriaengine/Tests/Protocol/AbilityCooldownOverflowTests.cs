using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #679: AbilityDef.On_DenyActivate's cooldown check used to compute
	/// Globals.TimeAsSpan - ab.LastUsage unconditionally - Ability's own "never used" sentinel
	/// is TimeSpan.MinValue, and subtracting that always throws OverflowException. Real-user-
	/// reported via c_f_healer's granted stub ability, repeating in the server log every time
	/// something tried to activate it. A real exception here (the pre-fix behavior) blows up
	/// this whole Fact, not just fails an assert.
	/// </summary>
	[Collection(AbilityCooldownOverflowCollection.Name)]
	public class AbilityCooldownOverflowTests {
		private const string Account = "abilitycooldowntest";
		private const string Password = "abilitycooldowntest";
		private const string CharName = "AbilityActivator";

		[Fact]
		public void ActivatingANeverUsedAbilityDoesNotThrow() {
			using (var fixture = new GameServerPairFixture()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					client.ReadCharacterList();
					client.SendCreateCharacter(CharName);

					int mark = fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestActivateNeverUsedAbility");
					Assert.True(fixture.WaitForGameOutputSince("ABILITY_ACTIVATE_TEST:", mark, TimeSpan.FromSeconds(10)));
					Assert.DoesNotContain("TimeSpan overflowed", fixture.GameOutputSnapshot());
				}
			}
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture) {
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

	[CollectionDefinition(AbilityCooldownOverflowCollection.Name, DisableParallelization = true)]
	public class AbilityCooldownOverflowCollection {
		public const string Name = "AbilityCooldownOverflowPair";
	}
}
