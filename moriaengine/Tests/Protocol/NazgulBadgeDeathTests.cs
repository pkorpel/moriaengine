using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1037 - server side in scripts/Tests/NazgulBadgeDeathTests.cs.</summary>
	[Collection(NazgulBadgeDeathCollection.Name)]
	public class NazgulBadgeDeathTests {
		private const string TestAccount = "protocoltest";
		private readonly GameServerPairFixture fixture;

		public NazgulBadgeDeathTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void OfficeBadgesStayOnTheBodyThroughDeath() {
			int redirectPort = this.RedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestAccount);
				game.ReadCharacterList();
				game.SendCreateCharacter("Ringbearer");
				game.SendSpeech(".TestBadgeThroughDeath");
				Assert.True(this.fixture.WaitForGameOutput("BADGE_DONE", TimeSpan.FromSeconds(30)), this.fixture.GameOutputSnapshot());
			}
			string output = this.fixture.GameOutputSnapshot();
			foreach (string badge in new[] { "ring", "leadingRing", "bracelet" }) {
				foreach (string check in new[] { "StartsOnTheBody", "IsOnTheGhost", "NoCorpseHoldsIt", "IsOnTheResurrected", "ThePackIsNotTheBody", "LoginPutsItBackOn" }) {
					string marker = "BADGE: " + badge + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
			}
		}

		private int RedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestAccount);
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
	public class NazgulBadgeDeathCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NazgulBadgeDeath";
	}
}
