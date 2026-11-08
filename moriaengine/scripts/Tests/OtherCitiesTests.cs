using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1304 - the living world in the other cities, on the server: names by folk, greetings, news, factions, events and
	/// festivals. Tests/Protocol/OtherCitiesTests.cs reads a Dol Amroth merchant's greeting through a client first.</summary>
	public static class OtherCitiesTests {
		internal static readonly string[] NewCities = { "Dol_Amroth", "Erebor", "Dol", "Fornost", "Kralovstvi_lesnich_elfu", "Sede_pristavy", "Nurn", "Rhun" };
		private static readonly TagKey venueTK = TagKey.Acquire("_othercitiestest_venue_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("OTHERCITIES_CHECK: " + name + "=" + ok);
		}

		private static bool Plain(string text) {
			if (text == null) {
				return false;
			}
			foreach (char c in text) {
				if (c >= 128) {
					return false;
				}
			}
			return true;
		}

		private static CityVenue OpenAir(string city, string type) {
			Point4D p = WorldEvents.CentreOf(city);
			int[] spot = null;
			foreach (int[] tile in WorldEvents.Reachable(Map.GetMap(p.M), new[] { p.X, p.Y, p.Z }, 20)) {
				if (WorldSpots.Distance(tile, p.X, p.Y) >= 3) {
					spot = tile;
					break;
				}
			}
			if (spot == null) {
				throw new SEException("No spot for a test venue in " + city + ".");
			}
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
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
			}
			return venue;
		}

		/// <summary>A smithy in Erebor and a market in Dol Amroth with their people; the hero beside a Dol Amroth merchant.</summary>
		[SteamFunction]
		public static void TestOtherCitiesSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				player.SetTag(venueTK, OpenAir("Erebor", "smithy")); //the names (Erebor's centre is under the mountain, its floors in the sight line)
				CityVenue market = OpenAir("Dol_Amroth", "market");
				Item merchant = Residents.OfVenue(market.VenueId).Find(g => Residents.RoleKeyOf(g) == "merchant");
				NPC body = Residents.BodyOf(merchant);
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				Console.WriteLine("OTHERCITIES_SETUP: merchant=" + body.Uid);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		[SteamFunction]
		public static void TestOtherCitiesChain(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Chain(player);
			} finally {
				player.Account.PLevel = plevel;
				VenueClock.Override = null;
				WorldEvents.ResetForTests();
				Console.WriteLine("OTHERCITIES_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			//names by folk, from lists that exist; Erebor's smithy people carry dwarf names
			bool lists = (Residents.NameList("Kralovstvi_lesnich_elfu", false) == "NAMES_ELF_MALE") && (Residents.NameList("Sede_pristavy", true) == "NAMES_ELF_FEMALE")
				&& (Residents.NameList("Erebor", false) == "NAMES_DWARF_MALE") && (Residents.NameList("Dainovo_kralovstvi", true) == "NAMES_DWARF_FEMALE")
				&& (Residents.NameList("Rhun", false) == "NAMES_EASTERLING_MALE") && (Residents.NameList("Dol_Amroth", false) == "NAMES_HUMANMALE")
				&& (Residents.NameList("Hobitin", false) == "NAMES_HOBBIT_MALE");
			foreach (string list in new[] { "NAMES_ELF_MALE", "NAMES_ELF_FEMALE", "NAMES_DWARF_MALE", "NAMES_DWARF_FEMALE", "NAMES_EASTERLING_MALE", "NAMES_EASTERLING_FEMALE" }) {
				lists &= Plain(NameList.Random(list));
			}
			CityVenue smithy = (CityVenue) player.GetTag(venueTK);
			bool dwarves = Residents.OfVenue(smithy.VenueId).Count > 0;
			foreach (Item gem in Residents.OfVenue(smithy.VenueId)) {
				dwarves &= NameList.Contains(Residents.NameList("Erebor", Residents.IsFemale(gem)), Residents.NameOf(gem));
			}
			Report("namesByFolk", lists && dwarves);

			//greetings and news of their own, plain ASCII; Rhun keeps to Mordor's side
			bool talk = true;
			foreach (string city in NewCities) {
				for (int i = 0; i < 6; i++) {
					string greeting = CityFlavour.StrangerGreeting(city), news = Tavern.Lore(city);
					talk &= Plain(greeting) && Plain(news) && (Tavern.LocalLore(city) != null);
				}
			}
			for (int i = 0; i < 30; i++) {
				talk &= Array.IndexOf(Tavern.CommonLore, Tavern.Lore("Rhun")) < 0;
			}
			Report("greetingsAndNewsOfTheirOwn", talk);

			//factions: each new city has some, the smithy's people serve Erebor's smiths, a faction's city has a centre
			bool factions = CityFlavour.FactionOf(Residents.OfVenue(smithy.VenueId).Find(g => Residents.RoleKeyOf(g) == "smith")) == "er_smiths";
			foreach (string city in NewCities) {
				factions &= Array.Exists(Faction.All, f => f.City == city);
			}
			foreach (Faction f in Faction.All) {
				factions &= Plain(f.Name) && (WorldEvents.CentreOf(f.City) != null);
			}
			Report("factionsPerCity", factions);

			//world events: every city has a centre; each kind keeps its own rules; a caravan sets out from each new city
			WorldEventKind raid = WorldEvents.KindOf("najezd"), caravan = WorldEvents.KindOf("karavana");
			bool events = raid.IsFor("Dol_Amroth") && raid.IsFor("Fornost") && !raid.IsFor("Rhun") && !raid.IsFor("Nurn") && caravan.IsFor("Erebor");
			List<string> failed = new List<string>();
			foreach (string city in NewCities) {
				events &= Array.IndexOf(WorldEvents.Cities, city) >= 0;
				WorldEvents.ResetForTests();
				WorldEvent started = WorldEvents.Start(caravan, city);
				if (started == null) {
					failed.Add(city);
				} else {
					WorldEvents.Stop(started);
				}
			}
			Console.WriteLine("OTHERCITIES_INFO: caravans failed in " + string.Join(",", failed));
			Report("eventsInTheOtherCities", events && (failed.Count == 0) && (Array.IndexOf(WorldEvents.Cities, "London") < 0) && (Array.IndexOf(WorldEvents.Cities, "Dainovo_kralovstvi") < 0));

			//festivals where they fit
			Report("festivalsWhereTheyFit", WorldEvents.KindOf("slunovrat").IsFor("Dol_Amroth") && WorldEvents.KindOf("yule").IsFor("Erebor")
				&& WorldEvents.KindOf("dozinky").IsFor("Dol") && WorldEvents.KindOf("den_oka").IsFor("Nurn") && WorldEvents.KindOf("den_oka").IsFor("Rhun")
				&& !WorldEvents.KindOf("yule").IsFor("Rhun") && !WorldEvents.KindOf("den_oka").IsFor("Erebor"));

			//a festival under the mountain: its host stands where the square's guests can walk to
			WorldEvents.ResetForTests();
			Festival yule = WorldEvents.Start(WorldEvents.KindOf("yule"), "Erebor") as Festival;
			bool hostReachable = false;
			if (yule != null) {
				Point4D c = yule.Centre;
				hostReachable = WorldEvents.Reachable(Map.GetMap(c.M), new[] { c.X, c.Y, c.Z }, 3).Exists(t => (t[0] == yule.Host.X) && (t[1] == yule.Host.Y) && (t[2] == yule.Host.Z));
				WorldEvents.Stop(yule);
			}
			Report("aFestivalHostCanBeReached", hostReachable);
		}
	}
}
