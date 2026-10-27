using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_bleed given real
	/// behavior (NpcBleedEffect.cs/.ct/.scp, MonsterSpecialAttacks.TryApplyBleed). See
	/// scripts/Tests/NpcBleedTests.cs's own doc comment for the full writeup.
	/// </summary>
	[Collection(NpcBleedCollection.Name)]
	public class NpcBleedTests {
		private const string Account = "npcbleedtest";
		private const string Password = "npcbleedtest";

		private readonly GameServerPairFixture fixture;

		public NpcBleedTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BleedAppliesWithExpectedEffectPower() {
			using (var client = this.ConnectLoginAndCreateCharacter("BleedApplyChecker")) {
				client.SendSpeech(".TestBleedAppliesOnHit");
				Assert.True(this.fixture.WaitForGameOutput("BLEED_APPLY_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "BLEED_APPLY_TEST:");

				Assert.Contains("bleeding=True", line);
				Assert.Contains("effectPower=10", line); //30 damage / 3, matching the real archive ratio
			}
		}

		[Fact]
		public void BleedStacksOnRepeatedHits() {
			using (var client = this.ConnectLoginAndCreateCharacter("BleedStackChecker")) {
				client.SendSpeech(".TestBleedStacksOnRepeatedHits");
				Assert.True(this.fixture.WaitForGameOutput("BLEED_STACK_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "BLEED_STACK_TEST:");

				Assert.Contains("afterFirst=10", line);
				Assert.Contains("afterSecond=20", line);
				Assert.Contains("stacked=True", line);
			}
		}

		[Fact]
		public void BleedTickSelfRemovesOnFullResist() {
			using (var client = this.ConnectLoginAndCreateCharacter("BleedResistChecker")) {
				client.SendSpeech(".TestBleedTickSelfRemovesOnFullResist");
				Assert.True(this.fixture.WaitForGameOutput("BLEED_RESIST_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "BLEED_RESIST_TEST:");

				Assert.Contains("appliedBeforeTick=True", line);
				Assert.Contains("stillBleedingAfterTick=False", line);
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

	[CollectionDefinition(NpcBleedCollection.Name, DisableParallelization = true)]
	public class NpcBleedCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcBleedPair";
	}
}
