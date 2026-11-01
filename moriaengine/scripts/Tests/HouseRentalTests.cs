using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helpers for Tests/Protocol/HouseRentalTests.cs. a_dum_4 stands in Dol Amroth.</summary>
	public static class HouseRentalTests {
		private const string City = "Dol_Amroth";
		private static House house;
		private static Character owner;

		private static FlaggedRegion Area {
			get {
				return (FlaggedRegion) StaticRegion.GetByDefname("a_dum_4");
			}
		}

		private static Town Town {
			get {
				foreach (Thing thing in Thing.AllThings) {
					TownStone stone = thing as TownStone;
					if ((stone != null) && (CityOfStone(stone) == City)) {
						return CompiledScripts.Town.ForStone(stone, City);
					}
				}
				throw new InvalidOperationException("No stone of " + City);
			}
		}

		private static string CityOfStone(TownStone stone) {
			FlaggedRegion area = stone.GetMap().GetRegionFor(stone) as FlaggedRegion;
			while ((area != null) && string.IsNullOrEmpty(area.City)) {
				area = area.Parent as FlaggedRegion;
			}
			return (area == null) ? null : area.City;
		}

		[SteamFunction]
		public static void TestRentalOwner(Character self) {
			owner = self;
			self.Realm = Realm.Gondor;
			self.Account.PLevel = 1;
			Point4D p = Area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			house = House.Create(Area);
			house.Owner = self;
			house.PaidUntil = Globals.TimeInSeconds + (86400 * 30);
			Console.WriteLine("RENTAL_OWNER: city=" + HouseRentals.CityOf(Area) + " here=" + HouseRentals.CityAt(self) + " housesInCity=" + HouseRentals.InCity(City).Count);
		}

		[SteamFunction]
		public static void TestRentalViewer(Character self, ScriptArgs sa) {
			Town town = Town;
			Point4D p = Area.P;
			self.P(p.X, p.Y, p.Z, p.M);
			self.Realm = Realm.Gondor;
			self.Account.PLevel = 1;
			town.mayor = sa.Args.Contains("mayor") ? self : null;
			house.AddMember(self, HouseRank.Friend);
			Console.WriteLine("RENTAL_VIEWER: mayor=" + (town.mayor == self));
		}

		[SteamFunction]
		public static void TestRentalGm(Character self) {
			self.Account.PLevel = 7;
			Console.WriteLine("RENTAL_GM: " + self.IsGM);
		}

		/// <summary>A realm's office exists from its stone's first use.</summary>
		[SteamFunction]
		public static void TestRentalOffice(Character self) {
			foreach (Thing thing in Thing.AllThings) {
				RealmStone stone = thing as RealmStone;
				if ((stone != null) && (RealmOffice.For(Realm.Gondor) == null)) {
					RealmOffice.ForStone(stone, Realm.Gondor);
				}
			}
			Console.WriteLine("RENTAL_OFFICE: " + (RealmOffice.For(Realm.Gondor) != null));
		}

		[SteamFunction]
		public static void TestRentalShow(Character self) {
			D_HouseRentals.Show((Player) self, City, 0);
			Console.WriteLine("RENTAL_SHOWN:");
		}

		[SteamFunction]
		public static void TestRentalReport(Character self) {
			Town town = Town;
			RealmOffice office = RealmOffice.For(Realm.Gondor);
			Console.WriteLine("RENTAL_REPORT: townBan=" + (town.FindBan(owner) != null) + " realmBan=" + ((office != null) && (office.FindBan(owner) != null))
				+ " citizen=" + (town.FindCitizen(owner) != null) + " office=" + (office != null));
		}
	}
}
