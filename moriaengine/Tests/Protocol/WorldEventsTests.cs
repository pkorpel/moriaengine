using System;
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

				//#1315 - .udalost's dialog: a raid started on Edoras by kind and place, listed, stopped
				this.Run(game, ".TestWorldEventsSetup", "WORLDEVENTS_SETUP_DONE");
				this.Run(game, ".TestWorldEventsDialogButtons", "WORLDEVENTS_DIALOG:");
				string buttons = this.LastLine("WORLDEVENTS_DIALOG:");
				game.SendSpeech(".udalost");
				var dialog = ReadUntil(game, "Udalosti ve svete");
				Assert.Contains("Ted nic nebezi.", dialog.Strings);
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "kind"));
				dialog = ReadUntil(game, "Zacit: ");
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "place"));
				dialog = ReadUntil(game, "Udalosti ve svete");
				Assert.Contains(dialog.Strings, s => s.EndsWith(", Edoras"));
				this.Run(game, ".TestWorldEventsState", "WORLDEVENTS_STATE:");
				Assert.Contains("running=1 kind=najezd city=Edoras", this.LastLine("WORLDEVENTS_STATE:"));
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "stop"));
				dialog = ReadUntil(game, "Udalosti ve svete");
				Assert.Contains("Ted nic nebezi.", dialog.Strings);
				this.Run(game, ".TestWorldEventsState", "WORLDEVENTS_STATE:");
				Assert.Contains("running=0", this.LastLine("WORLDEVENTS_STATE:"));

				//#1335 - the raid's description from its info button: what it does and where; on to its places, back to the
				//description by Popis, then back to the overview
				Assert.Matches(@"4011\s+4013\s+1\s+0\s+" + Number(buttons, "info") + @"\b", dialog.LayoutText); //the info button is drawn: a server takes any button number sent
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "info"));
				dialog = ReadUntil(game, "Skreti najezd (najezd)");
				Assert.Contains(dialog.Strings, s => s.StartsWith("Kdy: sam od sebe za soumraku", StringComparison.Ordinal) && s.Contains("zkusenosti"));
				Assert.Contains(dialog.Strings, s => s.Contains("Kde: ") && s.Contains("Edoras") && s.Contains("Minas Tirith"));
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "start"));
				dialog = ReadUntil(game, "Zacit: Skreti najezd");
				Assert.Matches(@"4011\s+4013\s+1\s+0\s+" + Number(buttons, "describe") + @"\b", dialog.LayoutText);
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "describe"));
				dialog = ReadUntil(game, "Skreti najezd (najezd)");
				Assert.Contains(dialog.Strings, s => s.StartsWith("Kdy: sam od sebe za soumraku", StringComparison.Ordinal)); //Popis gave the description, not the overview
				game.SendGumpResponse(dialog.GumpUid, Number(buttons, "back"));
				dialog = ReadUntil(game, "Udalosti ve svete");
				this.Run(game, ".TestWorldEventsDescriptions", "WORLDEVENTS_DESCRIPTIONS_DONE");
				this.AssertChecks("everyKindIsDescribed", "theDescriptionReadsTheSettings");
				game.SendGumpResponse(dialog.GumpUid, 0);
			}
		}

		//the next gump with this text start (skipping any the server sent before it)
		private static (int GumpUid, string LayoutText, System.Collections.Generic.List<string> Strings) ReadUntil(TestGameClient game, string start) {
			var dialog = game.ReadGumpDialogWithStrings(Wait);
			for (int i = 0; (i < 5) && !dialog.Strings.Exists(s => s.StartsWith(start, StringComparison.Ordinal)); i++) {
				dialog = game.ReadGumpDialogWithStrings(Wait);
			}
			Assert.Contains(dialog.Strings, s => s.StartsWith(start, StringComparison.Ordinal));
			return dialog;
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

	/// <summary>#1286 - fire and fever: the client beats a fire out with three double clicks and cures a sick resident with a
	/// double click (the resident's usual talk gives way to the cure); the server walks the rest.</summary>
	[Collection(WorldEventsCollection.Name)]
	public class CityEventsTests : LivingWorldTestBase {
		public CityEventsTests(GameServerPairFixture servers)
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
		public void FiresAreBeatenOutAndFeversCured() {
			using (TestGameClient game = this.Login("Hasic Edorasu")) {
				this.Run(game, ".TestWorldEventsCitySetup", "WORLDEVENTS_CITY:");
				string city = this.LastLine("WORLDEVENTS_CITY:");
				Assert.Contains("house=1", city);
				int burning = Number(city, "burning");
				uint fire = (uint) Number(city, "fire");
				for (int i = 0; i < 3; i++) {
					game.SendDoubleClick(fire);
					System.Threading.Thread.Sleep(400);
				}
				Assert.Contains("burning=" + (burning - 1), this.State(game, "burning=" + (burning - 1)));

				this.Run(game, ".TestWorldEventsFeverStart", "WORLDEVENTS_FEVER:");
				string fever = this.LastLine("WORLDEVENTS_FEVER:");
				int sick = Number(fever, "sick");
				game.SendDoubleClick((uint) Number(fever, "uid"));
				Assert.Contains("sick=" + (sick - 1), this.State(game, "sick=" + (sick - 1)));

				this.Run(game, ".TestWorldEventsCityChain", "WORLDEVENTS_CITY_DONE");
				this.AssertChecks("feverCured", "feverLost", "feverDeaths", "fireBlows", "fireOut", "fireOneBlowUnpaid", "fireLost");
			}
		}
	}

	/// <summary>#1289 - a festival by the calendar: midsummer's day starts Minas Tirith's; the client asks the host for food and
	/// catches the runaway piglet; the server walks the game, the end, the calendar and Mordor's day of the Eye.</summary>
	[Collection(WorldEventsCollection.Name)]
	public class FestivalTests : LivingWorldTestBase {
		public FestivalTests(GameServerPairFixture servers)
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
		public void FestivalsComeWithTheirDays() {
			using (TestGameClient game = this.Login("Host slavnosti")) {
				this.Run(game, ".TestWorldEventsFestivalSetup", "WORLDEVENTS_FESTIVAL:");
				string setup = this.LastLine("WORLDEVENTS_FESTIVAL:");
				Assert.Contains("started=1", setup);
				game.SendDoubleClick((uint) Number(setup, "host"));
				Assert.Contains("kind=slunovrat city=Minas_Tirith fed=1", this.State(game, "fed=1"));

				this.Run(game, ".TestWorldEventsFestivalAnimal", "WORLDEVENTS_ANIMAL:");
				game.SendDoubleClick((uint) Number(this.LastLine("WORLDEVENTS_ANIMAL:"), "uid"));
				Assert.Contains("rounds=1", this.State(game, "rounds=1"));

				this.Run(game, ".TestWorldEventsFleeProbe", "WORLDEVENTS_FLEE:");
				int near = Number(this.LastLine("WORLDEVENTS_FLEE:"), "dist");
				System.Threading.Thread.Sleep(3000); //the animal's own clock runs it off
				this.Run(game, ".TestWorldEventsFleeCheck", "WORLDEVENTS_FLED:");
				string fled = this.LastLine("WORLDEVENTS_FLED:");
				Assert.True((Number(fled, "orders") >= 1) && (Number(fled, "dist") > near), "The runaway animal didn't run: " + fled); //sent running by its own clock, and away

				this.Run(game, ".TestWorldEventsFestivalChain", "WORLDEVENTS_FESTIVAL_DONE");
				this.AssertChecks("festivalFood", "festivalGame", "festivalEnds", "festivalCalendar", "festivalMordor");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WorldEventsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WorldEvents";
	}
}
