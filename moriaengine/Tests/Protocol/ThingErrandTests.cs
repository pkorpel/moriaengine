using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1293 - errands with things in the world: a client asks a market merchant for work, takes "Zlodej" and catches the
	/// pickpocket with a double click; the server walks the six templates. Server side: scripts/Tests/ThingErrandTests.cs.</summary>
	[Collection(ThingErrandsCollection.Name)]
	public class ThingErrandTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1;

		public ThingErrandTests(GameServerPairFixture servers)
			: base(servers, "thingerrandtests", "THINGERRANDS") {
		}

		private string State(TestGameClient game, string want) {
			string state = "";
			for (int i = 0; (i < 20) && !state.Contains(want); i++) {
				System.Threading.Thread.Sleep(250);
				this.Run(game, ".TestThingErrandsState", "THINGERRANDS_STATE:");
				state = this.LastLine("THINGERRANDS_STATE:");
			}
			return state;
		}

		[Fact]
		public void ErrandsPutThingsInTheWorld() {
			using (TestGameClient game = this.Login("Mestsky bezec")) {
				this.Run(game, ".TestThingErrandsSetup", "THINGERRANDS_SETUP:");
				uint merchant = (uint) Number(this.LastLine("THINGERRANDS_SETUP:"), "merchant");

				game.SendDoubleClick(merchant);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Zlodej", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);
				Assert.Contains("thief=0", this.State(game, "thief=0"));

				this.Run(game, ".TestThingErrandsBesideThief", "THINGERRANDS_THIEF:");
				game.SendDoubleClick((uint) Number(this.LastLine("THINGERRANDS_THIEF:"), "uid"));
				Assert.Contains("thief=1", this.State(game, "thief=1"));

				this.Run(game, ".TestThingErrandsChain", "THINGERRANDS_CHAIN_DONE");
				this.AssertChecks("thiefCaughtByClick", "runaway", "thingsGoWithTheErrand", "lamplighter", "cellar", "thief", "shipment", "pressgang", "notInTheCamp", "plainCzech");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ThingErrandsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ThingErrands";
	}
}
