using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #734 - see scripts/Tests/NpcSupportOnlySpellWeavingTests.cs's own doc comment for the full
	/// writeup. A melee NPC whose only spells are support/self-targeted (heal, a buff - no
	/// offensive spell at all, same kit shape as the real guard that exposed this) used to hold
	/// at spell range forever with no way to ever actually engage. Own dedicated collection/
	/// account, same precedent as NpcRangeOverrideTests.cs.
	/// </summary>
	[Collection(NpcSupportOnlySpellWeavingCollection.Name)]
	public class NpcSupportOnlySpellWeavingTests {
		private const string Account = "supportonlyspelltest";
		private const string Password = "supportonlyspelltest";

		private readonly GameServerPairFixture fixture;

		public NpcSupportOnlySpellWeavingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BuffsItselfOnceThenClosesToMeleeInsteadOfHoldingRangeForever() {
			using (var client = this.ConnectLoginAndCreateCharacter("SupportOnlyBuffCloseChecker")) {
				//same "teleport to confirmed open ground" precedent as
				//NpcRangeOverrideTests.MageSpellCastRangeOverrideForcesCloserEngagement - the
				//default spawn point is a boxed-in antechamber unreliable for real movement.
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				client.SendSpeech(".TestSupportOnlyNpcBuffsOnceThenCloses");
				Assert.True(this.fixture.WaitForGameOutput("SUPPORT_ONLY_BUFF_THEN_CLOSE_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SUPPORT_ONLY_BUFF_THEN_CLOSE_TEST:");

				Assert.Contains("movedOnFirstTick=False", line);
				Assert.Contains("castAttemptsAfterFirst=1", line);
				Assert.Contains("movedOnSecondTick=True", line);
			}
		}

		[Fact]
		public void HealsItselfInsteadOfBuffingOrIdlingWhenHurt() {
			using (var client = this.ConnectLoginAndCreateCharacter("SupportOnlyHealChecker")) {
				client.SendSpeech(".TestSupportOnlyNpcHealsInsteadOfBuffingWhenHurt");
				Assert.True(this.fixture.WaitForGameOutput("SUPPORT_ONLY_HEALS_WHEN_HURT_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SUPPORT_ONLY_HEALS_WHEN_HURT_TEST:");

				Assert.Contains("castAttempts=1", line);
				Assert.Contains("healed=True", line);
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

	[CollectionDefinition(NpcSupportOnlySpellWeavingCollection.Name, DisableParallelization = true)]
	public class NpcSupportOnlySpellWeavingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcSupportOnlySpellWeavingPair";
	}
}
