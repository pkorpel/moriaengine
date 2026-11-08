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

	/// <summary>#1286 - a GM starts the beacons of Gondor; the client double clicks the second beacon (refused: not its turn) and
	/// then the first (lit); the server lights the rest, lands corsairs at Pelargir and keeps a troll busy until sunrise.</summary>
	[Collection(WorldEventsCollection.Name)]
	public class OutdoorEventsTests : LivingWorldTestBase {
		public OutdoorEventsTests(GameServerPairFixture servers)
			: base(servers, "worldeventtests", "WORLDEVENTS") {
		}

		private string State(TestGameClient game, string want) {
			string state = "";
			for (int i = 0; (i < 40) && !state.Contains(want); i++) {
				System.Threading.Thread.Sleep(250);
				this.Run(game, ".TestWorldEventsState", "WORLDEVENTS_STATE:");
				state = this.LastLine("WORLDEVENTS_STATE:");
			}
			return state;
		}

		[Fact]
		public void BeaconsTrollsAndCorsairs() {
			using (TestGameClient game = this.Login("Strazce majaku")) {
				this.Run(game, ".TestWorldEventsSetup", "WORLDEVENTS_SETUP_DONE");
				game.SendSpeech(".udalost \"start majaky Minas_Tirith\"");
				Assert.Contains("kind=majaky city=Minas_Tirith lit=0", this.State(game, "kind=majaky"));

				this.Run(game, ".TestWorldEventsBeacon 1", "WORLDEVENTS_BEACON:");
				game.SendDoubleClick((uint) Number(this.LastLine("WORLDEVENTS_BEACON:"), "uid"));
				string refusal = "";
				for (int i = 0; (i < 30) && !refusal.Contains("Nejdriv musi horet majak cislo 1"); i++) {
					refusal = game.ReadSystemMessage(Wait); //the click reached the beacon and wasn't its turn
				}
				Assert.Contains("Nejdriv musi horet majak cislo 1", refusal);
				Assert.Contains("lit=0", this.State(game, "lit=0"));
				this.Run(game, ".TestWorldEventsBeacon 0", "WORLDEVENTS_BEACON:");
				game.SendDoubleClick((uint) Number(this.LastLine("WORLDEVENTS_BEACON:"), "uid"));
				Assert.Contains("lit=1", this.State(game, "lit=1"));

				this.Run(game, ".TestWorldEventsOutdoorChain", "WORLDEVENTS_OUTDOOR_DONE");
				this.AssertChecks("beaconsLit", "beaconsLost", "corsairs", "trollStone", "trollDawnAndRemoval", "trollLeft", "trollKilled");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WorldEventsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WorldEvents";
	}
}
