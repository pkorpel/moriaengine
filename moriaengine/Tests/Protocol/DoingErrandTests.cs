using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1293 - the doing errand templates on the engine's own steps: a client asks Edoras's harbourmaster for work and takes
	/// the stowaway; the server builds and plays the five templates with their choices. Server side: scripts/Tests/DoingErrandTests.cs.</summary>
	[Collection(DoingErrandsCollection.Name)]
	public class DoingErrandTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1;

		public DoingErrandTests(GameServerPairFixture servers)
			: base(servers, "doingerrandtests", "DOINGERRANDS") {
		}

		[Fact]
		public void ResidentsOfferTheDoingErrands() {
			using (TestGameClient game = this.Login("Pristavni pomocnik")) {
				this.Run(game, ".TestDoingErrandsSetup", "DOINGERRANDS_SETUP:");
				uint master = (uint) Number(this.LastLine("DOINGERRANDS_SETUP:"), "master");

				game.SendDoubleClick(master);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Cerny pasazer", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stowaway=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestDoingErrandsState", "DOINGERRANDS_STATE:");
					state = this.LastLine("DOINGERRANDS_STATE:");
				}
				Assert.Contains("stowaway=1", state);

				this.Run(game, ".TestDoingErrandsChain", "DOINGERRANDS_CHAIN_DONE");
				this.AssertChecks("commission", "courier", "ghostwords", "brawl", "stowaway", "notInTheCamp", "plainCzech");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class DoingErrandsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DoingErrands";
	}
}
