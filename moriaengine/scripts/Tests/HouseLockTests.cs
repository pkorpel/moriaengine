using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helpers for Tests/Protocol/HouseLockTests.cs.</summary>
	public static class HouseLockTests {
		private static House house;
		private static Item sign, bag, coin, door, ingot;

		private static Item Make(string defname, int dx, int dy) {
			Point4D p = house.Area.P;
			return (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(p.X + dx, p.Y + dy, p.Z, p.M);
		}

		[SteamFunction]
		public static void TestLockSetup(Character self) {
			self.Account.PLevel = 1;
			FlaggedRegion area = (FlaggedRegion) StaticRegion.GetByDefname("a_dum_2");
			House old = House.ForRegion(area);
			if (old != null) {
				old.Delete();
			}
			house = House.Create(area);
			house.Owner = self;
			house.PaidUntil = Globals.TimeInSeconds + (86400 * 30); //unpaid, the sign's first click evicts
			Point4D p = area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			sign = Make("i_0xbd1", 0, 0);
			t_housemenu.Link(sign, house);
			house.PlaceRentChest(p.X, p.Y, p.Z, p.M);
			bag = Make("i_bag", 0, 0);
			coin = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(bag);
			door = Make("i_flip_door_probe", 0, 0);
			ingot = Make("i_ingot_iron", 0, 0);
			Console.WriteLine("LOCK_SETUP: sign=" + (uint) sign.FlaggedUid + " bag=" + (uint) bag.FlaggedUid + " coin=" + (uint) coin.FlaggedUid
				+ " door=" + (uint) door.FlaggedUid + " ingot=" + (uint) ingot.FlaggedUid + " inRegion=" + (bag.GetMap().GetRegionFor(bag) == area));
		}

		[SteamFunction]
		public static void TestLockVisit(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			Point4D p = house.Area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			house.RemoveMember(self);
			if (sa.Args.Contains("friend")) { //a plain account's Args keep their quotes
				house.AddMember(self, HouseRank.Friend);
			}
			Console.WriteLine("LOCK_VISIT: " + sa.Args);
		}

		[SteamFunction]
		public static void TestLockReport(Character self) {
			Console.WriteLine("LOCK_REPORT: bag=" + bag.LockLevel + "/" + (bag.LockedTo != null) + "/" + bag.Flag_NonMovable
				+ " person=" + ((bag.LockPerson == null) ? "none" : bag.LockPerson.Name) + " list=" + house.LockedItems.Count
				+ " ingot=" + (ingot.LockedTo != null) + " door=" + door.Type.Defname + "/" + door.IsDoorOpen
				+ " coinIn=" + (coin.Cont == bag) + " bagGround=" + bag.IsOnGround + " color=" + bag.Color);
		}

		[SteamFunction]
		public static void TestLockCheck(Character self) {
			Console.WriteLine("LOCK_CHECK: " + self.Name + " locked=" + bag.IsLockedAgainst(self));
		}

		[SteamFunction]
		public static void TestLockBlock(Character self, ScriptArgs sa) {
			house.Blocked = (sa.Args == "on");
			Console.WriteLine("LOCK_BLOCK: color=" + bag.Color);
		}

		[SteamFunction]
		public static void TestLockCloseDoor(Character self) {
			if (door.IsDoorOpen) {
				door.UseDoor();
			}
			Console.WriteLine("LOCK_DOOR: closed");
		}

		[SteamFunction]
		public static void TestLockEvict(Character self) {
			house.Empty();
			Console.WriteLine("LOCK_EVICTED: list=" + house.LockedItems.Count + " door=" + door.Type.Defname + " bagInChest=" + !bag.IsOnGround
				+ " attr=" + bag.Flag_NonMovable + " chest=" + house.RentChest.Count);
		}

		[SteamFunction]
		public static void TestLockDestroy(Character self) {
			Item thing = Make("i_bag", 0, 0);
			house.Lock(thing, HouseRank.Owner, null);
			house.Delete();
			Console.WriteLine("LOCK_DESTROYED: fixed=" + thing.Flag_NonMovable);
		}
	}
}
