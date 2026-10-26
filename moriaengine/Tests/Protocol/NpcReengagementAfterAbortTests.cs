using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Regression for a real user-reported bug: a ranged NPC (c_titan_ranger) fired a
	/// few shots then stopped attacking entirely, while a caster NPC in the same fight kept
	/// going. See scripts/Tests/NpcReengagementAfterAbortTests.cs for the full root-cause
	/// writeup - a transient LOS-loss abort deletes WeaponSkillTargetQueuePlugin entirely, and
	/// NPCAiTimer's own "add once per engagement" bookkeeping never noticed.</summary>
	[Collection(NpcReengagementAfterAbortCollection.Name)]
	public class NpcReengagementAfterAbortTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";
		private const string TestCharName = "Tester";

		public NpcReengagementAfterAbortTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NpcReengagesAfterTargetQueueAbort() {
			int redirectPort = WaitForServerListingAndGetRedirectPort();

			bool sawResult;
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				string[] characters = game.ReadCharacterList();
				Assert.All(characters, name => Assert.Null(name));

				game.SendCreateCharacter(TestCharName);
				game.SendSpeech(".TestNpcReengagesAfterTargetQueueAbort");

				sawResult = this.fixture.WaitForGameOutput("REENGAGEMENT_TEST:", TimeSpan.FromSeconds(20));
			}

			Assert.True(sawResult, "Expected the re-engagement check to report. Game output:\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int idx = output.LastIndexOf("REENGAGEMENT_TEST:", StringComparison.Ordinal);
			int end = output.IndexOf('\n', idx);
			string line = output.Substring(idx, (end < 0 ? output.Length : end) - idx);

			Assert.Contains("engagedBeforeAbort=True", line);
			Assert.Contains("engagedRightAfterAbort=False", line);
			Assert.Contains("engagedAfterNextFightTick=True", line);
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

	[CollectionDefinition(NpcReengagementAfterAbortCollection.Name, DisableParallelization = true)]
	public class NpcReengagementAfterAbortCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcReengagementAfterAbortPair";
	}
}
