using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - the woodland realm's stories on the server: the barrel run (a client takes it, pushes the barrels off and reports,
	/// Tests/Protocol/WoodlandStoryTests.cs), then the vanishing feast, the spiders and the wine from Rhun here.</summary>
	public static class WoodlandStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();
		private const string Woodland = "Kralovstvi_lesnich_elfu";

		private static void Report(string name, bool ok) {
			Console.WriteLine("WOODLAND_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_woodlandtest_" + key + "_");
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

		/// <summary>A tavern and a library in the woodland realm, a tavern in Edoras; only the barrel run offered; the hero beside the
		/// innkeeper.</summary>
		[SteamFunction]
		public static void TestWoodlandSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D wood = WorldEvents.CentreOf(Woodland), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> w = Spots(wood, 2);
				player.SetTag(VenueKey("tavern"), OpenAir(Woodland, "tavern", w[0], wood.M));
				player.SetTag(VenueKey("library"), OpenAir(Woodland, "library", w[1], wood.M));
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "tavern", Spots(edoras, 1)[0], edoras.M));
				Point4D lorien = WorldEvents.CentreOfArea(StaticRegion.GetByDefname("a_Lorien"));
				player.SetTag(VenueKey("lorien"), OpenAir(Woodland, "tavern", Spots(lorien, 1)[0], lorien.M)); //the same city on the map
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new BarrelRunErrand());
				player.RemoveTag(Errands.LogTK);
				NPC innkeeper = Residents.BodyOf(Gem(player, "tavern", "innkeeper"));
				player.Go(new Point4D(innkeeper.X, innkeeper.Y, innkeeper.Z, innkeeper.M));
				Console.WriteLine("WOODLAND_SETUP: innkeeper=" + innkeeper.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the next barrel; its uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestWoodlandBesideBarrel(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "barrelrun");
			Thing barrel = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => !t.IsDeleted);
			if (barrel != null) {
				Beside(player, barrel);
			}
			Console.WriteLine("WOODLAND_BARREL: uid=" + ((barrel == null) ? 0 : barrel.Uid) + " errand=" + ((errand == null) ? 0 : errand.id));
		}

		[SteamFunction]
		public static void TestWoodlandBesideInnkeeper(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "barrelrun");
			NPC innkeeper = Residents.BodyOf(Gem(player, "tavern", "innkeeper"));
			player.Go(new Point4D(innkeeper.X, innkeeper.Y, innkeeper.Z, innkeeper.M));
			Report("theBarrelsFloatInTime", (errand != null) && (errand.index == 1) && (Math.Abs((errand.Expires - Errands.Now).TotalMinutes - 10) < 2));
			Console.WriteLine("WOODLAND_INNKEEPER: uid=" + innkeeper.Uid);
		}

		[SteamFunction]
		public static void TestWoodlandState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theRunIsReported", !Errands.Active(player).Exists(e => e.key == "barrelrun") && (player.Gold > gold));
			Console.WriteLine("WOODLAND_STATE_DONE");
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
			player.Go(new Point4D(thing.X, thing.Y, thing.Z, thing.M));
		}

		private static bool Talk(Player player, Errand errand, int choice = -1) {
			Item gem = (errand.Current == null) ? null : Residents.ById(errand.Current.residentId);
			if (gem == null) {
				return false; //a step without a resident (things): nobody to talk to
			}
			NPC body = Residents.BodyOf(gem);
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			return Errands.Do(player, errand, body, choice);
		}

		[SteamFunction]
		public static void TestWoodlandChain(Character self) {
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
				Console.WriteLine("WOODLAND_CHAIN_DONE");
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
			ErrandTemplates.Register(new VanishingFeastErrand());
			ErrandTemplates.Register(new SpiderNestsErrand());
			ErrandTemplates.Register(new EastWineErrand());
			Item innkeeper = Gem(player, "tavern", "innkeeper"), librarian = Gem(player, "library", "librarian"), edorasInnkeeper = Gem(player, "edoras", "innkeeper");
			Report("onlyTheWoodlandRealm", (Build(player, new VanishingFeastErrand(), edorasInnkeeper, "Edoras") == null) && (Build(player, new SpiderNestsErrand(), edorasInnkeeper, "Edoras") == null)
				&& (Build(player, new BarrelRunErrand(), edorasInnkeeper, "Edoras") == null) && (Build(player, new EastWineErrand(), edorasInnkeeper, "Edoras") == null)
				&& (Build(player, new EastWineErrand(), librarian, Woodland) == null) && (Build(player, new BarrelRunErrand(), librarian, Woodland) == null)
				&& (Build(player, new BarrelRunErrand(), Gem(player, "lorien", "innkeeper"), Woodland) == null) && (Build(player, new VanishingFeastErrand(), Gem(player, "lorien", "innkeeper"), Woodland) == null)
				&& (Build(player, new VanishingFeastErrand(), librarian, Woodland) != null) && (Build(player, new EastWineErrand(), innkeeper, Woodland) != null));

			//the feast: three lights, each gone as reached; told
			Fresh(player);
			Errand feast = Build(player, new VanishingFeastErrand(), librarian, Woodland);
			bool feastDone = (feast != null) && Errands.Accept(player, feast) && UseAll(player, feast) && Talk(player, feast) && !Errands.Active(player).Contains(feast);
			Report("theFeastsLightsVanish", feastDone);

			//the spiders: three laid on the road out
			Fresh(player);
			Errand nests = Build(player, new SpiderNestsErrand(), innkeeper, Woodland);
			bool cleared = false;
			if ((nests != null) && Errands.Accept(player, nests)) {
				List<Thing> spiders = Errands.ThingsOf(player, nests);
				cleared = (spiders.Count == SpiderNestsErrand.Spiders) && spiders.TrueForAll(t => t is NPC);
				foreach (Thing spider in spiders) {
					((NPC) spider).CauseDeath(player);
				}
				cleared &= (nests.index == 1) && Talk(player, nests) && !Errands.Active(player).Contains(nests);
			}
			Report("theSpidersAreCleared", cleared);

			//the wine: the cask waits in Rhun's streets, far from the woods
			Fresh(player);
			Errand wine = Build(player, new EastWineErrand(), innkeeper, Woodland);
			bool fetched = false;
			if ((wine != null) && Errands.Accept(player, wine)) {
				List<Thing> cask = Errands.ThingsOf(player, wine);
				Point4D rhun = WorldEvents.CentreOf("Rhun");
				bool inRhun = (cask.Count == 1) && (cask[0].M == rhun.M) && (Point2D.GetSimpleDistance(cask[0], rhun) <= Errands.ThingSpread);
				fetched = inRhun && UseAll(player, wine) && Talk(player, wine) && !Errands.Active(player).Contains(wine);
			}
			Report("theWineIsFetchedFromRhun", fetched);
		}
	}
}
