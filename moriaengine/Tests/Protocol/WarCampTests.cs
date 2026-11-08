using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1280 - Barad-dur's war camp: a client asks a resident there for an order ("Rozkaz?"), gets the offer as an order
	/// ("Rozumim." / "Odmitnout.") and refuses it, which costs standing; the server walks the ranks, orders and rival captains; the
	/// rival's cutthroats arrive by their timer. Server side: scripts/Tests/WarCampTests.cs.</summary>
	[Collection(WarCampCollection.Name)]
	public class WarCampTests : LivingWorldTestBase {
		private const int AskButton = 1, RefuseButton = 2;

		public WarCampTests(GameServerPairFixture servers)
			: base(servers, "warcamptests", "WARCAMP") {
		}

		[Fact]
		public void OrdersRanksAndRivalCaptains() {
			using (TestGameClient game = this.Login("Skret Baradduru")) {
				this.Run(game, ".TestWarCampSetup", "WARCAMP_SETUP_DONE");
				uint giver = (uint) Number(this.LastLine("WARCAMP_SETUP:"), "giver");

				game.SendDoubleClick(giver);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Rozkaz?", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Rozumim.", offer.Strings);
				Assert.Contains("Odmitnout.", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, RefuseButton);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("rep=-25"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestWarCampState", "WARCAMP_STATE:");
					state = this.LastLine("WARCAMP_STATE:");
				}
				Assert.Contains("rep=-25", state);

				this.Run(game, ".TestWarCampChain", "WARCAMP_CHAIN_DONE");
				this.AssertChecks("theClientRefusedAnOrder", "ranks", "promotionPaysOnce", "rankCapsTheTier", "shorterAndBetterPaid", "failingCostsStanding",
					"mordorOnly", "serving", "ironOrder", "tributeOrder", "headsOrder", "failingHisOrders", "switching", "weeklySettlement", "cutthroats", "nemesis");

				this.Run(game, ".TestWarCampCutthroatTimer", "WARCAMP_TIMER_ARMED");
				System.Threading.Thread.Sleep(3000);
				this.Run(game, ".TestWarCampCutthroatReport", "WARCAMP_CUTTHROATS:");
				Assert.Contains("count=2", this.LastLine("WARCAMP_CUTTHROATS:"));
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WarCampCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WarCamp";
	}
}
