using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1317 - the stories of Erebor and Dale: a client takes the false Arkenstone from a merchant of Erebor, catches the con
	/// man with a double click and brings the stone in the dialog; then the ravens, the axe, the deep veins, the fish and the bargeman
	/// on the server. Server side: scripts/Tests/EreborStoryTests.cs.</summary>
	[Collection(EreborStoriesCollection.Name)]
	public class EreborStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1, FirstStepButton = 100;

		public EreborStoryTests(GameServerPairFixture servers)
			: base(servers, "erebortests", "EREBOR") {
		}

		[Fact]
		public void EreborAndDaleHaveTheirStories() {
			using (TestGameClient game = this.Login("Trpaslik z hory")) {
				this.Run(game, ".TestEreborSetup", "EREBOR_SETUP:");
				string setup = this.LastLine("EREBOR_SETUP:");
				uint merchant = (uint) Number(setup, "merchant");
				Assert.True(setup.Contains("talk=True"), setup);

				game.SendDoubleClick(merchant);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Falesny Arcikam", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				this.Run(game, ".TestEreborBesideConMan", "EREBOR_CONMAN:");
				string conMan = this.LastLine("EREBOR_CONMAN:");
				Assert.True(Number(conMan, "uid") != 0, conMan);
				int errand = Number(conMan, "errand");
				game.SendDoubleClick((uint) Number(conMan, "uid"));
				System.Threading.Thread.Sleep(300);

				this.Run(game, ".TestEreborBesideGiver", "EREBOR_GIVER:");
				this.AssertChecks("theConManIsCaughtWithTheStone");
				game.SendDoubleClick(merchant);
				talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Tady je ten jeho 'Arcikam'.", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, FirstStepButton + (errand * 10));
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestEreborState \"" + Number(setup, "gold") + "\"", "EREBOR_STATE_DONE");
				this.AssertChecks("theStoneIsBrought");

				this.Run(game, ".TestEreborChain", "EREBOR_CHAIN_DONE");
				this.AssertChecks("onlyTheirOwnCities", "theRavensCarryItToDale", "theAncestralAxeComesHome", "theDeepVeinsNeedAMiner", "theFishQuotaIsMet",
					"theBargemanIsCaughtWithHisCrate", "aTwoPartTownsThingsAreInTheGiversPart");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class EreborStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "EreborStories";
	}
}
