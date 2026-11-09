using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - Dol Amroth's stories on the server: the corsair spies (a client takes it, catches them and brings the letter,
	/// Tests/Protocol/DolAmrothStoryTests.cs), then the swan knights' tourney and the lighthouse here.</summary>
	public static class DolAmrothStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("DOLAMROTH_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_dolamrothtest_" + key + "_");
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

		/// <summary>A harbour and a barracks in Dol Amroth, a harbour in Edoras; only the spies offered; the hero beside the harbourmaster.</summary>
		[SteamFunction]
		public static void TestDolAmrothSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D city = WorldEvents.CentreOf("Dol_Amroth"), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> d = Spots(city, 2);
				player.SetTag(VenueKey("harbour"), OpenAir("Dol_Amroth", "harbour", d[0], city.M));
				player.SetTag(VenueKey("barracks"), OpenAir("Dol_Amroth", "barracks", d[1], city.M));
				Point4D pelargir = WorldEvents.CentreOfArea(StaticRegion.GetByDefname("a_Pelargir"));
				player.SetTag(VenueKey("pelargir"), OpenAir("Dol_Amroth", "harbour", Spots(pelargir, 1)[0], pelargir.M)); //Pelargir is Dol Amroth's on the map
				List<int[]> e = Spots(edoras, 2);
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "harbour", e[0], edoras.M));
				player.SetTag(VenueKey("edorasbarracks"), OpenAir("Edoras", "barracks", e[1], edoras.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new CorsairSpiesErrand());
				player.RemoveTag(Errands.LogTK);
				NPC master = Residents.BodyOf(Gem(player, "harbour", "harbourmaster"));
				player.Go(new Point4D(master.X, master.Y, master.Z, master.M));
				Console.WriteLine("DOLAMROTH_SETUP: master=" + master.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the next spy still free, held still; its uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestDolAmrothBesideSpy(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "corsairspies");
			Thing spy = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => (t is NPC) && !t.IsDeleted);
			if (spy != null) {
				Beside(player, spy);
			}
			Console.WriteLine("DOLAMROTH_SPY: uid=" + ((spy == null) ? 0 : spy.Uid) + " errand=" + ((errand == null) ? 0 : errand.id));
		}

		[SteamFunction]
		public static void TestDolAmrothBesideMaster(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "corsairspies");
			NPC master = Residents.BodyOf(Gem(player, "harbour", "harbourmaster"));
			player.Go(new Point4D(master.X, master.Y, master.Z, master.M));
			Item placed = (errand == null) ? null : errand.Current.placed;
			bool letter = (placed != null) && !placed.IsDeleted && (placed.TopObj() == player) && (placed.Def == ThingDef.GetByDefname(DolAmrothStories.LetterDef));
			Report("theSpiesAreCaughtWithTheLetter", (errand != null) && (errand.index == 1) && letter);
			Console.WriteLine("DOLAMROTH_MASTER: uid=" + master.Uid);
		}

		[SteamFunction]
		public static void TestDolAmrothState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theLetterIsDelivered", !Errands.Active(player).Exists(e => e.key == "corsairspies") && (player.Gold > gold));
			Console.WriteLine("DOLAMROTH_STATE_DONE");
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
		public static void TestDolAmrothChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				Chain(player);
			} finally {
				account.PLevel = plevel;
				VenueClock.Override = null;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("DOLAMROTH_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new SwanTourneyErrand());
			ErrandTemplates.Register(new LighthouseErrand());
			Item master = Gem(player, "harbour", "harbourmaster"), guard = Gem(player, "barracks", null);
			Item edorasMaster = Gem(player, "edoras", "harbourmaster"), edorasGuard = Gem(player, "edorasbarracks", null);
			Report("onlyDolAmrothsOwn", (Build(player, new SwanTourneyErrand(), edorasGuard, "Edoras") == null) && (Build(player, new CorsairSpiesErrand(), edorasMaster, "Edoras") == null)
				&& (Build(player, new LighthouseErrand(), edorasMaster, "Edoras") == null) && (Build(player, new SwanTourneyErrand(), master, "Dol_Amroth") == null)
				&& (Build(player, new LighthouseErrand(), guard, "Dol_Amroth") == null) && (Build(player, new SwanTourneyErrand(), guard, "Dol_Amroth") != null)
				&& (Build(player, new LighthouseErrand(), master, "Dol_Amroth") != null));

			//the tourney: on foot or bare-handed a quintain doesn't fall; mounted and armed it does
			Fresh(player);
			Errand tourney = Build(player, new SwanTourneyErrand(), guard, "Dol_Amroth");
			bool refused = false, ridden = false;
			if ((tourney != null) && Errands.Accept(player, tourney)) {
				List<Thing> quintains = Errands.ThingsOf(player, tourney);
				Beside(player, quintains[0]);
				Item sword = (Item) ((ItemDef) ThingDef.GetByDefname("i_sword_long_iron")).Create(player);
				Errands.UseThing(quintains[0], player); //on foot, armed
				sword.Delete();
				Character horse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(player.P());
				player.Mount = horse;
				Errands.UseThing(quintains[0], player); //mounted, bare-handed
				refused = !quintains[0].IsDeleted && (tourney.index == 0);
				sword = (Item) ((ItemDef) ThingDef.GetByDefname("i_sword_long_iron")).Create(player);
				foreach (Thing quintain in quintains) {
					if (!quintain.IsDeleted) {
						Beside(player, quintain);
						Errands.UseThing(quintain, player);
					}
				}
				ridden = (player.Weapon != null) && (tourney.index == 1) && Talk(player, tourney) && !Errands.Active(player).Contains(tourney);
				player.Dismount();
				horse.Delete();
				sword.Delete();
			}
			Report("theTourneyIsRiddenMountedAndArmed", refused && ridden);

			//the lighthouse: only in the evening; the fire lit on the road; ginseng for the keeper
			Fresh(player);
			Errand lighthouse = Build(player, new LighthouseErrand(), master, "Dol_Amroth");
			bool lit = false, notByDay = false;
			if ((lighthouse != null) && Errands.Accept(player, lighthouse)) {
				VenueClock.Override = 10;
				notByDay = !Talk(player, lighthouse);
				VenueClock.Override = 20;
				bool evening = Talk(player, lighthouse) && (lighthouse.index == 1);
				VenueClock.Override = null;
				List<Thing> fire = Errands.ThingsOf(player, lighthouse);
				if (evening && (fire.Count == 1)) {
					Beside(player, fire[0]);
					Errands.UseThing(fire[0], player);
				}
				Item ginseng = (Item) ((ItemDef) ThingDef.GetByDefname("i_reag_ginseng")).Create(player.Backpack);
				ginseng.Amount = LighthouseErrand.Ginseng;
				lit = evening && (lighthouse.index == 2) && Talk(player, lighthouse) && !Errands.Active(player).Contains(lighthouse) && ginseng.IsDeleted;
			}
			Report("theLighthouseIsLitInTheEvening", lit && notByDay);

			//a Pelargir harbourmaster's spies are in Pelargir's streets, not Dol Amroth's 600 tiles away
			Fresh(player);
			Item pelargirMaster = Gem(player, "pelargir", "harbourmaster");
			Errand spies = Build(player, new CorsairSpiesErrand(), pelargirMaster, "Dol_Amroth");
			bool inPelargir = false;
			if ((spies != null) && Errands.Accept(player, spies)) {
				CityVenue harbour = Residents.WorkplaceOf(pelargirMaster);
				Point4D dolAmroth = WorldEvents.CentreOf("Dol_Amroth");
				List<Thing> found = Errands.ThingsOf(player, spies);
				inPelargir = (found.Count == CorsairSpiesErrand.Spies) && found.TrueForAll(t => (Point2D.GetSimpleDistance(t, harbour) < 60) && (Point2D.GetSimpleDistance(t, dolAmroth) > 200));
				Errands.GiveUp(player, spies);
			}
			Report("aTownsThingsAreInThatTown", inPelargir);
		}
	}
}
