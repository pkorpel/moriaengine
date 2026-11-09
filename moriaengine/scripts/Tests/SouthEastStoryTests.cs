using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - the stories of Umbar, Harad and Rhun on the server: the mumak's calf (a client takes it, catches the calf and
	/// reports, Tests/Protocol/SouthEastStoryTests.cs), then the corsair raid, the spice caravan and the horse trade here.</summary>
	public static class SouthEastStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("SOUTHEAST_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_southeasttest_" + key + "_");
		}

		private static CityVenue OpenAir(string city, string type, int[] spot, byte m) {
			VenueDraft draft = new VenueDraft();
			draft.SetType(VenueType.Get(type).IndexOf());
			draft.M = m;
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

		private static List<int[]> Spots(Point4D p, int count) {
			List<int[]> spots = new List<int[]>();
			foreach (int[] tile in WorldEvents.Reachable(Map.GetMap(p.M), new[] { p.X, p.Y, p.Z }, 25)) {
				if ((WorldSpots.Distance(tile, p.X, p.Y) >= 3) && spots.TrueForAll(o => WorldSpots.Distance(tile, o[0], o[1]) >= 6)) {
					spots.Add(tile);
				}
			}
			if (spots.Count < count) {
				throw new SEException("Only " + spots.Count + " spots for test venues near " + p + ".");
			}
			return spots;
		}

		private static Item Gem(Player player, string key, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(key));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if ((role == null) || (Residents.RoleKeyOf(gem) == role)) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>A caravanserai and a market in Harad, a corsair den and a harbour in Umbar, a stable in Rhun, a stable in Edoras;
		/// only the calf offered; the hero beside Harad's caravan master.</summary>
		[SteamFunction]
		public static void TestSouthEastSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D harad = WorldEvents.CentreOf("Harad"), umbar = WorldEvents.CentreOf("Umbar"), rhun = WorldEvents.CentreOf("Rhun"), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> h = Spots(harad, 2), u = Spots(umbar, 3), r = Spots(rhun, 2);
				player.SetTag(VenueKey("caravanserai"), OpenAir("Harad", "caravanserai", h[0], harad.M));
				player.SetTag(VenueKey("market"), OpenAir("Harad", "market", h[1], harad.M));
				player.SetTag(VenueKey("den"), OpenAir("Umbar", "corsairden", u[0], umbar.M));
				player.SetTag(VenueKey("harbour"), OpenAir("Umbar", "harbour", u[1], umbar.M));
				player.SetTag(VenueKey("umbarmarket"), OpenAir("Umbar", "market", u[2], umbar.M));
				player.SetTag(VenueKey("rhun"), OpenAir("Rhun", "stable", r[0], rhun.M));
				player.SetTag(VenueKey("rhunmarket"), OpenAir("Rhun", "market", r[1], rhun.M));
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "stable", Spots(edoras, 1)[0], edoras.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new MumakCalfErrand());
				player.RemoveTag(Errands.LogTK);
				NPC master = Residents.BodyOf(Gem(player, "caravanserai", "caravanmaster"));
				player.Go(new Point4D(master.X, master.Y, master.Z, master.M));
				Console.WriteLine("SOUTHEAST_SETUP: master=" + master.Uid + " gold=" + player.Gold + " talk=" + Errands.CanTalk(player, master));
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the calf, held still; its uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestSouthEastBesideCalf(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "mumakcalf");
			Thing calf = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => (t is NPC) && !t.IsDeleted);
			if (calf != null) {
				Beside(player, calf);
			}
			Console.WriteLine("SOUTHEAST_CALF: uid=" + ((calf == null) ? 0 : calf.Uid) + " errand=" + ((errand == null) ? 0 : errand.id));
		}

		[SteamFunction]
		public static void TestSouthEastBesideMaster(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "mumakcalf");
			NPC master = Residents.BodyOf(Gem(player, "caravanserai", "caravanmaster"));
			player.Go(new Point4D(master.X, master.Y, master.Z, master.M));
			Report("theCalfIsCaught", (errand != null) && (errand.index == 1));
			Console.WriteLine("SOUTHEAST_MASTER: uid=" + master.Uid);
		}

		[SteamFunction]
		public static void TestSouthEastState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theCalfIsReported", !Errands.Active(player).Exists(e => e.key == "mumakcalf") && (player.Gold > gold));
			Console.WriteLine("SOUTHEAST_STATE_DONE");
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city) {
			Errand errand = new Errand { key = template.Key, name = template.Name, giverId = Residents.IdOf(giver), city = city, tier = (int) template.Tier };
			return template.TryBuild(new ErrandContext(player, giver, city), errand) ? errand : null;
		}

		private static void Fresh(Player player) {
			ErrandLog log = Errands.Log(player);
			foreach (Errand left in new List<Errand>(log.active)) {
				Errands.GiveUp(player, left);
			}
			log.day = -1;
			log.lastKey = "";
			log.streak = 0;
		}

		private static void Beside(Player player, Thing thing) {
			if (thing is NPC npc) {
				NPCAiTimer.Freeze(npc, 60);
			}
			player.Go(new Point4D(thing.X, thing.Y, thing.Z, thing.M)); //#1324: Go keeps one in the halls under the mountain
		}

		private static bool Talk(Player player, Errand errand, int choice = -1) {
			Item gem = (errand.Current == null) ? null : Residents.ById(errand.Current.residentId);
			if (gem == null) {
				return false; //a step without a resident (things): nobody to talk to
			}
			NPC body = Residents.BodyOf(gem);
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M)); //#1324: Go keeps one in the halls under the mountain
			return Errands.Do(player, errand, body, choice);
		}

		[SteamFunction]
		public static void TestSouthEastChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				Chain(player);
			} finally {
				account.PLevel = plevel;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("SOUTHEAST_CHAIN_DONE");
			}
		}

		//the things of the current Things step used one by one; true when the step is done
		private static bool UseAll(Player player, Errand errand) {
			int index = errand.index;
			List<Thing> things = Errands.ThingsOf(player, errand);
			foreach (Thing thing in things) {
				if (!thing.IsDeleted) {
					Beside(player, thing);
					Errands.UseThing(thing, player);
				}
			}
			return (errand.index == index + 1) && things.TrueForAll(t => t.IsDeleted); //each goes as it's used
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new CorsairRaidErrand());
			ErrandTemplates.Register(new SpiceCaravanErrand());
			ErrandTemplates.Register(new HorseTradeErrand());
			Item master = Gem(player, "caravanserai", "caravanmaster"), merchant = Gem(player, "market", "merchant"), captain = Gem(player, "den", "corsaircaptain");
			Item harbourmaster = Gem(player, "harbour", "harbourmaster"), rhunMaster = Gem(player, "rhun", "stablemaster"), edorasMaster = Gem(player, "edoras", "stablemaster");
			Report("onlyTheirOwnCities", (Build(player, new HorseTradeErrand(), edorasMaster, "Edoras") == null) && (Build(player, new MumakCalfErrand(), edorasMaster, "Edoras") == null)
				&& (Build(player, new HorseTradeErrand(), master, "Harad") == null) && (Build(player, new MumakCalfErrand(), rhunMaster, "Rhun") == null)
				&& (Build(player, new SpiceCaravanErrand(), harbourmaster, "Umbar") == null) && (Build(player, new CorsairRaidErrand(), merchant, "Harad") == null)
				&& (Build(player, new MumakCalfErrand(), merchant, "Harad") == null) && (Build(player, new SpiceCaravanErrand(), merchant, "Harad") != null)
				&& (Build(player, new HorseTradeErrand(), rhunMaster, "Rhun") != null) && (Build(player, new CorsairRaidErrand(), harbourmaster, "Umbar") != null)
				&& (Build(player, new CorsairRaidErrand(), Gem(player, "umbarmarket", "merchant"), "Umbar") == null) && (Build(player, new HorseTradeErrand(), Gem(player, "rhunmarket", "merchant"), "Rhun") == null)
				&& (Build(player, new CorsairRaidErrand(), rhunMaster, "Rhun") == null));

			//the raid: two of the crew called, arrows brought
			Fresh(player);
			Errand raid = Build(player, new CorsairRaidErrand(), captain, "Umbar");
			bool ready = false;
			if ((raid != null) && Errands.Accept(player, raid)) {
				bool crew = Talk(player, raid) && Talk(player, raid);
				Item arrows = (Item) ((ItemDef) ThingDef.GetByDefname("i_arrow")).Create(player.Backpack);
				arrows.Amount = CorsairRaidErrand.Arrows;
				ready = crew && Talk(player, raid) && !Errands.Active(player).Contains(raid) && arrows.IsDeleted;
			}
			Report("theRaidIsPrepared", ready);

			//the spice caravan: the bandits laid, the crates picked up, brought
			Fresh(player);
			Errand spice = Build(player, new SpiceCaravanErrand(), merchant, "Harad");
			bool spiced = false;
			if ((spice != null) && Errands.Accept(player, spice)) {
				List<Thing> bandits = Errands.ThingsOf(player, spice);
				bool laid = (bandits.Count == SpiceCaravanErrand.Bandits) && bandits.TrueForAll(t => t is NPC);
				foreach (Thing bandit in bandits) {
					((NPC) bandit).CauseDeath(player);
				}
				spiced = laid && (spice.index == 1) && UseAll(player, spice) && Talk(player, spice) && !Errands.Active(player).Contains(spice);
			}
			Report("theSpicesAreRecovered", spiced);

			//the horse trade: two wild horses caught, sold
			Fresh(player);
			Errand trade = Build(player, new HorseTradeErrand(), rhunMaster, "Rhun");
			bool sold = false;
			if ((trade != null) && Errands.Accept(player, trade)) {
				sold = UseAll(player, trade) && Talk(player, trade) && !Errands.Active(player).Contains(trade);
			}
			Report("theHorsesAreSold", sold);
		}
	}
}
