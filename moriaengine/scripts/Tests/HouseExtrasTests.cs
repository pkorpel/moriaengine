using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helpers for Tests/Protocol/HouseExtrasTests.cs.</summary>
	public static class HouseExtrasTests {
		private const int OpenGroundX = 4763, OpenGroundY = 1363, OpenGroundZ = 10; //neighbouring tiles are often another house
		private static House house;
		private static Character owner;

		private static FlaggedRegion Area {
			get {
				return (FlaggedRegion) StaticRegion.GetByDefname("a_dum_2");
			}
		}

		[SteamFunction]
		public static void TestExtrasSetup(Character self) {
			self.Account.PLevel = 1;
			self.Realm = Area.HouseRealm;
			owner = self;
			House old = House.ForRegion(Area);
			if (old != null) {
				old.Delete();
			}
			house = House.Create(Area);
			house.Owner = self;
			house.PaidUntil = Globals.TimeInSeconds + (86400 * 30);
			Point4D p = Area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			Item sign = (Item) ((ItemDef) ThingDef.GetByDefname("i_0xbd1")).Create(p.X, p.Y, p.Z, p.M);
			t_housemenu.Link(sign, house);
			house.PlaceRentChest(p.X, p.Y, p.Z, p.M);
			GiveGold(self);
			Console.WriteLine("EXTRAS_SETUP: sign=" + (uint) sign.FlaggedUid + " inside=" + (self.Region == Area));
		}

		private static void GiveGold(Character self) {
			Item gold = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(self.Backpack);
			gold.Amount = 60000;
		}

		[SteamFunction]
		public static void TestExtrasMoreGold(Character self) {
			GiveGold(self);
			Console.WriteLine("EXTRAS_GOLD: " + GoldUtils.CountGoldOn(self));
		}

		[SteamFunction]
		public static void TestExtrasStand(Character self, ScriptArgs sa) {
			Point4D p = Area.P;
			if (sa.Args.Contains("outside")) {
				self.P(OpenGroundX, OpenGroundY, OpenGroundZ, 0);
			} else {
				self.P(p.X, p.Y, p.Z, p.M);
			}
			self.Realm = sa.Args.Contains("enemy") ? ((Area.HouseRealm == Realm.Gondor) ? Realm.Mordor : Realm.Gondor) : Area.HouseRealm;
			Console.WriteLine("EXTRAS_STAND: inside=" + (self.Region == Area));
		}

		[SteamFunction]
		public static void TestExtrasReport(Character self) {
			Console.WriteLine("EXTRAS_REPORT: bank=" + house.BankActivated + " gold=" + GoldUtils.CountGoldOn(owner));
		}

		/// <summary>The caller kills the owner, who then stands up again.</summary>
		[SteamFunction]
		public static void TestExtrasKill(Character self, ScriptArgs sa) {
			Point4D p = Area.P;
			if (sa.Args.Contains("outside")) {
				owner.P(OpenGroundX, OpenGroundY, OpenGroundZ, 0);
			} else {
				owner.P(p.X, p.Y, p.Z, p.M);
			}
			owner.CauseDeath(self);
			Console.WriteLine("EXTRAS_KILLED: dead=" + owner.Flag_Dead);
			owner.Resurrect();
		}

		[SteamFunction]
		public static void TestExtrasLogoutDelay(Character self) {
			HouseSettings.instance.logoutDelaySeconds = 2;
			Console.WriteLine("EXTRAS_DELAY: " + HouseEvents.LogoutDelayFor(owner));
		}

		[SteamFunction]
		public static void TestExtrasLinger(Character self) {
			Console.WriteLine("EXTRAS_LINGER: disconnected=" + owner.Flag_Disconnected + " lingering=" + owner.IsLingering);
		}

		[SteamFunction]
		public static void TestExtrasEvict(Character self) {
			house.Empty();
			Console.WriteLine("EXTRAS_EVICTED: bank=" + house.BankActivated);
		}
	}
}
