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
