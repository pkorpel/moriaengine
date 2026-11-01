using System;
using System.Globalization;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helpers for Tests/Protocol/HouseTests.cs and HouseRestartTests.cs.</summary>
	public static class HouseTests {
		private static House house;
		private static Item sign;

		private static FlaggedRegion Region {
			get {
				return (FlaggedRegion) StaticRegion.GetByDefname("a_dum_2");
			}
		}

		/// <summary>A house of a real region, its sign beside the caller. The caller stands anywhere, as a sign links.</summary>
		[SteamFunction]
		public static void TestHouseMake(Character self) {
			self.Account.PLevel = 1;
			House old = House.ForRegion(Region);
			if (old != null) {
				old.Delete();
			}
			if (sign != null) {
				sign.Delete();
			}
			house = House.Create(Region);
			sign = (Item) ((ItemDef) ThingDef.GetByDefname("i_0xbd1")).Create(self.X + 1, self.Y, self.Z, self.M);
			t_housemenu.Link(sign, house);
			Console.WriteLine("HOUSE_MADE: sign=" + (uint) sign.FlaggedUid + " rent=" + house.Rent + " me=" + (uint) self.FlaggedUid);
		}

		[SteamFunction]
		public static void TestHouseComeOver(Character self) {
			self.P(sign.X - 1, sign.Y, sign.Z, sign.M);
			Console.WriteLine("HOUSE_HERE: me=" + (uint) self.FlaggedUid);
		}

		[SteamFunction]
		public static void TestHouseReport(Character self) {
			Console.WriteLine("HOUSE_REPORT: owner=" + ((house.Owner == null) ? "none" : house.Owner.Name)
				+ " friends=" + house.Friends.Count + " coowners=" + house.CoOwners.Count + " rank=" + house.RankOf(self)
				+ " days=" + house.PaidDays + " name=" + house.HouseName + " blocked=" + house.Blocked + " signModel=" + sign.Model);
		}

		[SteamFunction]
		public static void TestHouseGm(Character self, ScriptArgs sa) {
			self.Account.PLevel = (byte) ((sa.Args == "on") ? 7 : 1);
			Console.WriteLine("HOUSE_GM: " + sa.Args);
		}

		/// <summary>AttrFlagTests' too: a client must be near what it picks up. One "x,y,z" argument.</summary>
		[SteamFunction]
		public static void TestTeleportTo(Character self, ScriptArgs sa) {
			string[] parts = sa.Argv[0].ToString().Split(',');
			int x = Convert.ToInt32(parts[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			int z = Convert.ToInt32(parts[2], CultureInfo.InvariantCulture);
			self.P(x, y, z, self.M);
			Console.WriteLine("HOUSE_LOCKDOWN_SETUP: teleported");
		}

		/// <summary>The chest beside the caller, so a click and a drop reach it.</summary>
		[SteamFunction]
		public static void TestHouseChest(Character self) {
			if (house.RentChest != null) {
				house.RentChest.Delete();
			}
			house.PlaceRentChest(self.X - 1, self.Y, self.Z, self.M);
			Item gold = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(self.X, self.Y + 1, self.Z, self.M);
			gold.Amount = 8900;
			Console.WriteLine("HOUSE_CHEST: chest=" + (uint) house.RentChest.FlaggedUid + " gold=" + (uint) gold.FlaggedUid + " amount=" + gold.Amount
				+ " x=" + gold.X + " y=" + gold.Y + " z=" + gold.Z);
		}

		[SteamFunction]
		public static void TestHousePaid(Character self) {
			Console.WriteLine("HOUSE_PAID: seconds=" + (long) (house.PaidUntil - Globals.TimeInSeconds) + " chestItems=" + house.RentChest.Count
				+ " days=" + house.PaidDays + " rent=" + house.Rent);
		}

		[SteamFunction]
		public static void TestHouseResetPaid(Character self) {
			house.PaidUntil = Globals.TimeInSeconds;
			Console.WriteLine("HOUSE_RESET: done");
		}

		/// <summary>Checks by the rules of f_payRentFrom. Value: 100000 at a rent of 89000.</summary>
		[SteamFunction]
		public static void TestHouseCheques(Character self) {
			house.Owner = self;
			house.PaidUntil = Globals.TimeInSeconds;
			ThingDef checkDef = ThingDef.GetByDefname("i_cashiers_check");
			for (int i = 0; i < 2; i++) {
				((CashiersItem) ((ItemDef) checkDef).Create(house.RentChest)).Value = (i == 0) ? 100000 : 89000;
			}
			house.CheckRent(self);
			Console.WriteLine("HOUSE_CHEQUES: seconds=" + (long) (house.PaidUntil - Globals.TimeInSeconds) + " left=" + house.RentChest.Count);
		}

		[SteamFunction]
		public static void TestHouseSetRent(Character self) {
			house.PaidUntil = Globals.TimeInSeconds + (86400 * 10);
			house.SetRent(house.Rent / 2);
			Console.WriteLine("HOUSE_SETRENT: days=" + house.PaidDays + " rent=" + house.Rent);
		}

		/// <summary>In the real region: a loose thing, a fixed one, a locked door, a pet, friends, then an unpaid check.</summary>
		[SteamFunction]
		public static void TestHouseEvict(Character self, ScriptArgs sa) {
			FlaggedRegion area = Region;
			Point4D p = area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			house.Owner = self;
			house.AddMember(self, HouseRank.Friend);
			house.PaidUntil = Globals.TimeInSeconds - 10;
			Item loose = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(p.X, p.Y, p.Z, p.M);
			Item fixedThing = (Item) ((ItemDef) ThingDef.GetByDefname("i_flip_probe")).Create(p.X, p.Y, p.Z, p.M);
			fixedThing.SetAttribute(ItemAttributes.attr_move_never, true);
			Item door = (Item) ((ItemDef) ThingDef.GetByDefname("i_flip_door_probe")).Create(p.X, p.Y, p.Z, p.M);
			door.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door_locked");
			door.LockUid = 5;
			NPC pet = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(p.X, p.Y, p.Z, p.M);
			pet.ControlMaster = self;
			if (sa.Args == "timer") {
				house.GetTimer(HouseRentTimer.Key).DueInSeconds = 1;
			} else {
				house.CheckRent(self);
				ReportEviction(self, door);
			}
			lastDoor = door;
		}

		private static Item lastDoor;

		private static void ReportEviction(Character self, Item door) {
			string bags = "";
			foreach (Item bag in house.RentChest) {
				bags += "[" + bag.Name.Substring(0, 5) + ":" + ((Container) bag).Count + "]";
			}
			Console.WriteLine("HOUSE_EVICTED: owner=" + ((house.Owner == null) ? "none" : house.Owner.Name) + " friends=" + house.Friends.Count
				+ " bags=" + bags + " doorType=" + door.Type.Defname + " doorLock=" + door.LockUid + " doorThere=" + door.IsOnGround);
		}

		[SteamFunction]
		public static void TestHouseEvictedReport(Character self) {
			ReportEviction(self, lastDoor);
		}

		[SteamFunction]
		public static void TestHouseForSave(Character self) {
			House old = House.ForRegion(Region);
			if (old != null) {
				old.Delete();
			}
			house = House.Create(Region);
			house.Owner = self;
			house.HouseName = "Hnizdo";
			house.PaidUntil = Globals.TimeInSeconds + (86400 * 12);
			house.Rent = 123000;
			Console.WriteLine("HOUSE_SAVED: ready");
		}

		[SteamFunction]
		public static void TestHouseAfterRestart(Character self) {
			House found = House.ForRegion(Region);
			Console.WriteLine("HOUSE_LOADED: found=" + (found != null) + " owner=" + ((found == null) || (found.Owner == null) ? "none" : found.Owner.Name)
				+ " name=" + ((found == null) ? "" : found.HouseName) + " rent=" + ((found == null) ? 0 : found.Rent)
				+ " days=" + ((found == null) ? 0 : found.PaidDays));
		}
	}
}
