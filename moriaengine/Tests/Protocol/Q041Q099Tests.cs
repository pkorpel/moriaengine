using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1278 - q041: a client greets the smuggler, gets his dialog and promises the money; the server walks the rest. Server
	/// side: scripts/Tests/Q041Tests.cs.</summary>
	[Collection(Q041Collection.Name)]
	public class Q041Tests : LivingWorldTestBase {
		public Q041Tests(GameServerPairFixture servers)
			: base(servers, "q041tests", "Q041") {
		}

		[Fact]
		public void TheSmuggler() {
			using (TestGameClient game = this.Login("Prebehlik Gondoru")) {
				this.Run(game, ".TestQ041Setup", "Q041_SETUP_DONE");
				game.SendSpeech("zdravim");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Paserak"));
				Assert.Contains(gump.Strings, s => s.Contains("Vidis mladeho muze"));
				Assert.Matches(@"gumppic 38 217 12644\b", gump.LayoutText); //0x3164
				Q008Q31Steps.Press(game, gump.GumpUid, "Q041", "q041_bring_money", "stage=1", this.RunFor, this.LastLineFor);
				this.Run(game, ".TestQ041Chain", "Q041_CHAIN_DONE");
				this.AssertChecks("theClientPromisedTheMoney", "whoHeTalksTo", "theBribe", "gondorKillsTheElf", "theElfOnlyAtFour", "mordorHelpsBorek", "thePrice",
					"thePortal", "toGondor", "toMordorAndAgain", "journalTextsArePlain");
			}
		}

		private void RunFor(TestGameClient game, string command, string marker) {
			this.Run(game, command, marker);
		}

		private string LastLineFor(string marker) {
			return this.LastLine(marker);
		}
	}

	/// <summary>#1278 - q099: an outcast greets Podivin, gets his dialog and takes the first bag; the server walks the rest. Server
	/// side: scripts/Tests/Q099Tests.cs.</summary>
	[Collection(Q041Collection.Name)]
	public class Q099Tests : LivingWorldTestBase {
		public Q099Tests(GameServerPairFixture servers)
			: base(servers, "q041tests", "Q099") {
		}

		[Fact]
		public void TheOutcastsWayBack() {
			using (TestGameClient game = this.Login("Vyvrhel Gondoru")) {
				this.Run(game, ".TestQ099Setup", "Q099_SETUP_DONE");
				game.SendSpeech("dobry den");
				var gump = game.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(gump.Strings, s => s.Contains("Podivin"));
				Assert.Contains(gump.Strings, s => s.Contains("Bud zdrav, vyvrheli!"));
				Assert.Matches(@"gumppic 38 217 12336\b", gump.LayoutText); //0x3030
				Q008Q31Steps.Press(game, gump.GumpUid, "Q099", "q099:1", "stage=2", this.RunFor, this.LastLineFor);
				this.Run(game, ".TestQ099Chain", "Q099_CHAIN_DONE");
				this.AssertChecks("theClientTookTheFirstBag", "outcastsOnly", "theFourBags", "miningForty", "redeemedToGondor", "mordorBackAndAgain", "levelFloor",
					"journalTextsArePlain");
			}
		}

		private void RunFor(TestGameClient game, string command, string marker) {
			this.Run(game, command, marker);
		}

		private string LastLineFor(string marker) {
			return this.LastLine(marker);
		}
	}

	[CollectionDefinition(Name)]
	public class Q041Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Q041";
	}
}
