using System.Globalization;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1274 - q008: a client greets Ancam (his mustang in the corral), gets his dialog and takes the crate; the server walks
	/// the rest. Server side: scripts/Tests/Q008Tests.cs.</summary>
	[Collection(Q008Collection.Name)]
	public class Q008Tests : LivingWorldTestBase {
		public Q008Tests(GameServerPairFixture servers)
			: base(servers, "q008tests", "Q008") {
		}

		[Fact]
		public void TheBlackMustang() {
			using (TestGameClient game = this.Login("Krotitel Mustangu")) {
				this.Run(game, ".TestQ008Setup", "Q008_SETUP_DONE");
				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Ancam"));
				Assert.Contains(gump.Strings, s => s.Contains("Mustang"));
				Q008Q31Steps.Press(game, gump.GumpUid, "Q008", "ancam_2:1", "stage=1", this.RunFor, this.LastLineFor);
				this.Run(game, ".TestQ008Chain", "Q008_CHAIN_DONE");
				this.AssertChecks("theClientTookTheCrate", "rangersOnly", "reagentsAndKills", "theCrateHandedIn", "theCorral", "anotherRound",
					"theWaitPath", "banned", "journalTextsArePlain");
			}
		}

		private void RunFor(TestGameClient game, string command, string marker) {
			this.Run(game, command, marker);
		}

		private string LastLineFor(string marker) {
			return this.LastLine(marker);
		}
	}

	/// <summary>#1274 - q31: a client greets the cook, gets his dialog and takes the bag; the server walks the rest. Server side:
	/// scripts/Tests/Q31Tests.cs.</summary>
	[Collection(Q008Collection.Name)]
	public class Q31Tests : LivingWorldTestBase {
		public Q31Tests(GameServerPairFixture servers)
			: base(servers, "q008tests", "Q31") {
		}

		[Fact]
		public void TheCooksBirds() {
			using (TestGameClient game = this.Login("Lovec Ptacku")) {
				this.Run(game, ".TestQ31Setup", "Q31_SETUP_DONE");
				game.SendSpeech("ahoj");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Kuchar Mrkvicka"));
				Assert.Contains(gump.Strings, s => s.Contains("Zdravicko, ja jsem kuchar Mrkvicka."));
				Q008Q31Steps.Press(game, gump.GumpUid, "Q31", "kuchar_1:1", "stage=1", this.RunFor, this.LastLineFor);
				this.Run(game, ".TestQ31Chain", "Q31_CHAIN_DONE");
				this.AssertChecks("theClientTookTheBag", "fiftyBirdsForALlama", "eachCookHisOwnSide", "lostForGood", "aNewBag", "journalTextsArePlain");
			}
		}

		private void RunFor(TestGameClient game, string command, string marker) {
			this.Run(game, command, marker);
		}

		private string LastLineFor(string marker) {
			return this.LastLine(marker);
		}
	}

	internal static class Q008Q31Steps {
		/// <summary>Presses an exit button through the client and waits until the server reports the wanted stage.</summary>
		internal static void Press(TestGameClient game, int gumpUid, string prefix, string button, string wanted,
				System.Action<TestGameClient, string, string> run, System.Func<string, string> lastLine) {
			run(game, ".Test" + prefix + "Button \"" + button + "\"", prefix + "_BUTTON:");
			string line = lastLine(prefix + "_BUTTON:");
			game.SendGumpResponse(gumpUid, int.Parse(line.Substring(line.IndexOf("id=", System.StringComparison.Ordinal) + 3).Trim(), CultureInfo.InvariantCulture));
			string state = "";
			for (int i = 0; (i < 20) && !state.Contains(wanted); i++) {
				System.Threading.Thread.Sleep(250);
				run(game, ".Test" + prefix + "State", prefix + "_STATE:");
				state = lastLine(prefix + "_STATE:");
			}
			Assert.Contains(wanted, state);
		}
	}

	[CollectionDefinition(Name)]
	public class Q008Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q008";
	}
}
