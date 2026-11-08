using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1282 - Harad and Umbar on the server: their people's names and looks, the new venue types, the factions errands serve,
	/// and the two errands of their own, a caravan's cargo and contraband past the harbourmaster. Tests/Protocol/HaradUmbarTests.cs
	/// asks a caravan master for work through a client first.</summary>
	public static class HaradUmbarTests {
		private static readonly TagKey venuesTK = TagKey.Acquire("_haradumbartest_venues_");

		private static readonly HashSet<string> haradNames = new HashSet<string> {
			"Azrak", "Bahrun", "Darhan", "Ezir", "Fadun", "Hasran", "Imrun", "Jarrak", "Kazim", "Malek", "Nazar", "Rashun", "Sahim", "Tarik", "Yazdun", "Zahran",
			"Amira", "Bahra", "Dalia", "Esra", "Farah", "Hadra", "Jasra", "Kamra", "Laila", "Nadra", "Rasha", "Salma", "Tahira", "Yara", "Zaida"
		};

		private static readonly HashSet<string> umbarNames = new HashSet<string> {
			"Ardamir", "Azrubel", "Belzagar", "Dalmor", "Erkhan", "Gimrod", "Hazagar", "Imrazor", "Khozar", "Mordrin", "Nimrazor", "Sakalthor", "Tarbek", "Ulvaran",
			"Valandur", "Zagrund", "Adrana", "Azruniel", "Belzira", "Calmira", "Dervala", "Inzilbeth", "Mirzana", "Nimrasil", "Sakara", "Tazmira", "Ulvara", "Zimrael"
		};

		private static void Report(string name, bool ok) {
			Console.WriteLine("HARADUMBAR_CHECK: " + name + "=" + ok);
		}

		private static CityVenue OpenAir(string region, string city, string type, int dx) {
			StaticRegion area = StaticRegion.GetByDefname(region);
			Point4D p = area.P;
			int[] spot = WorldSpots.NearestStandable(Map.GetMap(p.M), p.X + dx, p.Y, 8);
			VenueDraft draft = new VenueDraft();
			draft.SetType(VenueType.Get(type).IndexOf());
			draft.M = p.M;
			draft.City = city;
			draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
			string problem;
			CityVenue venue = draft.Save(out problem);
			if (venue == null) {
				throw new SEException(city + "'s " + type + " wasn't saved: " + problem);
			}
			Residents.FillRoles(venue);
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
			}
			return venue;
		}

		private static Item Role(CityVenue venue, string role) {
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>A caravanserai and a temple in Harad, a harbour and a market in Umbar; only the two new errands; a Mordor hero beside
		/// the caravan master.</summary>
		[SteamFunction]
		public static void TestHaradUmbarSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				CityVenue serai = OpenAir("a_Harad", CityFlavour.Harad, "caravanserai", 0);
				CityVenue temple = OpenAir("a_Harad", CityFlavour.Harad, "temple", 12);
				CityVenue harbour = OpenAir("a_Umbar", CityFlavour.Umbar, "harbour", 0);
				CityVenue market = OpenAir("a_Umbar", CityFlavour.Umbar, "market", 12);
				player.SetTag(venuesTK, new[] { serai, temple, harbour, market });
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new CaravanErrand());
				player.Realm = Realm.Mordor;
				player.RemoveTag(Errands.LogTK);
				NPC master = Residents.BodyOf(Role(serai, "caravanmaster"));
				player.Go(new Point4D(master.X, master.Y, master.Z, master.M));
				Console.WriteLine("HARADUMBAR_SETUP: master=" + master.Uid);
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("HARADUMBAR_SETUP_DONE");
			}
		}

		[SteamFunction]
		public static void TestHaradUmbarState(Character self) {
			Console.WriteLine("HARADUMBAR_STATE: caravans=" + Errands.Active((Player) self).FindAll(e => e.key == "caravan").Count);
		}

		private static void Near(Player player, Item gem) {
			NPC body = Residents.BodyOf(gem);
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
		}

		[SteamFunction]
		public static void TestHaradUmbarChain(Character self) {
			Player player = (Player) self;
			CityVenue[] venues = (CityVenue[]) player.GetTag(venuesTK);
			CityVenue serai = venues[0], temple = venues[1], harbour = venues[2], market = venues[3];
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1;
			try {
				Chain(player, serai, temple, harbour, market);
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("HARADUMBAR_CHAIN_DONE");
		}

		private static void Chain(Player player, CityVenue serai, CityVenue temple, CityVenue harbour, CityVenue market) {
			//names and looks
			bool harad = true, umbar = true, dark = true; //Umbar's skins are the usual random ones, which can fall in the dark range too
			foreach (CityVenue v in new[] { serai, temple }) {
				foreach (Item gem in Residents.OfVenue(v.VenueId)) {
					harad &= haradNames.Contains(Residents.NameOf(gem));
					int color = Residents.BodyOf(gem).Color;
					dark &= (color >= 1050) && (color <= 1058);
				}
			}
			foreach (CityVenue v in new[] { harbour, market }) {
				foreach (Item gem in Residents.OfVenue(v.VenueId)) {
					umbar &= umbarNames.Contains(Residents.NameOf(gem));
				}
			}
			Report("namesAndLooks", harad && umbar && dark);

			//the new venue types, the guest who has no trade, the greetings
			Item traveller = Role(serai, "traveller");
			Report("venuesAndGreetings", VenueType.Get("caravanserai").Mordor && VenueType.Get("corsairden").Mordor && (traveller != null) && !BasicErrands.IsWorker(traveller)
				&& (CityFlavour.StrangerGreeting(CityFlavour.Harad) != null) && (CityFlavour.StrangerGreeting(CityFlavour.Umbar) != null) && (CityFlavour.StrangerGreeting("Edoras") == null));

			//the faction an errand serves, by the giver's workplace
			Item master = Role(serai, "caravanmaster"), priest = Role(temple, "priest"), harbourmaster = Role(harbour, "harbourmaster");
			List<Item> merchants = new List<Item>();
			foreach (Item gem in Residents.OfVenue(market.VenueId)) {
				if (Residents.RoleKeyOf(gem) == "merchant") {
					merchants.Add(gem);
				}
			}
			Report("factions", (CityFlavour.FactionOf(master) == "ha_caravans") && (CityFlavour.FactionOf(priest) == "ha_serpent")
				&& (CityFlavour.FactionOf(harbourmaster) == "um_harbour") && (CityFlavour.FactionOf(merchants[0]) == "um_fences"));

			//the caravan the client took: the cargo to someone of another trade, then word back; the caravan masters' faction gains
			List<Errand> active = Errands.Active(player);
			Errand caravan = active.Find(e => e.key == "caravan");
			bool taken = (caravan != null) && (caravan.steps.Count == 2) && (caravan.steps[0].Kind == ErrandStepKind.Deliver)
				&& (BasicErrands.VenueKey(Residents.ById(caravan.steps[0].residentId)) != "caravanserai");
			bool done = false;
			if (taken) {
				int city = Reputation.Of(player, CityFlavour.Harad), faction = Reputation.OfFaction(player, "ha_caravans");
				Item receiver = Residents.ById(caravan.steps[0].residentId);
				Near(player, receiver);
				Errands.Do(player, caravan, Residents.BodyOf(receiver));
				Near(player, master);
				Errands.Do(player, caravan, Residents.BodyOf(master));
				int rep = ErrandSettings.instance.repMedium;
				done = !Errands.Active(player).Contains(caravan) && (Reputation.Of(player, CityFlavour.Harad) == city + rep)
					&& (Reputation.OfFaction(player, "ha_caravans") == faction + rep);
			}
			//only in Harad, only from caravan work
			ErrandContext elsewhere = new ErrandContext(player, merchants[0], CityFlavour.Umbar);
			ErrandContext temples = new ErrandContext(player, priest, CityFlavour.Harad);
			Report("caravan", taken && done && !new CaravanErrand().TryBuild(elsewhere, new Errand()) && !new CaravanErrand().TryBuild(temples, new Errand()));

			//contraband: past the harbourmaster by bribe, then to the buyer
			ErrandTemplates.Unregister("caravan");
			ErrandTemplates.Register(new ContrabandErrand());
			bool onlyUmbar = !new ContrabandErrand().TryBuild(new ErrandContext(player, master, CityFlavour.Harad), new Errand());
			int harbourRep = Reputation.OfFaction(player, "um_harbour"), fences = Reputation.OfFaction(player, "um_fences"), umbarCity = Reputation.Of(player, CityFlavour.Umbar);
			GoldUtils.Grant(player, (uint) ContrabandErrand.Bribe);
			bool bribed = Contraband(player, merchants[0], 0);
			int rep2 = ErrandSettings.instance.repMedium;
			Report("contrabandBribe", onlyUmbar && bribed && (Reputation.OfFaction(player, "um_harbour") == harbourRep + ContrabandErrand.HarbourLikesBribes)
				&& (Reputation.OfFaction(player, "um_fences") == fences + rep2) && (Reputation.Of(player, CityFlavour.Umbar) == umbarCity + rep2) //the bribe leaves the city alone
				&& (ErrandItems(player) == 0));

			//no gold for the bribe: the harbour's masters remember; a denial with Hiding 100 passes, the chance has its floor
			long own = GoldUtils.CountGoldOn(player);
			if (own > 0) {
				GoldUtils.Pay(player, own, PaySource.Usual, "");
			}
			harbourRep = Reputation.OfFaction(player, "um_harbour");
			bool unpaid = Contraband(player, merchants[1], 0) && (Reputation.OfFaction(player, "um_harbour") <= harbourRep - ContrabandErrand.HarbourLoss);
			player.SetRealSkillValue((int) SkillName.Hiding, 1000);
			harbourRep = Reputation.OfFaction(player, "um_harbour");
			bool denied = Contraband(player, merchants[2], 1) && (Reputation.OfFaction(player, "um_harbour") == harbourRep);
			player.SetRealSkillValue((int) SkillName.Hiding, 0);
			//a denial that fails: the harbour's masters lose 40, their rivals the corsairs gain half, the city only the errand's own
			int perGiver = ErrandSettings.instance.perGiverPerDay;
			ErrandSettings.instance.perGiverPerDay = 9;
			harbourRep = Reputation.OfFaction(player, "um_harbour");
			int corsairs = Reputation.OfFaction(player, "um_corsairs");
			umbarCity = Reputation.Of(player, CityFlavour.Umbar);
			ContrabandErrand.forcedRoll = 99;
			bool caught;
			try {
				caught = Contraband(player, merchants[0], 1) && (Reputation.OfFaction(player, "um_harbour") == harbourRep - ContrabandErrand.HarbourLoss)
					&& (Reputation.OfFaction(player, "um_corsairs") == corsairs + (ContrabandErrand.HarbourLoss / 2)) && (Reputation.Of(player, CityFlavour.Umbar) == umbarCity + rep2);
			} finally {
				ContrabandErrand.forcedRoll = null;
				ErrandSettings.instance.perGiverPerDay = perGiver;
			}
			//guests give nothing and serve no faction
			Item guest = Role(serai, "traveller");
			bool guests = (CityFlavour.FactionOf(null) == null) && (guest != null) && (CityFlavour.FactionOf(guest) == null)
				&& !new CaravanErrand().TryBuild(new ErrandContext(player, guest, CityFlavour.Harad), new Errand());
			Report("contrabandOtherWays", unpaid && denied && caught && guests && (ContrabandErrand.DenialChance(player) == 20) && (ErrandItems(player) == 0));
		}

		private static int ErrandItems(Player player) {
			int count = 0;
			foreach (AbstractItem item in player.Backpack) {
				if (Errands.IsErrandItem(item)) {
					count++;
				}
			}
			return count;
		}

		/// <summary>One contraband run from this fence, the way past the harbourmaster picked by `choice`. True when it ended done.</summary>
		private static bool Contraband(Player player, Item fence, int choice) {
			Errand errand = Errands.Make(player, fence);
			if ((errand == null) || (errand.key != "contraband") || !Errands.Accept(player, errand)) {
				return false;
			}
			Item inspector = Residents.ById(errand.steps[0].residentId);
			Item buyer = Residents.ById(errand.steps[1].residentId);
			bool shape = (errand.steps[0].Kind == ErrandStepKind.Choose) && (BasicErrands.RoleKey(inspector) == "harbourmaster")
				&& (errand.steps[1].Kind == ErrandStepKind.Deliver) && (BasicErrands.VenueKey(buyer) != "harbour");
			Near(player, inspector);
			Errands.Do(player, errand, Residents.BodyOf(inspector), choice);
			Near(player, buyer);
			Errands.Do(player, errand, Residents.BodyOf(buyer));
			Near(player, fence);
			Errands.Do(player, errand, Residents.BodyOf(fence));
			return shape && !Errands.Active(player).Contains(errand);
		}
	}
}
