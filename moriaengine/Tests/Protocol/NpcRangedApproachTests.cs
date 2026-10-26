using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for a real user-reported bug: a ranged-weapon NPC (c_titan_ranger) sent
	/// to attack a target standing on a different floor/elevation just stood there instead of
	/// repositioning, even though a spellcasting NPC (c_titan_mage) correctly closed distance
	/// against the same kind of obstruction. Reproduced deterministically via a real wall placed
	/// between the two (blocks line of sight the same way height/floor differences do, per
	/// Character.CanInteractWith/CanSeeLOS - same mechanism LineOfSightBlockingTests.cs already
	/// proves), rather than live AI-tick timing (documented elsewhere in this codebase as flaky
	/// in this sandbox).
	/// </summary>
	[Collection(NpcRangedApproachCollection.Name)]
	public class NpcRangedApproachTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";
		private const string TestCharName = "Tester";

		public NpcRangedApproachTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RangedNpcRepositionsWhenLosBlocked() {
			int redirectPort = WaitForServerListingAndGetRedirectPort();

			bool sawResult;
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				string[] characters = game.ReadCharacterList();
				Assert.All(characters, name => Assert.Null(name));

				game.SendCreateCharacter(TestCharName);
				game.SendSpeech(".TestRangedNpcRepositionsWhenLosBlocked");

				sawResult = this.fixture.WaitForGameOutput("RANGED_APPROACH_TEST:", TimeSpan.FromSeconds(20));
			}

			Assert.True(sawResult, "Expected the approach check to report. Game output:\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int idx = output.LastIndexOf("RANGED_APPROACH_TEST:", StringComparison.Ordinal);
			int end = output.IndexOf('\n', idx);
			string line = output.Substring(idx, (end < 0 ? output.Length : end) - idx);
			Assert.Contains("hasLineOfSight=False", line);
			Assert.Contains("moved=True", line);
		}

		private readonly GameServerPairFixture fixture;

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
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
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NpcRangedApproachCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcRangedApproachPair";
	}
}
