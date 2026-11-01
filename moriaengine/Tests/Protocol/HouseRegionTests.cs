using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - the archive's house numbers and realms reach the regions.</summary>
	[Collection(HouseRegionCollection.Name)]
	public class HouseRegionTests {
		private const string Account = "houseregiontest";

		private readonly GameServerPairFixture fixture;

		public HouseRegionTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryHouseKnowsItsNumberAndRealm() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (TestGameClient client = new TestGameClient("127.0.0.1", redirectPort)) {
				client.SendLogin(Account, Account);
				client.ReadCharacterList();
				client.SendCreateCharacter("House Regions");
				Thread.Sleep(500);
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestHouseRegions");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_REGIONS:", mark, TimeSpan.FromSeconds(15)));
				string output = this.fixture.GameOutputSnapshot();
				string line = output.Substring(output.LastIndexOf("HOUSE_REGIONS:", StringComparison.Ordinal));
				Assert.Contains("numbered=383 houses=281 gondor=247 mordor=133 dum2=2/Mordor", line);
			}
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Account);
						if (login.ReadServersList() < 1) {
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
	public class HouseRegionCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HouseRegion";
	}
}
