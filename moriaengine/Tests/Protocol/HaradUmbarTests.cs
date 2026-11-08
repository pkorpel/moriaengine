using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1282 - Harad and Umbar: a client greets a Harad caravan master (a stranger's welcome in Harad's voice), asks for work
	/// and takes the caravan's cargo; the server walks names, looks, venues, factions, the caravan and the contraband. Server side:
	/// scripts/Tests/HaradUmbarTests.cs.</summary>
	[Collection(HaradUmbarCollection.Name)]
	public class HaradUmbarTests : LivingWorldTestBase {
		private const int AskButton = 1, AcceptButton = 1;
		private static readonly string[] haradGreetings = { "Pokoj s tebou, poutniku z dalky.", "Vitej pod nasim sluncem. Co hledas?", "Prach cest je na tobe. Mluv." };

		public HaradUmbarTests(GameServerPairFixture servers)
			: base(servers, "haradumbartests", "HARADUMBAR") {
		}

		[Fact]
		public void HaradAndUmbarHaveTheirOwnColour() {
			using (TestGameClient game = this.Login("Kupec Haradu")) {
				this.Run(game, ".TestHaradUmbarSetup", "HARADUMBAR_SETUP_DONE");
				uint master = (uint) Number(this.LastLine("HARADUMBAR_SETUP:"), "master");

				game.SendDoubleClick(master);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(talk.Strings, s => System.Array.Exists(haradGreetings, g => s.Contains(g)));
				game.SendGumpResponse(talk.GumpUid, AskButton);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Karavana", offer.Strings);
				game.SendGumpResponse(offer.GumpUid, AcceptButton);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("caravans=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestHaradUmbarState", "HARADUMBAR_STATE:");
					state = this.LastLine("HARADUMBAR_STATE:");
				}
				Assert.Contains("caravans=1", state);

				this.Run(game, ".TestHaradUmbarChain", "HARADUMBAR_CHAIN_DONE");
				this.AssertChecks("namesAndLooks", "venuesAndGreetings", "factions", "caravan", "contrabandBribe", "contrabandOtherWays");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class HaradUmbarCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HaradUmbar";
	}
}
