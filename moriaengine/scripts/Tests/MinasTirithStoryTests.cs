using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - Minas Tirith's stories on the server: the Steward's archives (a client takes it, searches the shelves, gets the
	/// page from the borrower and returns it, Tests/Protocol/MinasTirithStoryTests.cs), then the forgotten herb here.</summary>
	public static class MinasTirithStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("TIRITH_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_tirithtest_" + key + "_");
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

		/// <summary>A library and Houses of Healing in Minas Tirith, a library in Edoras; only the archives offered; the hero beside the
		/// librarian.</summary>
		[SteamFunction]
		public static void TestTirithSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D tirith = WorldEvents.CentreOf("Minas_Tirith"), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> t = Spots(tirith, 2);
				player.SetTag(VenueKey("library"), OpenAir("Minas_Tirith", "library", t[0], tirith.M));
				player.SetTag(VenueKey("healing"), OpenAir("Minas_Tirith", "healing", t[1], tirith.M));
				List<int[]> e = Spots(edoras, 2);
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "library", e[0], edoras.M));
				player.SetTag(VenueKey("edorashealing"), OpenAir("Edoras", "healing", e[1], edoras.M)); //a healer elsewhere: only the city keeps the herb out
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new StewardArchivesErrand());
				player.RemoveTag(Errands.LogTK);
				NPC librarian = Residents.BodyOf(Gem(player, "library", "librarian"));
				player.Go(new Point4D(librarian.X, librarian.Y, librarian.Z, librarian.M));
				Console.WriteLine("TIRITH_SETUP: librarian=" + librarian.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the next unsearched shelf; its uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestTirithBesideShelf(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "archives");
			Thing shelf = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => !t.IsDeleted);
			bool inArchive = (shelf != null) && Residents.WorkplaceOf(Gem(player, "library", "librarian")).Contains(shelf.X, shelf.Y, shelf.Z);
			if (shelf != null) {
				Beside(player, shelf);
			}
			Console.WriteLine("TIRITH_SHELF: uid=" + ((shelf == null) ? 0 : shelf.Uid) + " errand=" + ((errand == null) ? 0 : errand.id) + " inArchive=" + inArchive);
		}

		/// <summary>The hero beside the resident of the errand's current step; its uid (0 if none).</summary>
		[SteamFunction]
		public static void TestTirithBesideStep(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "archives");
			Item gem = ((errand == null) || (errand.Current == null)) ? null : Residents.ById(errand.Current.residentId);
			NPC body = (gem == null) ? null : Residents.BodyOf(gem);
			if (body != null) {
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			}
			Console.WriteLine("TIRITH_STEP: uid=" + ((body == null) ? 0 : body.Uid) + " index=" + ((errand == null) ? -1 : errand.index));
		}

		[SteamFunction]
		public static void TestTirithState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("thePageIsBack", !Errands.Active(player).Exists(e => e.key == "archives") && (player.Gold > gold));
			Console.WriteLine("TIRITH_STATE_DONE");
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
		public static void TestTirithChain(Character self) {
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
				Console.WriteLine("TIRITH_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new ForgottenHerbErrand());
			Item librarian = Gem(player, "library", "librarian"), student = Gem(player, "library", "student"), healer = Gem(player, "healing", "healer");
			Item patient = Gem(player, "healing", "patient"), edorasLibrarian = Gem(player, "edoras", "librarian");
			Report("onlyMinasTirithsOwn", (Build(player, new StewardArchivesErrand(), edorasLibrarian, "Edoras") == null) && (Build(player, new ForgottenHerbErrand(), Gem(player, "edorashealing", "healer"), "Edoras") == null) && (Build(player, new StewardArchivesErrand(), student, "Minas_Tirith") == null)
				&& (Build(player, new ForgottenHerbErrand(), patient, "Minas_Tirith") == null) && (Build(player, new ForgottenHerbErrand(), librarian, "Minas_Tirith") == null)
				&& (Build(player, new StewardArchivesErrand(), librarian, "Minas_Tirith") != null) && (Build(player, new ForgottenHerbErrand(), healer, "Minas_Tirith") != null));

			//the archives: the last shelf names the borrower, who is the next step
			Errand mystery = Build(player, new StewardArchivesErrand(), librarian, "Minas_Tirith");
			Item borrower = (mystery == null) ? null : Residents.ById(mystery.steps[1].residentId);
			Report("theLastShelfNamesTheBorrower", (borrower != null) && mystery.steps[0].reply.Contains(Residents.NameOf(borrower)) && (mystery.steps[2].itemDef == MinasTirithStories.PageDef));

			//the herb: the old one's rhyme, three plants on the road out, brought to the healer
			Fresh(player);
			Errand herb = Build(player, new ForgottenHerbErrand(), healer, "Minas_Tirith");
			bool done = false;
			if ((herb != null) && Errands.Accept(player, herb)) {
				bool rhyme = Talk(player, herb) && (herb.index == 1);
				List<Thing> plants = Errands.ThingsOf(player, herb);
				bool gathered = rhyme && (plants.Count == ForgottenHerbErrand.Plants);
				foreach (Thing plant in plants) {
					Beside(player, plant);
					gathered &= Errands.UseThing(plant, player);
				}
				done = gathered && (herb.index == 2) && Talk(player, herb) && !Errands.Active(player).Contains(herb);
			}
			Report("theForgottenHerbIsFoundAndBrought", done);
		}
	}
}
