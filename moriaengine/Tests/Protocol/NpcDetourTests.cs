using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #1031 - the player blocks the only near gap of a long wall; a walking NPC has to go round
	/// its end, which lies outside the normal search window. Server side in
	/// scripts/Tests/NpcDetourTests.cs, driven by the real AI.
	/// </summary>
	[Collection(NpcDetourCollection.Name)]
	public class NpcDetourTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcDetourTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NpcWalksAroundAWallWhenAPlayerBlocksTheGap() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter("Blocker");
				game.SendSpeech(".TestNpcDetourStart");
				Assert.True(this.fixture.WaitForGameOutput("NPC_DETOUR_STARTED", TimeSpan.FromSeconds(30)), this.fixture.GameOutputSnapshot());

				Stopwatch clock = Stopwatch.StartNew();
				while (clock.Elapsed < TimeSpan.FromSeconds(50)) {
					Thread.Sleep(5000);
					game.SendSpeech(".TestNpcDetourReport");
					Thread.Sleep(300);
					if (this.fixture.GameOutputSnapshot().Contains("passed=True", StringComparison.Ordinal)) {
						return;
					}
				}
			}
			Assert.Fail("The NPC never got past the wall. Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
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
	public class NpcDetourCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcDetour";
	}
}
