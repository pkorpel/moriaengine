using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1268 - q017 Alchymista Terlak: a client greets the innkeeper, hears of Terlak and drinks the free potion; the server
	/// walks the rest; then a run times out and another ends in a death, each on its own timer. Server side: scripts/Tests/Q017Tests.cs.</summary>
	[Collection(Q017Collection.Name)]
	public class Q017Tests : LivingWorldTestBase {
		public Q017Tests(GameServerPairFixture servers)
			: base(servers, "q017tests", "Q017") {
		}

		[Fact]
		public void AlchemistTerlak() {
			using (TestGameClient game = this.Login("Terlakuv Zachrance")) {
				this.Run(game, ".TestQ017Setup", "Q017_SETUP_DONE");

				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Terlak hmm... rekni mi vic.."));
				this.Run(game, ".TestQ017Button \"hospodsky:8\"", "Q017_BUTTON:");
				string line = this.LastLine("Q017_BUTTON:");
				int id = int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture);
				game.SendGumpResponse(gump.GumpUid, id);
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("protection=2"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ017State", "Q017_STATE:");
					state = this.LastLine("Q017_STATE:");
				}
				Assert.Contains("protection=2", state);

				this.Run(game, ".TestQ017Chain", "Q017_CHAIN_DONE");
				this.AssertChecks("theClientDrankTheFreePotion", "laterPotionsWantDeerEyes", "theFullBagAndGoldForAPotion", "theEntryGatesRules",
					"theExitGivesTheProfessionsItem", "theRewardsRules", "laterRunsWantMoreCrystalsAndWaitFourHours", "crystalsDropInTheGatesDungeons",
					"journalTextsArePlain");

				foreach (string ending in new[] { "timeout", "death" }) {
					this.Run(game, ".TestQ017RunsEndedStart \"" + ending + "\"", "Q017_RUN_STARTED:");
					Assert.Contains("onRun=True", this.LastLine("Q017_RUN_STARTED:"));
					string ended = "";
					for (int i = 0; (i < 30) && !ended.Contains("out=True"); i++) {
						System.Threading.Thread.Sleep(250);
						this.Run(game, ".TestQ017RunsEndedCheck", "Q017_RUN_ENDED:");
						ended = this.LastLine("Q017_RUN_ENDED:");
					}
					Assert.True(ended.Contains("onRun=False out=True timer=False"), ending + ": " + ended);
				}
			}
		}
	}

	[CollectionDefinition(Name)]
	public class Q017Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q017";
	}
}
