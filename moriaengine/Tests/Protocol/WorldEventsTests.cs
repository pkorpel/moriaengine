using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1284 - the world events: a GM starts an orc raid on Edoras with .udalost through a client and reads it in the
	/// journal's Udalosti tab; the server then beats it and loses one, runs a caravan to the end and to its loss, clears a past
	/// run's leftovers and walks the scheduler. Server side: scripts/Tests/WorldEventsTests.cs.</summary>
	[Collection(WorldEventsCollection.Name)]
	public class WorldEventsTests : LivingWorldTestBase {
		public WorldEventsTests(GameServerPairFixture servers)
			: base(servers, "worldeventtests", "WORLDEVENTS") {
		}

		[Fact]
		public void RaidsAndCaravansComeAndGo() {
			using (TestGameClient game = this.Login("Obrance Edorasu")) {
				this.Run(game, ".TestWorldEventsSetup", "WORLDEVENTS_SETUP_DONE");

				game.SendSpeech(".udalost \"start najezd Edoras\"");
				string state = "";
				for (int i = 0; (i < 40) && !state.Contains("running=1"); i++) {
					System.Threading.Thread.Sleep(250);
					this.Run(game, ".TestWorldEventsState", "WORLDEVENTS_STATE:");
					state = this.LastLine("WORLDEVENTS_STATE:");
				}
				Assert.Contains("running=1 kind=najezd city=Edoras", state);

				this.Run(game, ".TestWorldEventsJournal", "WORLDEVENTS_JOURNAL_DONE");
				var journal = game.ReadGumpDialogWithStrings(Wait);
				for (int i = 0; (i < 5) && !journal.Strings.Contains("Prave se deje"); i++) {
					journal = game.ReadGumpDialogWithStrings(Wait); //anything the server sent before it
				}
				Assert.Contains("Prave se deje", journal.Strings);
				Assert.Contains(journal.Strings, s => s.StartsWith("Skreti najezd na Edoras"));

				this.Run(game, ".TestWorldEventsChain", "WORLDEVENTS_CHAIN_DONE");
				this.AssertChecks("raidRules", "raidStarted", "raidWon", "noStrangersCredited", "raidLost", "startRefused", "caravanAmbush",
					"caravanArrived", "bystanderUnpaid", "caravanLost", "leftoversCleared", "scheduler");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WorldEventsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WorldEvents";
	}
}
