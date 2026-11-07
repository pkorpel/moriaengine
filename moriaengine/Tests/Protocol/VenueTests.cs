using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1240 - venues: the room scan on a real Edoras building, a venue from it, export and .SetupWorld bringing it back,
	/// candidate rooms; and an open-air venue made through .venue's dialog and cursor corners. Server side: scripts/Tests/VenueTests.cs.</summary>
	[Collection(VenueCollection.Name)]
	public class VenueTests : LivingWorldTestBase {
		public VenueTests(GameServerPairFixture servers)
			: base(servers, "venuetests", "VENUE") {
		}

		[Fact]
		public void ARoomIsFoundSavedExportedAndBroughtBack() {
			using (TestGameClient game = this.Login("Mapper")) {
				this.Run(game, ".TestVenueScan", "VENUE_SCAN_DONE");
				this.AssertChecks("anEdorasRoomIsFound", "theRoomIsBounded", "everyTileHasARoof", "theRectanglesCoverExactlyTheTiles", "noRoofNoRoom",
					"aDraftSaves", "theCityIsFoundByItself", "theIdIsPermanent", "theMarkerIsHidden", "aTileInsideIsTheVenue", "theStreetIsNot",
					"anUpperFloorIsNot", "aTavernIsOpenInTheEvening", "andClosedAtDawn", "theTypeHasRoles", "twentyFourHoursIsAlways", "theClockHasShifts",
					"aSecondVenueGetsTheNextId", "theListFindsIt", "edorasHasOtherRooms", "aMarkedRoomIsntProposed");
				this.Run(game, ".TestVenueExport", "VENUE_EXPORT_DONE");
				this.AssertChecks("theVenueIsThere", "oneIsWritten", "deletedIsGone", "setupBringsItBack", "withItsTypeNameAndArea", "hiddenAndFixed",
					"aSecondRunAddsNothing", "andKeepsTheGmsChange");
			}
		}

		[Fact]
		public void AnOpenAirVenueThroughTheDialogAndCorners() {
			using (TestGameClient game = this.Login("Marketeer")) {
				this.Run(game, ".TestVenueOpenAirPrepare", "VENUE_OPEN_READY");
				string ready = this.LastLine("VENUE_OPEN_READY");
				Assert.DoesNotContain("none", ready);
				int x = Number(ready, "x"), y = Number(ready, "y"), z = Number(ready, "z");

				game.SendSpeech(".venue");
				var dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.Contains("Nove misto"));
				Assert.Contains(dialog.Strings, s => s.Contains("Edoras"));
				game.SendGumpResponse(dialog.GumpUid, 3, new Dictionary<int, string>()); //corners by cursor
				Thread.Sleep(500);
				game.SendTargetGround(x, y, z);
				Thread.Sleep(500);
				game.SendTargetGround(x + 4, y + 3, z);
				dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.Contains("1 obdelnik"));
				game.SendGumpResponse(dialog.GumpUid, 11, new Dictionary<int, string>()); //next type: the bath house
				dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.Contains("Lazne"));
				game.SendGumpResponse(dialog.GumpUid, 1, new Dictionary<int, string> { { 20, "Dobytci trh" }, { 21, "7" }, { 22, "19" } });
				Thread.Sleep(500);

				this.Run(game, ".TestVenueOpenAirReport", "VENUE_OPEN_DONE");
				this.AssertChecks("theDialogSavedTheMarket", "itIsOpenAir", "withOneRectangleFromTheCorners", "ofTheTypePicked", "withTheHoursTyped");

				game.SendSpeech(".venues");
				dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.Contains("Dobytci trh"));
			}
		}
	}

	[CollectionDefinition(Name)]
	public class VenueCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Venue";
	}
}
