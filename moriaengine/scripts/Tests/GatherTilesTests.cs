using System;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#958 - server-side helpers for Tests/Protocol/GatherTilesTests.cs, on real map spots.</summary>
	public static class GatherTilesTests {
		private static House house;
		private static RentChest chest;
		private static Item gold, junk;

		private static FlaggedRegion Area {
			get {
				return (FlaggedRegion) StaticRegion.GetByDefname("a_dum_2");
			}
		}

		/// <summary>The real checks on what's in reach of a real spot, by model, and what the spot yields.</summary>
		[SteamFunction]
		public static void TestGatherSpot(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim('"').Split(' ');
			bool mine = args[2] == "mine";
			byte m = 0;
			Map map = Map.GetMap(m);
			int[] at = WorldSpots.NearestStandable(map, int.Parse(args[0]), int.Parse(args[1]), 12);
			self.P(at[0], at[1], at[2], m);
			SortedDictionary<string, string> seen = new SortedDictionary<string, string>();
			int yielded = 0, tried = 0;
			for (int x = self.X - 3; x <= self.X + 3; x++) {
				for (int y = self.Y - 3; y <= self.Y + 3; y++) {
					foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
						if (Point2D.GetSimpleDistance(self, s) <= 3) {
							bool valid = mine ? MiningSkillDef.IsValidVein(self, s) : LumberjackingSkillDef.IsValidTree(self, s);
							seen["0x" + ((int) s.Id).ToString("x")] = valid.ToString();
							if (valid) {
								tried++;
								if ((mine ? MiningVeinSystem.Mine(self, m, x, y) : LumberjackingVeinSystem.Chop(self, m, x, y)) != null) {
									yielded++;
								}
							}
						}
					}
				}
			}
			string found = "";
			foreach (KeyValuePair<string, string> entry in seen) {
				found += " " + entry.Key + "=" + entry.Value;
			}
			Console.WriteLine("GATHER_SPOT: tried=" + tried + " yielded=" + yielded + " region=" + RegionResourceSystem.FindRegionType(m, self.X, self.Y, mine ? "t_rock" : "t_tree") + found);
		}

		/// <summary>Real trunks over a stretch of forest: what comes of chopping each once, with no skill.</summary>
		[SteamFunction]
		public static void TestChopForest(Character self) {
			Map map = Map.GetMap(0);
			int trees = 0, wood = 0;
			for (int x = 2950; x <= 3100; x++) {
				for (int y = 1100; y <= 1300; y++) {
					foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
						if (((int) s.Id == 0xcd0) && (trees < 200)) {
							trees++;
							if (LumberjackingVeinSystem.Chop(self, 0, x, y) != null) {
								wood++;
							}
						}
					}
				}
			}
			Console.WriteLine("CHOP_FOREST: trees=" + trees + " wood=" + wood);
		}

		[SteamFunction]
		public static void TestChestSetup(Character self) {
			House old = House.ForRegion(Area);
			if (old != null) {
				old.Delete();
			}
			house = House.Create(Area);
			house.Owner = self;
			house.PaidUntil = Globals.TimeInSeconds + 86400;
			Point4D p = Area.P;
			house.PlaceRentChest(p.X, p.Y, p.Z, p.M);
			chest = house.RentChest;
			Console.WriteLine("CHEST_SETUP: chest=" + (uint) chest.FlaggedUid);
		}

		/// <summary>The caller, an ordinary player, comes with money and a bag.</summary>
		[SteamFunction]
		public static void TestChestVisitor(Character self) {
			Point4D p = Area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			self.Realm = Area.HouseRealm;
			gold = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(p.X, p.Y, p.Z, p.M);
			gold.Amount = 50000;
			junk = (Item) ((ItemDef) ThingDef.GetByDefname("i_bag")).Create(p.X, p.Y, p.Z, p.M);
			Console.WriteLine("CHEST_VISITOR: gold=" + (uint) gold.FlaggedUid + " junk=" + (uint) junk.FlaggedUid + " gm=" + self.IsGM);
		}

		[SteamFunction]
		public static void TestChestReport(Character self) {
			Console.WriteLine("CHEST_REPORT: items=" + chest.Count + " goldIn=" + (!gold.IsDeleted && (gold.Cont == chest)) + " junkIn=" + (junk.Cont == chest)
				+ " paidGone=" + gold.IsDeleted + " days=" + house.PaidDays);
		}
	}
}
