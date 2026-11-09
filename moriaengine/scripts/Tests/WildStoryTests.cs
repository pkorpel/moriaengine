using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - the wild's stories on the server: the cave creature's riddles (a client takes it, finds the dark pool and types the
	/// answer, Tests/Protocol/WildStoryTests.cs), then a wrong answer, the Black Rider at the ferry and the singing hermit here.</summary>
	public static class WildStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("WILD_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_wildtest_" + key + "_");
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

		/// <summary>A tavern and a market in Edoras, a tavern in Hobitin, a tavern in Harad; only the riddles offered; the hero beside
		/// Edoras's innkeeper.</summary>
		[SteamFunction]
		public static void TestWildSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D edoras = WorldEvents.CentreOf("Edoras"), hobitin = WorldEvents.CentreOf("Hobitin"), harad = WorldEvents.CentreOf("Harad");
				List<int[]> e = Spots(edoras, 2);
				player.SetTag(VenueKey("tavern"), OpenAir("Edoras", "tavern", e[0], edoras.M));
				player.SetTag(VenueKey("market"), OpenAir("Edoras", "market", e[1], edoras.M));
				player.SetTag(VenueKey("hobitin"), OpenAir("Hobitin", "tavern", Spots(hobitin, 1)[0], hobitin.M));
				player.SetTag(VenueKey("harad"), OpenAir("Harad", "tavern", Spots(harad, 1)[0], harad.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new CaveRiddlesErrand());
				player.RemoveTag(Errands.LogTK);
				NPC innkeeper = Residents.BodyOf(Gem(player, "tavern", "innkeeper"));
				player.Go(new Point4D(innkeeper.X, innkeeper.Y, innkeeper.Z, innkeeper.M));
				Console.WriteLine("WILD_SETUP: innkeeper=" + innkeeper.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the dark pool; its uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestWildBesidePool(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "caveriddles");
			Thing pool = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => !t.IsDeleted);
			if (pool != null) {
				Beside(player, pool);
			}
			Console.WriteLine("WILD_POOL: uid=" + ((pool == null) ? 0 : pool.Uid) + " errand=" + ((errand == null) ? 0 : errand.id));
		}

		/// <summary>The answer to the riddle the pool asked (the test's cheat sheet).</summary>
		[SteamFunction]
		public static void TestWildRiddleAnswer(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "caveriddles");
			Thing pool = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => !t.IsDeleted);
			int riddle = ((pool != null) && (pool.GetTag(WildStories.RiddleTK) is int asked)) ? asked : -1;
			Console.WriteLine("WILD_ANSWER: " + ((riddle < 0) ? "none" : Games.Riddles[riddle][1].Replace(' ', '_')) + " end");
		}

		[SteamFunction]
		public static void TestWildBesideInnkeeper(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "caveriddles");
			NPC innkeeper = Residents.BodyOf(Gem(player, "tavern", "innkeeper"));
			player.Go(new Point4D(innkeeper.X, innkeeper.Y, innkeeper.Z, innkeeper.M));
			Report("theRiddleIsWon", (errand != null) && (errand.index == 1));
			Console.WriteLine("WILD_INNKEEPER: uid=" + innkeeper.Uid);
		}

		[SteamFunction]
		public static void TestWildState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theGameIsTold", !Errands.Active(player).Exists(e => e.key == "caveriddles") && (player.Gold > gold));
			Console.WriteLine("WILD_STATE_DONE");
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
		public static void TestWildChain(Character self) {
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
				Console.WriteLine("WILD_CHAIN_DONE");
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
			ErrandTemplates.Register(new BlackRiderErrand());
			ErrandTemplates.Register(new SingingHermitErrand());
			Item innkeeper = Gem(player, "tavern", "innkeeper"), merchant = Gem(player, "market", "merchant"), hobbit = Gem(player, "hobitin", "innkeeper");
			Item haradInnkeeper = Gem(player, "harad", "innkeeper");
			Report("whoGivesThem", (Build(player, new CaveRiddlesErrand(), merchant, "Edoras") == null) && (Build(player, new CaveRiddlesErrand(), haradInnkeeper, "Harad") != null)
				&& (Build(player, new BlackRiderErrand(), innkeeper, "Edoras") == null) && (Build(player, new BlackRiderErrand(), hobbit, "Hobitin") != null)
				&& (Build(player, new SingingHermitErrand(), haradInnkeeper, "Harad") == null) && (Build(player, new SingingHermitErrand(), innkeeper, "Edoras") != null));

			//a wrong answer: the creature comes out at the hero, the pool stays dark a while
			Fresh(player);
			Errand riddles = Build(player, new CaveRiddlesErrand(), innkeeper, "Edoras");
			bool wrongBites = false;
			if ((riddles != null) && Errands.Accept(player, riddles)) {
				List<Thing> pool = Errands.ThingsOf(player, riddles);
				Beside(player, pool[0]);
				Errands.UseThing(pool[0], player); //the riddle asked
				int riddle = (pool[0].GetTag(WildStories.RiddleTK) is int asked) ? asked : 0;
				CaveRiddlesErrand.Answer(player, (Item) pool[0], riddle, "nevim");
				bool creature = false;
				foreach (AbstractCharacter ch in player.GetMap().GetCharsInRange(player.X, player.Y, 6)) {
					if ((ch is NPC npc) && (npc.Def == ThingDef.GetByDefname(WildStories.CreatureDef)) && (npc.GetTimer(WildVanishTimer.Key) != null)) {
						creature = true;
						npc.Delete();
					}
				}
				Errands.UseThing(pool[0], player); //still angry: no riddle, no count
				wrongBites = creature && !pool[0].IsDeleted && (riddles.index == 0) && (pool[0].GetTag(WildStories.AngryUntilTK) is double)
					&& (pool[0].GetTag(WildStories.RiddleTK) == null); //the second use asked nothing
			}
			Report("aWrongAnswerBringsTheCreature", wrongBites);

			//the ferry: the rider sets out as the step begins; the rope pulled, he's gone
			Fresh(player);
			Errand ferry = Build(player, new BlackRiderErrand(), hobbit, "Hobitin");
			bool crossed = false;
			if ((ferry != null) && Errands.Accept(player, ferry)) {
				NPC rider = player.GetTag(WildStories.RiderKey(ferry)) as NPC;
				bool chased = (rider != null) && !rider.IsDeleted && (rider.GetTimer(WildVanishTimer.Key) != null)
					&& (rider.FindLayer(LayerNames.Robe) is Item robe) && robe.HasAttribute(ItemAttributes.attr_newbie) && (Math.Abs((ferry.Expires - Errands.Now).TotalMinutes - 15) < 1);
				crossed = chased && UseAll(player, ferry) && rider.IsDeleted && Talk(player, ferry) && !Errands.Active(player).Contains(ferry);
			}
			Report("theRiderIsLeftOnTheBank", crossed);

			//given up, the rider goes; two ferry errands at once, each its own rider, and one ending leaves the other's
			Fresh(player);
			Item otherHobbit = null;
			foreach (Item gem in Residents.OfVenue(((CityVenue) player.GetTag(VenueKey("hobitin"))).VenueId)) {
				if ((gem != hobbit) && (otherHobbit == null) && (Build(player, new BlackRiderErrand(), gem, "Hobitin") != null)) {
					otherHobbit = gem;
				}
			}
			Errand first = Build(player, new BlackRiderErrand(), hobbit, "Hobitin"), second = (otherHobbit == null) ? null : Build(player, new BlackRiderErrand(), otherHobbit, "Hobitin");
			bool riders = false;
			if ((first != null) && (second != null) && Errands.Accept(player, first) && Errands.Accept(player, second)) {
				NPC firstRider = player.GetTag(WildStories.RiderKey(first)) as NPC, secondRider = player.GetTag(WildStories.RiderKey(second)) as NPC;
				bool two = (firstRider != null) && (secondRider != null) && (firstRider != secondRider) && !firstRider.IsDeleted && !secondRider.IsDeleted;
				Errands.GiveUp(player, first);
				bool firstGone = firstRider.IsDeleted && (player.GetTag(WildStories.RiderKey(first)) == null) && !secondRider.IsDeleted;
				Errands.GiveUp(player, second);
				riders = two && firstGone && secondRider.IsDeleted;
				Console.WriteLine("WILD_RIDERS: two=" + two + " firstGone=" + firstGone + " secondGone=" + secondRider.IsDeleted);
			} else {
				Console.WriteLine("WILD_RIDERS: no second errand (otherHobbit=" + (otherHobbit != null) + ")");
			}
			Report("eachRiderGoesWithItsErrand", riders);

			//the hermit: his hut, then the lost traveller, then home
			Fresh(player);
			Errand hermit = Build(player, new SingingHermitErrand(), innkeeper, "Edoras");
			bool found = false;
			if ((hermit != null) && Errands.Accept(player, hermit)) {
				found = UseAll(player, hermit) && UseAll(player, hermit) && Talk(player, hermit) && !Errands.Active(player).Contains(hermit);
			}
			Report("theHermitFindsTheTraveller", found);
		}
	}
}
