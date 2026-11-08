using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1302 - Mordor's stories: a client takes the Mouth's dispatch as an order from the camp's overseer, finds the box at
	/// Minas Morgul and puts it in with a double click; then the pits of Nurn and the Eye's spies on the server. Server side:
	/// scripts/Tests/MordorStoryTests.cs.</summary>
	[Collection(MordorStoriesCollection.Name)]
	public class MordorStoryTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1;

		public MordorStoryTests(GameServerPairFixture servers)
			: base(servers, "mordorstorytests", "MORDORSTORIES") {
		}

		[Fact]
		public void NurnTheEyeAndTheMouthHaveTheirOrders() {
			using (TestGameClient game = this.Login("Skret Posel")) {
				this.Run(game, ".TestMordorStoriesSetup", "MORDORSTORIES_SETUP:");
				string setup = this.LastLine("MORDORSTORIES_SETUP:");
				uint overseer = (uint) Number(setup, "overseer");

				game.SendDoubleClick(overseer);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Rozkaz?", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Rozkazy Usta", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);

				this.Run(game, ".TestMordorStoriesBesideBox", "MORDORSTORIES_BOX:");
				this.AssertChecks("theBoxIsAtTheTower");
				uint box = (uint) Number(this.LastLine("MORDORSTORIES_BOX:"), "uid");
				game.SendDoubleClick(box);
				System.Threading.Thread.Sleep(500);
				this.Run(game, ".TestMordorStoriesState \"" + Number(setup, "gold") + "\"", "MORDORSTORIES_STATE_DONE");
				this.AssertChecks("theDispatchArrived");

				this.Run(game, ".TestMordorStoriesChain", "MORDORSTORIES_CHAIN_DONE");
				this.AssertChecks("nurnSlavesFleeInNurnFromItsOverseersOnly", "handedBackOrOneFreedForHalfAndKarma", "foundOutSometimesAndAGiftEveryThird",
					"theCluesSingleOutTheSpyFromTheCampsOverseers", "theRightSpyPaysAWrongOneCosts", "dispatchesOnlyFromTheCamp");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class MordorStoriesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MordorStories";
	}
}
