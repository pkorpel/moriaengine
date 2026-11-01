using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - a sign and a rent chest for every dwelling. Erebor's were placed live and aren't in the archive. A region's
	/// spawn point mostly lies on its doorstep, a tile or two outside, so the sign goes there; the chest is a post box that
	/// blocks the way, so it goes a few steps inside if there's a way in. The spots are proposals: .HouseMove moves them, a
	/// moved one stays, and doors are for GMs to place with .HouseDoor. A rent chest deleted comes back by itself.</summary>
	public sealed class HouseSetup : WorldSetup {
		public const string SignDef = "i_0xbd1";
		private const int MaxStepsIn = 3, NearestSpot = 3, HousesAStep = 20;

		public override string Name {
			get {
				return "Houses";
			}
		}

		[SteamFunction]
		public static void SetupHouses(Character self) {
			WorldSetupRun.Start(self, "Houses", new HouseSetup());
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (string step in this.PlaceInSteps(pieces, gm)) {
			}
		}

		protected internal override IEnumerable<string> PlaceInSteps(WorldPieces pieces, Character gm) {
			List<FlaggedRegion> houses = new List<FlaggedRegion>();
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && area.IsHouse) {
					houses.Add(area);
				}
			}
			int done = 0;
			foreach (FlaggedRegion area in houses) {
				PlaceHouse(pieces, gm, area);
				done++;
				if ((done % HousesAStep) == 0) {
					yield return done + "/" + houses.Count;
				}
			}
		}

		private static void PlaceHouse(WorldPieces pieces, Character gm, FlaggedRegion area) {
			House house = House.Create(area);
			int[] doorstep = Doorstep(area);
			t_housemenu.Link(pieces.Item("house/" + area.Defname + "/sign", SignDef, doorstep, area.Mapplane), house);
			int[] chest = ChestSpot(area, doorstep, gm);
			house.EnsureRentChest(new Point4D(chest[0], chest[1], chest[2], area.Mapplane), null);
		}

		//152 spawn points have no z, and some have one that's no floor
		private static int[] Doorstep(FlaggedRegion area) {
			Point4D p = area.P;
			Map map = Map.GetMap(area.Mapplane);
			if ((p.Z != 0) && map.CanFit(p.X, p.Y, p.Z, 16, true, false, true)) {
				return new[] { (int) p.X, (int) p.Y, (int) p.Z };
			}
			int z = WorldSpots.LowestStandZ(map, p.X, p.Y);
			if (map.CanFit(p.X, p.Y, z, 16, true, false, true)) {
				return new[] { (int) p.X, (int) p.Y, z };
			}
			return WorldSpots.NearestStandable(map, p.X, p.Y, NearestSpot) ?? new[] { (int) p.X, (int) p.Y, z }; //in a wall or on a dock's edge
		}

		private static int[] ChestSpot(FlaggedRegion area, int[] doorstep, Character gm) {
			Map map = Map.GetMap(area.Mapplane);
			Point3D from = new Point3D(doorstep[0], doorstep[1], doorstep[2]);
			for (int steps = MaxStepsIn; steps > 0; steps--) {
				foreach (int[] spot in WorldSpots.WalkableRing(map, from, gm.MovementSettings, steps)) {
					if (map.GetRegionFor(spot[0], spot[1]) == area) {
						return spot;
					}
				}
			}
			return WorldSpots.WalkableRing(map, from, gm.MovementSettings, 1)[0]; //no way in from the outside: beside the sign
		}
	}
}
