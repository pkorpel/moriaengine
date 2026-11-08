using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1307 - the user's answers to #1237's saved questions, those without a quest test of their own: citizenship wants
	/// Vazeny, a day's cap of captain's orders, the pilot cities' factions served by errands from their workplaces.
	/// Tests/Protocol/AnswerTests.cs runs it.</summary>
	public static class AnswerTests {
		private static void Report(string name, bool ok) {
			Console.WriteLine("ANSWERS_CHECK: " + name + "=" + ok);
		}

		private static readonly List<int[]> used = new List<int[]>();

		//an open-air venue near the city's centre, 6 tiles from the others made, so the 5x5 squares don't overlap
		private static CityVenue OpenAir(string city, string type) {
			Point4D p = WorldEvents.CentreOf(city);
			int[] spot = null;
			foreach (int[] tile in WorldEvents.Reachable(Map.GetMap(p.M), new[] { p.X, p.Y, p.Z }, 25)) {
				if ((WorldSpots.Distance(tile, p.X, p.Y) >= 3) && used.TrueForAll(o => WorldSpots.Distance(tile, o[0], o[1]) >= 6)) {
					spot = tile;
					break;
				}
			}
			if (spot == null) {
				throw new SEException("No spot for a test venue in " + city + ".");
			}
			used.Add(spot);
			VenueDraft draft = new VenueDraft();
			draft.SetType(VenueType.Get(type).IndexOf());
			draft.M = p.M;
			draft.City = city;
			draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
			CityVenue venue = draft.Save(out string problem);
			if (venue == null) {
				throw new SEException(city + "'s " + type + " wasn't saved: " + problem);
			}
			Residents.FillRoles(venue);
			return venue;
		}

		private static string FactionAt(CityVenue venue) {
			List<Item> people = Residents.OfVenue(venue.VenueId);
			Item worker = people.Find(g => BasicErrands.IsWorker(g));
			return (worker == null) ? "none" : (CityFlavour.FactionOf(worker) ?? "null");
		}

		[SteamFunction]
		public static void TestAnswersChain(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Chain(player);
			} finally {
				RivalCaptains.clock = null;
				player.RemoveTag(RivalCaptains.OrderTK);
				player.Account.PLevel = plevel;
				Console.WriteLine("ANSWERS_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			//citizenship wants a respected name
			ReputationSettings rs = ReputationSettings.instance;
			Reputation.Set(player, "Edoras", rs.respectedFrom - 1);
			bool known = !Reputation.MayBecomeCitizen(player, "Edoras");
			Reputation.Set(player, "Edoras", rs.respectedFrom);
			Report("citizenshipWantsVazeny", (rs.citizenshipMinLevel == (int) ReputationLevel.Vazeny) && known && Reputation.MayBecomeCitizen(player, "Edoras"));

			//three captain's orders a real day, then none until the next day
			Captain captain = RivalCaptains.All[0];
			DateTime noon = DateTime.UtcNow.Date.AddHours(12);
			RivalCaptains.clock = noon;
			player.RemoveTag(RivalCaptains.OrdersDayTK);
			player.RemoveTag(RivalCaptains.OrdersCountTK);
			bool open = !RivalCaptains.DayIsFull(player);
			for (int i = 0; i < WarCampSettings.instance.captainOrdersPerDay; i++) {
				RivalCaptains.Give(player, captain, 70);
				player.RemoveTag(RivalCaptains.OrderTK);
			}
			bool full = RivalCaptains.DayIsFull(player) && (RivalCaptains.OrdersToday(player) == WarCampSettings.instance.captainOrdersPerDay);
			RivalCaptains.clock = noon.AddDays(1);
			bool nextDay = !RivalCaptains.DayIsFull(player) && (RivalCaptains.OrdersToday(player) == 0);
			Report("threeCaptainOrdersADay", (WarCampSettings.instance.captainOrdersPerDay == 3) && open && full && nextDay);

			//the pilot cities' factions by workplace
			Report("pilotFactionsByWorkplace", (FactionAt(OpenAir("Minas_Tirith", "barracks")) == "mt_citadel") && (FactionAt(OpenAir("Minas_Tirith", "healing")) == "mt_healers")
				&& (FactionAt(OpenAir("Edoras", "stable")) == "ed_breeders") && (FactionAt(OpenAir("Hobitin", "tavern")) == "ho_innkeepers")
				&& (FactionAt(OpenAir("Hobitin", "townhall")) == "ho_post"));
		}
	}
}
