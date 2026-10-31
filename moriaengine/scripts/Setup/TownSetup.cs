using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#892 - the stones of the Gondor cities. Erebor's were placed by GMs and aren't in the
	/// archive, so these spots are proposals: a few steps from each city's spawn point, inside its map
	/// area. The tax chest comes with the stone's first use. A GM moves a stone like any other piece.</summary>
	public sealed class TownSetup : WorldSetup {
		public const string StoneDef = "i_townstone";
		private const int StepsFromSpawnPoint = 3;

		/// <summary>The cities of map areas with tag.realm=1 and tag.jmeno_mesta, by tag.jmeno_mesta. The test
		/// town London and the abandoned Dainovo kralovstvi aren't among them.</summary>
		public static readonly string[] GondorCities = {
			"Minas_Tirith", "Edoras", "Dol_Amroth", "Fornost", "Hobitin", "Erebor", "Dol", "Sede_pristavy", "Kralovstvi_lesnich_elfu"
		};

		/// <summary>The realm's stones stand in its capital, as far on as a stone's own spot isn't taken.</summary>
		public const string GeneralCity = "Minas_Tirith";
		public const string GeneralStoneDef = "i_generalstone";

		public override string Name {
			get {
				return "Town stones";
			}
		}

		[SteamFunction]
		public static void SetupTowns(Character self) {
			new TownSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (string city in GondorCities) {
				FlaggedRegion area = CityArea(city);
				int[] spot = (area != null) ? SpotIn(area, gm) : null;
				if (spot == null) {
					gm.SysMessage("No spot for the stone of " + city + ", walk from its spawn point failed.");
				} else {
					pieces.Item(StonePieceId(city), StoneDef, spot, area.P.M);
				}
			}
			FlaggedRegion capital = CityArea(GeneralCity);
			int[] generalSpot = SpotIn(capital, gm, 1);
			if (generalSpot != null) {
				pieces.Item("realm/gondor/stone", GeneralStoneDef, generalSpot, capital.P.M);
			}
		}

		public static string StonePieceId(string city) {
			return "town/" + city + "/stone";
		}

		/// <summary>The map area named after the city; other areas that answer to it have other names.</summary>
		public static FlaggedRegion CityArea(string city) {
			return StaticRegion.GetByDefname("a_" + city.Replace('-', '_')) as FlaggedRegion;
		}

		/// <summary>A tile `StepsFromSpawnPoint` from the area's spawn point, the `nth` of them, inside the area.</summary>
		private static int[] SpotIn(FlaggedRegion area, Character gm, int nth = 0) {
			Point4D spawnPoint = area.P;
			Map map = Map.GetMap(spawnPoint.M);
			foreach (int[] spot in WorldSpots.WalkableRing(map, spawnPoint, gm.MovementSettings, StepsFromSpawnPoint + (2 * nth))) {
				bool moved = (spot[0] != spawnPoint.X) || (spot[1] != spawnPoint.Y);
				if (moved && (RegionEventSystem.AreaOf(map.GetRegionFor(spot[0], spot[1])) == area)) {
					return spot;
				}
			}
			return null;
		}
	}
}
