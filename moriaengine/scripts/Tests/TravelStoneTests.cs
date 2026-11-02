using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1035 - the stops, prices and sides are checked here; TravelStoneTests on the client side
	/// walks a real double click, dialog and ride, with these commands setting the scene.</summary>
	public static class TravelStoneTests {
		private static Item stone;

		[SteamFunction]
		public static void TestTravelStops(Character self) {
			bool allStand = true, allHaveStone = true;
			int gondor = 0, mordor = 0;
			for (int i = 0; i < TravelStops.All.Length; i++) {
				TravelStop stop = TravelStops.All[i];
				int[] arrival = stop.Arrival;
				int[] spot = TravelStonesSetup.StoneSpot(stop, self);
				allStand &= arrival != null;
				allHaveStone &= spot != null;
				gondor += (stop.RealmTag == 1) ? 1 : 0;
				mordor += (stop.RealmTag == 2) ? 1 : 0;
				Point4D wanted = stop.Destination;
				FlaggedRegion area = RegionEventSystem.AreaOf(Map.GetMap(wanted.M).GetRegionFor(wanted));
				Console.WriteLine("TRAVEL_STOP: " + (i + 1) + " " + stop.Name + " wanted=" + wanted + " arrival=" + Spot(arrival) + " stone=" + Spot(spot) +
					" off=" + ((arrival == null) ? -1 : WorldSpots.Distance(arrival, wanted.X, wanted.Y)) + " realm=" + stop.RealmTag + " area=" + ((area == null) ? "none" : area.Defname));
			}
			Report("everyStopCanBeStoodOn", allStand);
			Report("everyStopHasAStoneSpot", allHaveStone);
			Report("bothSidesHaveStops", (gondor > 0) && (mordor > 0));
			Console.WriteLine("TRAVEL_STOPS_DONE");
		}

		[SteamFunction]
		public static void TestTravelSetup(Character self) {
			new TravelStonesSetup().Run(self);
		}

		[SteamFunction]
		public static void TestTravelSetupCount(Character self) {
			int stones = 0;
			foreach (Thing thing in Thing.AllThings) {
				if (thing is TravelStone) {
					stones++;
				}
			}
			Console.WriteLine("TRAVEL_SETUP_STONES=" + stones);
		}

		[SteamFunction]
		public static void TestTravelSides(Character self) {
			self.Account.PLevel = 1;
			TravelStop edoras = Named("Edoras"), baradDur = Named("Barad-Dur"), fornost = Named("Fornost");
			Report("edorasIsGondor", edoras.RealmTag == 1);
			Report("baradDurIsMordor", baradDur.RealmTag == 2);

			self.Realm = Realm.Gondor;
			List<TravelStop> forGondor = TravelStops.Offered(self);
			Report("gondorIsNotOfferedMordor", forGondor.Contains(edoras) && !forGondor.Contains(baradDur));

			int threshold = NotorietySettings.instance.murdererThreshold;
			NotorietySettings.instance.murdererThreshold = 0; //anyone counts
			Report("murdererCountsAsMurderer", Notoriety.IsMurderer(self));
			List<TravelStop> forMurderer = TravelStops.Offered(self);
			Report("gondorMurdererKeepsGondorsStops", forMurderer.Contains(edoras) && !forMurderer.Contains(baradDur));
			NotorietySettings.instance.murdererThreshold = threshold;

			self.Realm = Realm.Mordor;
			List<TravelStop> forMordor = TravelStops.Offered(self);
			Report("mordorIsNotOfferedGondor", !forMordor.Contains(edoras) && forMordor.Contains(baradDur));

			self.Realm = Realm.None;
			Report("noRealmIsNotOfferedMordor", !TravelStops.Offered(self).Contains(baradDur));

			Point4D from = edoras.Destination;
			self.Realm = Realm.Gondor;
			int expected = (Point2D.GetSimpleDistance(from, fornost.Destination) * 1000) / 3500;
			Console.WriteLine("TRAVEL_INFO: Edoras to Fornost " + expected + " gp, asked " + TravelStops.Price(self, from, fornost));
			Report("priceIsDistanceBy3Point5", (expected > 0) && (TravelStops.Price(self, from, fornost) == expected));

			Point4D enemyStone = baradDur.Destination;
			int plain = (Point2D.GetSimpleDistance(enemyStone, fornost.Destination) * 1000) / 3500;
			Report("leavingEnemyLandCostsFiveTimes", TravelStops.Price(self, enemyStone, fornost) == plain * 5);
			self.Realm = Realm.Mordor;
			Report("mordorPaysPlainAtHome", TravelStops.Price(self, enemyStone, fornost) == plain);
			self.Realm = Realm.Gondor;
			self.Account.PLevel = 4;
			Report("gmIsOfferedEverything", TravelStops.Offered(self).Count == TravelStops.All.Length);
			Console.WriteLine("TRAVEL_SIDES_DONE");
		}

		/// <summary>A stone beside the player, standing at Edoras, as a Gondor player with gold.</summary>
		[SteamFunction]
		public static void TestTravelPrepare(Character self) {
			int[] arrival = Named("Edoras").Arrival;
			self.Go(new Point4D(arrival[0], arrival[1], arrival[2], 0));
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
			TravelSettings.instance.rideSeconds = 2;
			if ((stone == null) || stone.IsDeleted) {
				stone = (Item) ((ItemDef) ThingDef.GetByDefname("i_stone_travel")).Create(self.X + 1, self.Y, self.Z, self.M);
			}
			((Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(self.Backpack)).Amount = 20000;
			Console.WriteLine("TRAVEL_READY: stone=" + ((uint) stone.Uid | 0x40000000) + " gold=" + self.Gold + " price=" + TravelStops.Price(self, stone, Named("Fornost")));
		}

		[SteamFunction]
		public static void TestTravelBookRide(Character self) {
			TestTravelPrepare(self);
			TravelSettings.instance.rideSeconds = 600;
			((TravelStone) stone).Book((Player) self, Named("Fornost"));
			Console.WriteLine("TRAVEL_BOOKED: riding=" + self.HasTimer(TravelRide.Key));
		}

		[SteamFunction]
		public static void TestTravelCountRides(Character self) {
			int rides = 0;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is Character) && thing.HasTimer(TravelRide.Key)) {
					rides++;
				}
			}
			Console.WriteLine("TRAVEL_RIDES=" + rides);
		}

		[SteamFunction]
		public static void TestTravelStep(Character self) {
			self.Go(new Point4D(self.X + 1, self.Y, self.Z, self.M));
		}

		[SteamFunction]
		public static void TestTravelStray(Character self) {
			self.Go(new Point4D(self.X + 8, self.Y, self.Z, self.M));
		}

		[SteamFunction]
		public static void TestTravelBackToStone(Character self) {
			int[] arrival = Named("Edoras").Arrival;
			self.Go(new Point4D(arrival[0], arrival[1], arrival[2], 0));
		}

		[SteamFunction]
		public static void TestTravelBecomeOutcast(Character self) {
			self.Realm = Realm.GondorOutcast;
			Console.WriteLine("TRAVEL_OUTCAST");
		}

		[SteamFunction]
		public static void TestTravelReport(Character self) {
			int[] fornost = Named("Fornost").Arrival;
			Console.WriteLine("TRAVEL_REPORT: at=" + self.X + "," + self.Y + " gold=" + self.Gold + " riding=" + self.HasTimer(TravelRide.Key) +
				" atFornost=" + ((self.X == fornost[0]) && (self.Y == fornost[1])) + " seq=" + (++reports));
		}

		private static int reports;

		private static TravelStop Named(string name) {
			foreach (TravelStop stop in TravelStops.All) {
				if (stop.Name == name) {
					return stop;
				}
			}
			throw new SEException("No stop " + name);
		}

		private static string Spot(int[] spot) {
			return (spot == null) ? "null" : string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", spot[0], spot[1], spot[2]);
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("TRAVEL_CHECK: " + name + "=" + ok);
		}
	}
}
