using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1307 - the ferry to Ostrov nestalosti: a client takes the boat from Dol Amroth's ferryman through his dialog and lands
	/// on the island, paying the fare; then the routes, a follower along and no fare no ride on the server. Server side:
	/// scripts/Tests/FerryTests.cs.</summary>
	[Collection(FerryCollection.Name)]
	public class FerryTests : LivingWorldTestBase {
		private const int IslandButton = 1; //D_Ferry: FirstStop + the island's place in Ferry.All (0)

		public FerryTests(GameServerPairFixture servers)
			: base(servers, "ferrytests", "FERRY") {
		}

		[Fact]
		public void TheFerryGoesToTheIslandAndBack() {
			using (TestGameClient game = this.Login("Namornik")) {
				this.Run(game, ".TestFerrySetup", "FERRY_SETUP:");
				string setup = this.LastLine("FERRY_SETUP:");
				game.SendDoubleClick((uint) Number(setup, "ferryman"));
				var dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.StartsWith("Ostrov nestalosti (", System.StringComparison.Ordinal));
				game.SendGumpResponse(dialog.GumpUid, IslandButton);
				System.Threading.Thread.Sleep(3500); //the boat's count (1 s in the test) and its tick
				this.Run(game, ".TestFerryAfterClient \"" + Number(setup, "gold") + "\"", "FERRY_CLIENT_DONE");
				this.AssertChecks("theClientSailedToTheIsland");

				this.Run(game, ".TestFerryChain", "FERRY_CHAIN_DONE");
				this.AssertChecks("theRoutes", "aRideWithItsFollowersAfterTheCount", "strayAndTheBoatLeaves", "noGhostsNoFareNoRide");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class FerryCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Ferry";
	}
}
