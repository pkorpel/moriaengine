using System;
using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1270 - q009: a client greets Talathwen, gets her dialog and agrees to bring the cheque; the server then walks the
	/// rest through the dialog's buttons. Server side: scripts/Tests/Q009Tests.cs.</summary>
	[Collection(Q009Collection.Name)]
	public class Q009Tests : LivingWorldTestBase {
		public Q009Tests(GameServerPairFixture servers)
			: base(servers, "q009tests", "Q009") {
		}

		[Fact]
		public void TalathwensRing() {
			using (TestGameClient game = this.Login("Krejcuv Zakaznik")) {
				this.Run(game, ".TestQ009Setup", "Q009_SETUP_DONE");

				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Talathwen"));
				Assert.Contains(gump.Strings, s => s.Contains("Vitej v mem kramku !"));
				//#1312 - a long response (two rows, HTML) is coloured, the "Mohu pomoci?" page's second answer was black on black
				Assert.Contains(gump.Strings, s => s.StartsWith("<basefont color=#D7DF01>Vsiml jsem si dokonalych tvaru", StringComparison.Ordinal));
				this.Run(game, ".TestQ009Button \"krejci_1:1\"", "Q009_BUTTON:");
				game.SendGumpResponse(gump.GumpUid, Q009Q018Steps.IdOf(this.LastLine("Q009_BUTTON:")));
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stage=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ009State", "Q009_STATE:");
					state = this.LastLine("Q009_STATE:");
				}
				Assert.Contains("stage=1", state);

				this.Run(game, ".TestQ009Chain", "Q009_CHAIN_DONE");
				this.AssertChecks("theClientAgreed", "theJournalListsIt", "exactlyFiveMillion", "theWaitMoves", "theRing", "theRingsRules",
					"herLastDialogAndShop", "journalTextsArePlain");
			}
		}
	}

	/// <summary>#1270 - q018: a client greets the fisherman, gets his dialog and takes the task; the server walks the rest.
	/// Server side: scripts/Tests/Q018Tests.cs.</summary>
	[Collection(Q009Collection.Name)]
	public class Q018Tests : LivingWorldTestBase {
		public Q018Tests(GameServerPairFixture servers)
			: base(servers, "q009tests", "Q018") {
		}

		[Fact]
		public void TheGoldenSalmon() {
			using (TestGameClient game = this.Login("Lovec Lososu")) {
				this.Run(game, ".TestQ018Setup", "Q018_SETUP_DONE");

				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Rybar podivin"));
				Assert.Contains(gump.Strings, s => s.Contains("Zdravim te."));
				this.Run(game, ".TestQ018Button \"rybar:1\"", "Q018_BUTTON:");
				game.SendGumpResponse(gump.GumpUid, Q009Q018Steps.IdOf(this.LastLine("Q018_BUTTON:")));
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("stage=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestQ018State", "Q018_STATE:");
					state = this.LastLine("Q018_STATE:");
				}
				Assert.Contains("stage=1", state);

				this.Run(game, ".TestQ018Chain", "Q018_CHAIN_DONE");
				this.AssertChecks("theClientTookTheTask", "theJournalListsIt", "sentToElnassir", "elnassirsSalve", "theSalveForTheRune",
					"theRuneAndTheWheel", "theFishermansReward", "eachNpcOpensItsOwnDialog", "journalTextsArePlain");
			}
		}
	}

	internal static class Q009Q018Steps {
		internal static int IdOf(string line) {
			return int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture);
		}
	}

	[CollectionDefinition(Name)]
	public class Q009Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q009";
	}
}
