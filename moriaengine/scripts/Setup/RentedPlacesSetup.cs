using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - a sign and a rent chest for each of the 61 stalls and 38 fields. Erebor's were placed live; a stall's or
	/// field's spawn point is mostly inside it, so both go there or beside it. .HouseMove moves them.</summary>
	public sealed class RentedPlacesSetup : WorldSetup {
		private const int PlacesAStep = 20, NearestSpot = 3;

		public override string Name {
			get {
				return "Stalls and fields";
			}
		}

		[SteamFunction]
		public static void SetupStalls(Character self) {
			WorldSetupRun.Start(self, "Stalls and fields", new RentedPlacesSetup());
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (string step in this.PlaceInSteps(pieces, gm)) {
			}
		}

		internal static bool IsRentedPlace(FlaggedRegion area) {
			return (area.HouseNumber != 0) && (House.GemDefFor(area) != "i_house_gem");
		}

		protected internal override IEnumerable<string> PlaceInSteps(WorldPieces pieces, Character gm) {
			List<FlaggedRegion> places = new List<FlaggedRegion>();
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				if ((region is FlaggedRegion area) && IsRentedPlace(area)) {
					places.Add(area);
				}
			}
			int done = 0;
			foreach (FlaggedRegion area in places) {
				PlacePlace(pieces, gm, area);
				done++;
				if ((done % PlacesAStep) == 0) {
					yield return done + "/" + places.Count;
				}
			}
		}

		private static void PlacePlace(WorldPieces pieces, Character gm, FlaggedRegion area) {
			House place = House.Create(area);
			bool stall = place is Stall;
			Map map = Map.GetMap(area.Mapplane);
			Point4D p = area.P;
			int z = ((p.Z != 0) && map.CanFit(p.X, p.Y, p.Z, 16, true, false, true)) ? p.Z : WorldSpots.LowestStandZ(map, p.X, p.Y);
			int[] spot = new[] { (int) p.X, (int) p.Y, z };
			t_housemenu.Link(pieces.Item((stall ? "stall/" : "field/") + area.Defname + "/sign", stall ? "i_vendorsign" : "i_fieldsign", spot, area.Mapplane), place);
			int[] chest = WorldSpots.NearestStandable(map, p.X + 1, p.Y, NearestSpot) ?? spot;
			place.EnsureRentChest(new Point4D(chest[0], chest[1], chest[2], area.Mapplane), null);
		}
	}
}
