using System;
using System.Collections.Generic;
using System.Diagnostics;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1329 - an Outside step's things lie outside the town, wherever the road out winds: the old walk of 30 steps stayed
	/// inside Edoras about half the time (its hill) and always inside Minas Tirith. Every city of the scheduler is tried.
	/// Client side: Tests/Protocol/OutsideTownTests.cs.</summary>
	public static class OutsideTownTests {
		private const int OldTries = 10, Tries = 5, EdorasTries = 20; //few: the old walk isn't kept, and costs ~0.1-0.2 s each; Edoras, the case, more

		private static void Report(string name, bool ok) {
			Console.WriteLine("OUTSIDE_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestOutsideTown(Character self) {
			bool edorasStaysIn = false, minasTirithStaysIn = false, edorasOutside = false, everyTown = true;
			foreach (string city in WorldEvents.Cities) {
				Point4D centre = WorldEvents.CentreOf(city);
				FlaggedRegion town = (centre == null) ? null : Errands.TownAt(city, centre.X, centre.Y, centre.M);
				if (town == null) {
					Console.WriteLine("OUTSIDE_CITY: " + city + " no town at its centre");
					everyTown = false;
					continue;
				}
				//the old walk: how many of its roads end inside the town
				int oldInside = 0;
				for (int i = 0; i < OldTries; i++) {
					List<int[]> old = WorldEvents.RouteIn(centre, Errands.OutsideSteps);
					if ((old != null) && town.ContainsPoint(old[0][0], old[0][1])) {
						oldInside++;
					}
				}
				Errand errand = new Errand { key = "outsidetest", name = "test", city = city };
				ErrandStep step = Errands.Things(WildStories.PoolDef, 3, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false, "", "");
				int inside = 0, placed = 0, stacked = 0, tries = (city == "Edoras") ? EdorasTries : Tries;
				Stopwatch watch = Stopwatch.StartNew();
				List<List<int[]>> roads = WorldEvents.RoutesOutOf(centre, town, Errands.OutsideBeyond, Errands.OutsideMaxSteps); //the first walk: found and kept
				List<int[]> road = (roads.Count == 0) ? null : roads[0];
				long coldMs = watch.ElapsedMilliseconds;
				watch.Restart();
				for (int i = 0; i < tries; i++) {
					List<Point4D> spots = Errands.ThingSpots(errand, step, 3, "outsidetest/" + i);
					for (int j = 1; j < spots.Count; j++) {
						if (spots.FindIndex(o => (o.X == spots[j].X) && (o.Y == spots[j].Y)) < j) {
							stacked++; //two things on one tile: the stretch past the edge was too short
						}
					}
					foreach (Point4D spot in spots) {
						placed++;
						if ((spot.M != town.Mapplane) || town.ContainsPoint(spot.X, spot.Y)) {
							inside++;
						}
					}
				}
				watch.Stop();
				int stretch = Stretch(road, town);
				bool outside = (placed == tries * 3) && (inside == 0) && (stacked == 0) && (stretch >= Errands.OutsideBeyond);
				Console.WriteLine("OUTSIDE_CITY: " + city + " town=" + town.Defname + " oldInside=" + oldInside + "/" + OldTries + " placed=" + placed
					+  " inside=" + inside + " stacked=" + stacked + " stretch=" + stretch + " coldMs=" + coldMs + " warmMsAll=" + watch.ElapsedMilliseconds);
				if (city == "Edoras") {
					edorasStaysIn = oldInside > 0;
					edorasOutside = outside;
				} else if (city == "Minas_Tirith") {
					minasTirithStaysIn = oldInside == OldTries;
				}
				everyTown &= outside;
			}
			Report("theOldWalkStaysInside", edorasStaysIn && minasTirithStaysIn); //the case: 30 steps can stay inside Edoras, always inside Minas Tirith
			Report("edorasThingsLieOutside", edorasOutside);
			Report("everyTownsThingsLieOutside", everyTown);
			Console.WriteLine("OUTSIDE_DONE");
		}

		//how many tiles the road begins with outside the town
		private static int Stretch(List<int[]> road, StaticRegion town) {
			int past = 0;
			while ((road != null) && (past < road.Count) && !town.ContainsPoint(road[past][0], road[past][1])) {
				past++;
			}
			return past;
		}

		private static bool everyArea = true;

		/// <summary>Every area of one city (by its index in WorldEvents.Cities): three things placed outside it from its own centre, as
		/// for a giver working there, each on its own tile: Pelargir and Linhir of Dol Amroth, Esgaroth of Dale, Lorien of the woodland realm... One city a command,
		/// as a first walk out of a big town takes up to a second.</summary>
		[SteamFunction]
		public static void TestOutsideAreas(Character self, ScriptArgs sa) {
			int index = int.Parse(sa.Args.Trim().Trim('"'));
			string city = WorldEvents.Cities[index];
			if (index == 0) {
				everyArea = true;
			}
			List<FlaggedRegion> areas = VenueRegistry.AreasOf(city);
			everyArea &= areas.Count > 0; //a city with no areas would pass unseen
			foreach (FlaggedRegion area in areas) {
				Point4D centre = WorldEvents.CentreOfArea(area);
				Stopwatch watch = Stopwatch.StartNew();
				List<Point4D> spots = Errands.OutsideSpots(centre, area, 3);
				long ms = watch.ElapsedMilliseconds;
				List<List<int[]>> roads = WorldEvents.RoutesOutOf(centre, area, Errands.OutsideBeyond, Errands.OutsideMaxSteps); //kept by now
				int stretch = (roads.Count == 0) ? 0 : Stretch(roads[0], area);
				//three things, each on its own tile outside the area (an island's short shore takes other roads' ends)
				bool ok = (spots.Count == 3) && spots.TrueForAll(o => (o.M == area.Mapplane) && !area.ContainsPoint(o.X, o.Y))
					&& (spots.FindAll(o => (o.X == spots[0].X) && (o.Y == spots[0].Y)).Count == 1) && ((spots[1].X != spots[2].X) || (spots[1].Y != spots[2].Y));
				everyArea &= ok;
				Console.WriteLine("OUTSIDE_AREA: " + city + " " + area.Defname + " roads=" + roads.Count
					+ " stretch=" + stretch + " ok=" + ok + " ms=" + ms);
			}
			Console.WriteLine("OUTSIDE_AREAS_DONE " + index + " end");
		}

		[SteamFunction]
		public static void TestOutsideAreasReport(Character self) {
			Report("everyAreasRoadLeavesIt", everyArea);
			Console.WriteLine("OUTSIDE_AREAS_REPORTED");
		}
	}
}
