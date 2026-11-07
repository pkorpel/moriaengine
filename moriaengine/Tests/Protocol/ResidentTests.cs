using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1242 - residents: a tavern of Edoras filled, names, homes, the schedule by the hour, friendship, a death and the same
	/// person back, the export round trip, orcs in Barad-dur and hobbits in Hobitin; and .resident through a client.
	/// Server side: scripts/Tests/ResidentTests.cs.</summary>
	[Collection(ResidentCollection.Name)]
	public class ResidentTests : LivingWorldTestBase {
		public ResidentTests(GameServerPairFixture servers)
			: base(servers, "residenttests", "RESIDENT") {
		}

		[Fact]
		public void ATavernIsFilledAndItsPeopleKeepTheirHours() {
			using (TestGameClient game = this.Login("Census")) {
				this.Run(game, ".TestResidentFill", "RESIDENT_FILL_DONE");
				this.AssertChecks("edorasHasTwoRooms", "aTavernGetsItsSevenPeople", "fillingAgainAddsNobody", "everyoneHasAName", "namesDifferInTheCity",
					"everyoneHasABody", "theBodyCarriesTheNameAndRole", "theCitysHouseIsHome", "idsFollowTheRole", "residentsOfEdorasAreGondors", "humansInEdoras",
					"theyAreCalm", "atMiddayTheInnkeeperIsAtWork", "andThePatronAtHome", "inTheEveningThePatronHeadsForTheTavern", "andTheCookGoesHome", "aSociableCookGoesToTheTavernInstead",
					"atNightTheInnkeeperIsHomeToo", "theLeashIsSmall", "eachHasAFixedSpot", "theySpreadOverTheRoom", "theFriendshipKeyLoadsBack", "aStrangerIsZero", "helpRaisesIt", "itStopsAtTen", "andAtZero",
					"deadHasNoBody", "theGemBringsTheSamePersonBack", "theSchedulerRuns");
				this.Run(game, ".TestResidentExport", "RESIDENT_EXPORT_DONE");
				this.AssertChecks("thereAreResidents", "removingTheVenueRemovesItsPeople", "nobodyLeft", "setupBringsBackTheSamePeople", "withBodiesNamedAtOnce",
					"aSecondRunAddsNobody");
				this.Run(game, ".TestResidentRaces", "RESIDENT_RACES_DONE");
				this.AssertChecks("bothVenuesExist", "baradDursFolkAreOrcs", "ofMordor", "hobitinHasHobbitNames");

				//.resident on the innkeeper, renamed through the dialog
				this.Run(game, ".TestResidentBeside", "RESIDENT_BESIDE");
				string beside = this.LastLine("RESIDENT_BESIDE");
				Assert.DoesNotContain("none", beside);
				uint uid = uint.Parse(Regex.Match(beside, @"uid=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
				game.SendSpeech(".resident");
				Thread.Sleep(500);
				game.SendTargetUid(uid);
				var dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.Contains("Hostinsky"));
				game.SendGumpResponse(dialog.GumpUid, 1, new Dictionary<int, string> { { 10, "Hamund" } });
				game.ReadGumpDialogWithStrings(Wait);
				this.Run(game, ".TestResidentRenamed", "RESIDENT_RENAME_DONE");
				this.AssertChecks("theDialogRenamedHim");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ResidentCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Resident";
	}
}
