using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1291 - the talking errand templates: a client asks a town hall clerk for work and takes the census; the server builds
	/// and plays the eight templates with their choices. Server side: scripts/Tests/TalkErrandTests.cs.</summary>
	[Collection(TalkErrandsCollection.Name)]
	public class TalkErrandTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1;

		public TalkErrandTests(GameServerPairFixture servers)
			: base(servers, "talkerrandtests", "TALKERRANDS") {
		}

		[Fact]
		public void ResidentsOfferTheTalkingErrands() {
			using (TestGameClient game = this.Login("Pisar Edorasu")) {
				this.Run(game, ".TestTalkErrandsSetup", "TALKERRANDS_SETUP:");
				uint clerk = (uint) Number(this.LastLine("TALKERRANDS_SETUP:"), "clerk");

				game.SendDoubleClick(clerk);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Scitani", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("census=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestTalkErrandsState", "TALKERRANDS_STATE:");
					state = this.LastLine("TALKERRANDS_STATE:");
				}
				Assert.Contains("census=1", state);

				this.Run(game, ".TestTalkErrandsChain", "TALKERRANDS_CHAIN_DONE");
				this.AssertChecks("quarrel", "admirer", "invitation", "loveletter", "books", "omen", "ale", "census", "notInTheCamp", "plainCzech");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TalkErrandsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TalkErrands";
	}
}
