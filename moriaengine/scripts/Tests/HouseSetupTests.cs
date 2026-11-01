using System;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helpers for Tests/Protocol/HouseSetupTests.cs. a_dum_4 has its spawn point outside its region
	/// and no z of its own, as most have.</summary>
	public static class HouseSetupTests {
		private static House house;
		private static Item sign, chest;

		private static FlaggedRegion Area {
			get {
				return (FlaggedRegion) StaticRegion.GetByDefname("a_dum_4");
			}
		}

		private static Realm EnemyRealm {
			get {
				return (Area.HouseRealm == Realm.Gondor) ? Realm.Mordor : Realm.Gondor;
			}
		}

		[SteamFunction]
		public static void TestHousesAudit(Character self) {
			int regions = 0, wrongArea = 0, signs = 0, chestsIn = 0, chestsOut = 0, noChest = 0, signsInAir = 0;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area == null) || !area.IsHouse) {
					continue;
				}
				regions++;
				House found = House.ForRegion(area);
				if ((found == null) || (found.Area != area)) {
					wrongArea++;
				} else if (found.RentChest == null) {
					noChest++;
				} else if (found.GetMap().GetRegionFor(found.RentChest) == area) {
					chestsIn++;
				} else {
					chestsOut++;
				}
			}
			foreach (Thing thing in Thing.AllThings) {
				Item item = thing as Item;
				if ((item != null) && (item.Type is t_housemenu)) {
					signs++;
					if (!item.GetMap().CanFit(item.X, item.Y, item.Z, 16, true, false, true)) {
						signsInAir++;
					}
				}
			}
			Console.WriteLine("HOUSES_AUDIT: regions=" + regions + " wrongArea=" + wrongArea + " signs=" + signs + " chestsIn=" + chestsIn
				+ " chestsOut=" + chestsOut + " noChest=" + noChest + " signsInAir=" + signsInAir);
		}

		[SteamFunction]
		public static void TestHouseOutdoors(Character self) {
			house = House.ForRegion(Area);
			chest = house.RentChest;
			foreach (Thing thing in Thing.AllThings) {
				Item item = thing as Item;
				if ((item != null) && t_housemenu.IsSignOf(item, house)) {
					sign = item;
				}
			}
			self.P(sign.X, sign.Y, sign.Z, sign.M);
			Point2D inside = TileInside();
			Console.WriteLine("HOUSE_OUTDOORS: sign=" + (uint) sign.FlaggedUid + " chest=" + (uint) chest.FlaggedUid + " x=" + sign.X + " y=" + sign.Y + " z=" + sign.Z
				+ " inX=" + inside.X + " inY=" + inside.Y + " signOutside=" + (sign.GetMap().GetRegionFor(sign) != Area));
		}

		private static Point2D TileInside() {
			Map map = Map.GetMap(Area.Mapplane);
			foreach (ImmutableRectangle rectangle in Area.Rectangles) {
				for (int x = rectangle.MinX; x <= rectangle.MaxX; x++) {
					for (int y = rectangle.MinY; y <= rectangle.MaxY; y++) {
						if (map.GetRegionFor(x, y) == Area) {
							return new Point2D(x, y);
						}
					}
				}
			}
			throw new InvalidOperationException("Nothing inside " + Area.Defname);
		}

		[SteamFunction]
		public static void TestHousePlayer(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			self.Realm = sa.Args.Contains("enemy") ? EnemyRealm : Area.HouseRealm;
			Console.WriteLine("HOUSE_PLAYER: " + self.Realm);
		}

		/// <summary>A real step over the region's edge, or "no way in" if the map has none.</summary>
		[SteamFunction]
		public static void TestHouseWalkIn(Character self) {
			Map map = Map.GetMap(Area.Mapplane);
			foreach (ImmutableRectangle rectangle in Area.Rectangles) {
				for (int x = rectangle.MinX - 1; x <= rectangle.MaxX + 1; x++) {
					for (int y = rectangle.MinY - 1; y <= rectangle.MaxY + 1; y++) {
						if (map.GetRegionFor(x, y) == Area) {
							continue;
						}
						Point3D from = new Point3D(x, y, WorldSpots.LowestStandZ(map, x, y));
						for (int dir = 0; dir < 8; dir++) {
							int nx, ny, nz;
							if (map.CheckMovement(from, self.MovementSettings, (Direction) dir, false, out nx, out ny, out nz) && (map.GetRegionFor(nx, ny) == Area)) {
								self.P(from.X, from.Y, from.Z, Area.Mapplane);
								self.Direction = (Direction) dir; //a first step in a new direction only turns
								bool moved = self.WalkRunOrFly((Direction) dir, false, false);
								Console.WriteLine("HOUSE_WALK: moved=" + moved + " inside=" + (map.GetRegionFor(self.X, self.Y) == Area));
								return;
							}
						}
					}
				}
			}
			Console.WriteLine("HOUSE_WALK: no way in");
		}

		[SteamFunction]
		public static void TestHouseTeleport(Character self) {
			Point2D inside = TileInside();
			Map map = Map.GetMap(Area.Mapplane);
			SpellDef teleport = SpellDef.GetByDefname("s_teleport");
			self.P(sign.X, sign.Y, sign.Z, sign.M);
			teleport.Trigger_EffectGround(new Point4D(inside.X, inside.Y, WorldSpots.LowestStandZ(map, inside.X, inside.Y), Area.Mapplane),
				SpellEffectArgs.Acquire(self, self, null, teleport, 0, EffectFlag.FromSpellBook));
			Console.WriteLine("HOUSE_TELE: landed=" + (map.GetRegionFor(self.X, self.Y) == Area));
		}

		[SteamFunction]
		public static void TestHouseDie(Character self) {
			self.CauseDeath(null);
			Console.WriteLine("HOUSE_DEAD: " + self.Flag_Dead);
		}

		[SteamFunction]
		public static void TestHouseSetupReport(Character self) {
			int doors = 0, doorsOutside = 0;
			foreach (Thing thing in Thing.AllThings) {
				Item item = thing as Item;
				if ((item != null) && item.IsDoor && item.IsOnGround && (item.M == sign.M) && (Point2D.GetSimpleDistance(item.X, item.Y, sign.X, sign.Y) < 20) && item.Flag_NonMovable) {
					if (item.GetMap().GetRegionFor(item) == Area) {
						doors++;
					} else {
						doorsOutside++;
					}
				}
			}
			Console.WriteLine("SETUP_REPORT: owner=" + ((house.Owner == null) ? "none" : house.Owner.Name) + " signAt=" + sign.X + "," + sign.Y
				+ " doors=" + doors + " doorsOutside=" + doorsOutside + " signLevel=" + t_housemenu.LevelOf(sign));
		}
	}
}
