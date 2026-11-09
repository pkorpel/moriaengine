using System;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1333 - the roads out of the towns kept with the world: what .SetupWorld walked, what a restart brought back, and
	/// that using them walks nothing. Client side: Tests/Protocol/RoadsOutTests.cs.</summary>
	public static class RoadsOutTests {
		//the walks .SetupWorld makes: each city's centre in its town, each area of each city from its own centre
		private static List<string> Expected() {
			List<string> keys = new List<string>();
			foreach (string city in WorldEvents.Cities) {
				Point4D centre = WorldEvents.CentreOf(city);
				FlaggedRegion town = (centre == null) ? null : Errands.TownAt(city, centre.X, centre.Y, centre.M);
				if (town != null) {
					keys.Add(WorldEvents.RoadsOutKey(centre, town, Errands.OutsideBeyond, Errands.OutsideMaxSteps));
				}
				foreach (FlaggedRegion area in VenueRegistry.AreasOf(city)) {
					Point4D areaCentre = WorldEvents.CentreOfArea(area);
					if (areaCentre.M == area.Mapplane) {
						keys.Add(WorldEvents.RoadsOutKey(areaCentre, area, Errands.OutsideBeyond, Errands.OutsideMaxSteps));
					}
				}
			}
			return new List<string>(new HashSet<string>(keys)); //each once: a city's centre can be its main area's
		}

		/// <summary>How many roads are known, how many of .SetupWorld's walks are missing, how many walks since the boot.</summary>
		[SteamFunction]
		public static void TestRoadsOutReport(Character self) {
			List<string> expected = Expected();
			int missing = expected.FindAll(k => !WorldEvents.routesOut.ContainsKey(k)).Count, roads = 0;
			foreach (List<List<int[]>> walk in WorldEvents.routesOut.Values) {
				roads += walk.Count;
			}
			Console.WriteLine("ROADS_REPORT: known=" + WorldEvents.routesOut.Count + " expected=" + expected.Count + " missing=" + missing + " roads=" + roads
				+ " walked=" + WorldEvents.roadsWalked + " end");
		}

		/// <summary>Three things placed outside every city and every area, as errands would: from known roads this walks nothing.</summary>
		[SteamFunction]
		public static void TestRoadsOutUse(Character self) {
			int placed = 0, outside = 0;
			foreach (string city in WorldEvents.Cities) {
				Point4D centre = WorldEvents.CentreOf(city);
				FlaggedRegion town = (centre == null) ? null : Errands.TownAt(city, centre.X, centre.Y, centre.M);
				List<FlaggedRegion> towns = new List<FlaggedRegion>(VenueRegistry.AreasOf(city));
				if (town != null) {
					towns.Insert(0, town);
				}
				for (int i = 0; i < towns.Count; i++) {
					Point4D from = ((i == 0) && (town != null)) ? centre : WorldEvents.CentreOfArea(towns[i]);
					if (from.M != towns[i].Mapplane) {
						continue;
					}
					foreach (Point4D spot in Errands.OutsideSpots(from, towns[i], 3)) {
						placed++;
						if (!towns[i].ContainsPoint(spot.X, spot.Y)) {
							outside++;
						}
					}
				}
			}
			Console.WriteLine("ROADS_USE: placed=" + placed + " outside=" + outside + " walked=" + WorldEvents.roadsWalked + " end");
		}
	}
}
