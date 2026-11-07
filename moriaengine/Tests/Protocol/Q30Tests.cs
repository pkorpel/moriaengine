using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1276 - q30: a client greets Gondor's envoy, gets his dialog and takes the first hunt; the server walks the rest.
	/// Server side: scripts/Tests/Q30Tests.cs.</summary>
	[Collection(Q30Collection.Name)]
	public class Q30Tests : LivingWorldTestBase {
		public Q30Tests(GameServerPairFixture servers)
			: base(servers, "q30tests", "Q30") {
		}

		[Fact]
		public void TheRealmsEnvoy() {
			using (TestGameClient game = this.Login("Novacek Gondoru")) {
				this.Run(game, ".TestQ30Setup", "Q30_SETUP_DONE");
				game.SendSpeech("zdravim");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Zdravim..."));
				Assert.Contains(gump.Strings, s => s.Contains("Nejake drby ?"));
				Assert.Matches(@"gumppic 38 217 12290\b", gump.LayoutText); //Gondor's face, 0x3002 (#674's trap gave Mordor's 0x3111 on some boots)
				this.Run(game, ".TestQ30Button \"work_accept\"", "Q30_BUTTON:");
				string line = this.LastLine("Q30_BUTTON:");
				game.SendGumpResponse(gump.GumpUid, int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture));
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stage=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ30State", "Q30_STATE:");
					state = this.LastLine("Q30_STATE:");
				}
				Assert.Contains("stage=1", state);
				this.Run(game, ".TestQ30Chain", "Q30_CHAIN_DONE");
				this.AssertChecks("theClientTookTheFirstHunt", "aFullBagForExperience", "levelGates", "allFourteenAndTheCloak", "theCloaksOwner", "aLostCloak",
					"realmsAndChangers", "journalTextsArePlain");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class Q30Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q30";
	}
}
