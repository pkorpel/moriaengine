using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1272 - q026: the server curses the hero and takes them to Andor; a client greets Andor, gets his dialog and
	/// lets him look; the server then walks the rest through the dialogs' buttons and the fixtures' triggers. Server side:
	/// scripts/Tests/Q026Tests.cs.</summary>
	[Collection(Q026Collection.Name)]
	public class Q026Tests : LivingWorldTestBase {
		public Q026Tests(GameServerPairFixture servers)
			: base(servers, "q026tests", "Q026") {
		}

		[Fact]
		public void TheTombCurse() {
			using (TestGameClient game = this.Login("Prokleta Otrava")) {
				this.Run(game, ".TestQ026Setup", "Q026_SETUP_DONE");
				this.AssertChecks("theWraithCurses", "theCurseTicks", "thePotionThenEriol");

				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Andor"));
				Assert.Contains(gump.Strings, s => s.Contains("Nech me... skyt... byt"));
				this.Run(game, ".TestQ026Button \"q026_andor\"", "Q026_BUTTON:");
				string line = this.LastLine("Q026_BUTTON:");
				game.SendGumpResponse(gump.GumpUid, int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture));
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stage=4"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ026State", "Q026_STATE:");
					state = this.LastLine("Q026_STATE:");
				}
				Assert.Contains("stage=4", state);

				this.Run(game, ".TestQ026Chain", "Q026_CHAIN_DONE");
				this.AssertChecks("theClientToldAndor", "unasThenTheTapestry", "noKeyNoWay",
					"dolGuldur", "theDoorAndTheBook", "roxanasGoods", "theCure", "immuneForGood", "journalTextsArePlain");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class Q026Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q026";
	}
}
