using System;
using System.Collections.Generic;
using System.IO;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1240 - venues checked on the server; Tests/Protocol/VenueTests.cs drives these and the dialog through a client.</summary>
	public static class VenueTests {
		//a roofed building of Edoras (the statics scan of #1240 found a 55-tile room by 2316,2509)
		private const int EdorasX = 2310, EdorasY = 2505, Window = 24;

		private static void Report(string name, bool ok) {
			Console.WriteLine("VENUE_CHECK: " + name + "=" + ok);
		}

		/// <summary>The first covered spot near the Edoras building, where a GM would stand.</summary>
		private static Point4D EdorasRoomSpot() {
			Map map = Map.GetMap(0);
			for (int x = EdorasX; x < EdorasX + Window; x++) {
				for (int y = EdorasY; y < EdorasY + Window; y++) {
					int z = WorldSpots.LowestStandZ(map, x, y);
					if (RoomScan.IsCovered(map, x, y, z)) {
						FoundRoom room = RoomScan.Scan(map, new Point3D(x, y, z), settings);
						if ((room != null) && (room.Tiles.Count >= RoomScan.MinRoomTiles)) {
							return new Point4D(x, y, z, 0);
						}
					}
				}
			}
			return null;
		}

		private static IMovementSettings settings;

		[SteamFunction]
		public static void TestVenueScan(Character self) {
			settings = self.MovementSettings;
			VenueRegistry.Reset();
			foreach (CityVenue old in new List<CityVenue>(VenueRegistry.All)) {
				old.Delete();
			}
			Point4D spot = EdorasRoomSpot();
			Report("anEdorasRoomIsFound", spot != null);
			if (spot == null) {
				Console.WriteLine("VENUE_SCAN_DONE");
				return;
			}
			Map map = Map.GetMap(0);
			FoundRoom room = RoomScan.Scan(map, spot, self.MovementSettings);
			int area = 0;
			foreach (ImmutableRectangle rect in room.Rectangles) {
				area += (rect.Width + 1) * (rect.Height + 1);
			}
			bool everyTileCovered = true, everyTileInARect = true;
			foreach (int[] tile in room.Tiles) {
				everyTileCovered &= RoomScan.IsCovered(map, tile[0], tile[1], tile[2]);
				bool inRect = false;
				foreach (ImmutableRectangle rect in room.Rectangles) {
					inRect |= rect.Contains(tile[0], tile[1]);
				}
				everyTileInARect &= inRect;
			}
			Console.WriteLine("VENUE_SCAN: spot=" + spot + " tiles=" + room.Tiles.Count + " rects=" + room.Rectangles.Count + " z=" + room.ZMin + ".." + room.ZMax + " tooBig=" + room.TooBig);
			Report("theRoomIsBounded", !room.TooBig && (room.Tiles.Count < 400));
			Report("everyTileHasARoof", everyTileCovered);
			Report("theRectanglesCoverExactlyTheTiles", everyTileInARect && (area == room.Tiles.Count));
			Report("noRoofNoRoom", RoomScan.Scan(map, new Point3D(spot.X, spot.Y, spot.Z + 60), self.MovementSettings) == null);

			//a venue from it
			self.Go(spot);
			VenueDraft draft = VenueDraft.At(self);
			draft.SetType(VenueType.Get("tavern").IndexOf());
			draft.Title = "U Zlateho bazanta";
			string problem;
			CityVenue venue = draft.Save(out problem);
			Report("aDraftSaves", venue != null);
			Report("theCityIsFoundByItself", (venue != null) && (venue.City == "Edoras"));
			Report("theIdIsPermanent", (venue != null) && (venue.VenueId == "venue/Edoras/1"));
			Report("theMarkerIsHidden", (venue != null) && venue.HasAttribute(ItemAttributes.attr_invis));
			Report("aTileInsideIsTheVenue", VenueRegistry.At(spot) == venue);
			Report("theStreetIsNot", VenueRegistry.At(new Point4D(spot.X + 200, spot.Y, spot.Z, 0)) == null);
			Report("anUpperFloorIsNot", VenueRegistry.At(new Point4D(spot.X, spot.Y, spot.Z + 40, 0)) == null);
			Report("aTavernIsOpenInTheEvening", venue.IsOpenAt(20) && venue.IsOpenAt(1));
			Report("andClosedAtDawn", !venue.IsOpenAt(5));
			Report("theTypeHasRoles", venue.VenueType.RoleCount == 7);
			Report("twentyFourHoursIsAlways", VenueClock.IsOpen(0, 24, 3) && VenueClock.IsOpen(0, 24, 23.5));
			Report("theClockHasShifts", (VenueClock.ShiftAt(7) == VenueShift.Morning) && (VenueClock.ShiftAt(12) == VenueShift.Day) &&
				(VenueClock.ShiftAt(19) == VenueShift.Evening) && (VenueClock.ShiftAt(23) == VenueShift.Night));
			Report("aSecondVenueGetsTheNextId", VenueRegistry.NextId("Edoras") == "venue/Edoras/2");
			Report("theListFindsIt", VenueRegistry.InCity("Edoras").Contains(venue));

			//the same room isn't proposed again, others are
			List<FoundRoom> rooms = RoomScan.Candidates("Edoras", self.MovementSettings);
			bool overlaps = false;
			int rentedTiles = 0;
			Map venueMap = Map.GetMap(venue.M);
			foreach (FoundRoom candidate in rooms) {
				foreach (int[] tile in candidate.Tiles) {
					overlaps |= venue.Contains(tile[0], tile[1], tile[2]);
					rentedTiles += RoomScan.IsRental(venueMap, tile[0], tile[1]) ? 1 : 0;
				}
			}
			//#1312 - Edoras has player houses (dum), none of them is proposed
			int edorasHouses = 0;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion house = region as FlaggedRegion;
				if ((house != null) && (house.HouseNumber != 0) && (house.Mapplane == venue.M)) {
					ImmutableRectangle first = System.Linq.Enumerable.First(house.Rectangles);
					edorasHouses += (VenueRegistry.CityAt(new Point4D(first.MinX, first.MinY, 0, (byte) house.Mapplane)) == "Edoras") ? 1 : 0;
				}
			}
			Console.WriteLine("VENUE_HOUSES: Edoras=" + edorasHouses + " rentedTiles=" + rentedTiles);
			Report("edorasHasHouses", edorasHouses > 0);
			Report("noHouseIsProposed", rentedTiles == 0);
			Console.WriteLine("VENUE_CANDIDATES: Edoras=" + rooms.Count);
			Report("edorasHasOtherRooms", rooms.Count > 0);
			Report("aMarkedRoomIsntProposed", !overlaps);
			Console.WriteLine("VENUE_SCAN_DONE");
		}

		[SteamFunction]
		public static void TestVenueExport(Character self) {
			CityVenue venue = VenueRegistry.ById("venue/Edoras/1");
			Report("theVenueIsThere", venue != null);
			if (venue == null) {
				Console.WriteLine("VENUE_EXPORT_DONE");
				return;
			}
			string path = Path.Combine(Path.GetTempPath(), "moria-venues-" + Guid.NewGuid().ToString("N") + ".txt");
			VenueFile.PathOverride = path;
			try {
				string rects = CityVenue.FormatRects(venue.Rectangles);
				int zMin = venue.ZMin;
				Report("oneIsWritten", VenueFile.Export(path) == 1);
				venue.Delete();
				Report("deletedIsGone", VenueRegistry.ById("venue/Edoras/1") == null);
				//a venue marked on a rebuilt world before .SetupWorld ran doesn't take the file's id
				Report("aNewIdSkipsTheFilesIds", VenueRegistry.NextId("Edoras") != "venue/Edoras/1");
				new VenueSetup().Run(self);
				CityVenue back = VenueRegistry.ById("venue/Edoras/1");
				Report("setupBringsItBack", back != null);
				Report("withItsTypeNameAndArea", (back != null) && (back.VenueType.Key == "tavern") && (back.Title == "U Zlateho bazanta") &&
					(CityVenue.FormatRects(back.Rectangles) == rects) && (back.ZMin == zMin) && (back.City == "Edoras"));
				Report("hiddenAndFixed", (back != null) && back.HasAttribute(ItemAttributes.attr_invis) && back.HasAttribute(ItemAttributes.attr_move_never));
				if (back == null) {
					return;
				}
				back.Title = "Prejmenovana";
				new VenueSetup().Run(self);
				int count = 0, total = VenueRegistry.All.Count;
				foreach (CityVenue v in VenueRegistry.All) {
					count += (v.VenueId == "venue/Edoras/1") ? 1 : 0;
				}
				new VenueSetup().Run(self);
				Report("aSecondRunAddsNothing", (count == 1) && (VenueRegistry.All.Count == total));

				Report("andKeepsTheGmsChange", VenueRegistry.ById("venue/Edoras/1").Title == "Prejmenovana");
			} finally {
				VenueFile.PathOverride = null;
				File.Delete(path);
				Console.WriteLine("VENUE_EXPORT_DONE");
			}
		}

		/// <summary>An open-air spot of Edoras for the corners test, and the GM put there.</summary>
		[SteamFunction]
		public static void TestVenueOpenAirPrepare(Character self) {
			Map map = Map.GetMap(0);
			for (int x = EdorasX + 30; x < EdorasX + 70; x++) {
				int z;
				if (WorldSpots.CanStand(map, x, EdorasY + 40, out z) && !RoomScan.IsCovered(map, x, EdorasY + 40, z) && (VenueRegistry.At(new Point4D(x, EdorasY + 40, z, 0)) == null)) {
					self.Go(new Point4D(x, EdorasY + 40, z, 0));
					Console.WriteLine("VENUE_OPEN_READY x=" + x + " y=" + (EdorasY + 40) + " z=" + z);
					return;
				}
			}
			Console.WriteLine("VENUE_OPEN_READY none");
		}

		[SteamFunction]
		public static void TestVenueOpenAirReport(Character self) {
			CityVenue market = null;
			foreach (CityVenue venue in VenueRegistry.All) {
				if (venue.Title == "Dobytci trh") {
					market = venue;
				}
			}
			Report("theDialogSavedTheMarket", market != null);
			Report("itIsOpenAir", (market != null) && market.IsOpenAir);
			Report("withOneRectangleFromTheCorners", (market != null) && (market.Rectangles.Count == 1) && (market.Rectangles[0].Width == 4) && (market.Rectangles[0].Height == 3));
			Report("ofTheTypePicked", (market != null) && (market.VenueType.Key == "bathhouse"));
			Report("withTheHoursTyped", (market != null) && (market.Opens == 7) && (market.Closes == 19));
			//#1315 - the client lowered the bathers from the type's 2 to 1: one left, and the count goes into the export and back
			int bathers = 0;
			bool exported = false;
			if (market != null) {
				foreach (Item gem in Residents.OfVenue(market.VenueId)) {
					bathers += (Residents.RoleKeyOf(gem) == "bather") ? 1 : 0;
				}
				string path = Path.Combine(Path.GetTempPath(), "moria-venues-" + Guid.NewGuid().ToString("N") + ".txt");
				try {
					VenueFile.Export(path, out int residents);
					exported = VenueFile.Read(path).Exists(line => (line.Id == market.VenueId) && (line.Counts == "bather=1") && (line.Title == "Dobytci trh"));
				} finally {
					File.Delete(path);
				}
			}
			Report("theBathersAreOne", (market != null) && (bathers == 1) && (Residents.RoleCountOf(market) == 3) && (Residents.CountsText(market) == "bather=1"));
			Report("andTheCountIsExported", exported);
			Console.WriteLine("VENUE_OPEN_DONE");
		}
	}
}
