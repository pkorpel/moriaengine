using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - the Shire's stories on the server: the mushroom thieves (a client takes it, catches them and lets them off
	/// for the mushrooms, Tests/Protocol/ShireStoryTests.cs), then the birthday party and the silver spoons here.</summary>
	public static class ShireStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("SHIRE_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_shiretest_" + key + "_");
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

		/// <summary>A farm and a tavern in Hobitin, a tavern in Edoras; only the mushroom thieves offered; the hero beside a farmer.</summary>
		[SteamFunction]
		public static void TestShireSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D hobitin = WorldEvents.CentreOf("Hobitin"), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> h = Spots(hobitin, 2);
				player.SetTag(VenueKey("farm"), OpenAir("Hobitin", "farm", h[0], hobitin.M));
				player.SetTag(VenueKey("tavern"), OpenAir("Hobitin", "tavern", h[1], hobitin.M));
				List<int[]> e = Spots(edoras, 2);
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "farm", e[0], edoras.M));
				player.SetTag(VenueKey("edorastavern"), OpenAir("Edoras", "tavern", e[1], edoras.M)); //enough people for a party: only the city keeps it out
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new MushroomThievesErrand());
				player.RemoveTag(Errands.LogTK);
				NPC farmer = Residents.BodyOf(Gem(player, "farm", "farmer"));
				player.Go(new Point4D(farmer.X, farmer.Y, farmer.Z, farmer.M));
				Console.WriteLine("SHIRE_SETUP: farmer=" + farmer.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the next mushroom thief still in the field, held still; its uid, and the errand's id (0, 0 if none).</summary>
		[SteamFunction]
		public static void TestShireBesideThief(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "mushrooms");
			Thing thief = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => (t is NPC) && !t.IsDeleted);
			if (thief != null) {
				Beside(player, thief);
			}
			Console.WriteLine("SHIRE_THIEF: uid=" + ((thief == null) ? 0 : thief.Uid) + " errand=" + ((errand == null) ? 0 : errand.id));
		}

		/// <summary>The hero back beside the farmer for the answer.</summary>
		[SteamFunction]
		public static void TestShireBesideFarmer(Character self) {
			Player player = (Player) self;
			NPC farmer = Residents.BodyOf(Gem(player, "farm", "farmer"));
			player.Go(new Point4D(farmer.X, farmer.Y, farmer.Z, farmer.M));
			Errand errand = Errands.Active(player).Find(e => e.key == "mushrooms");
			Report("theThievesAreCaught", (errand != null) && (errand.index == 1) && (errand.Current.Kind == ErrandStepKind.Choose));
			Console.WriteLine("SHIRE_FARMER: uid=" + farmer.Uid);
		}

		[SteamFunction]
		public static void TestShireState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			int mushrooms = Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname("i_mushroom"));
			int pieces = 0;
			foreach (AbstractItem item in player.Backpack) {
				pieces += (item.Def == ThingDef.GetByDefname("i_mushroom")) ? 1 : 0;
			}
			Report("letOffForTheMushrooms", !Errands.Active(player).Exists(e => e.key == "mushrooms") && (player.Gold > gold) && (mushrooms == MushroomThievesErrand.MushroomsShared)
				&& (pieces == MushroomThievesErrand.MushroomsShared)); //six mushrooms, not one carrying six
			Console.WriteLine("SHIRE_STATE_DONE");
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
			NPC body = Residents.BodyOf(Residents.ById(errand.Current.residentId));
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			return Errands.Do(player, errand, body, choice);
		}

		[SteamFunction]
		public static void TestShireChain(Character self) {
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
				Console.WriteLine("SHIRE_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new BirthdayPartyErrand());
			ErrandTemplates.Register(new SilverSpoonsErrand());
			Item farmer = Gem(player, "farm", "farmer"), innkeeper = Gem(player, "tavern", "innkeeper");
			Item edorasFarmer = Gem(player, "edoras", "farmer");

			//only Hobitin's people give them; the mushrooms only the farmer's folk
			bool onlyHobitin = (Build(player, new MushroomThievesErrand(), edorasFarmer, "Edoras") == null) && (Build(player, new BirthdayPartyErrand(), Gem(player, "edorastavern", "innkeeper"), "Edoras") == null)
				&& (Build(player, new SilverSpoonsErrand(), edorasFarmer, "Edoras") == null);
			Report("onlyInHobitin", onlyHobitin);
			Errand turnedIn = Build(player, new MushroomThievesErrand(), farmer, "Hobitin");
			bool shape = (turnedIn != null) && (turnedIn.steps.Count == 2) && (turnedIn.steps[0].Kind == ErrandStepKind.Things) && (turnedIn.steps[0].count == MushroomThievesErrand.Thieves)
				&& (turnedIn.steps[1].Kind == ErrandStepKind.Choose);
			if (turnedIn != null) {
				turnedIn.steps[1].chosen = 0;
			}
			bool fullPay = (turnedIn != null) && (new MushroomThievesErrand().PayPercent(turnedIn) == 100);
			if (turnedIn != null) {
				turnedIn.steps[1].chosen = MushroomThievesErrand.ShareChoice;
			}
			bool halfPay = (turnedIn != null) && (new MushroomThievesErrand().PayPercent(turnedIn) == 50);
			Report("turnedInPaysInFull", shape && fullPay);
			Report("letOffPaysHalf", halfPay);

			//the birthday party: two invitations, the fireworks fetched and brought, a cake, then the evening; everyone near a guest once a day
			Fresh(player);
			Errand party = Build(player, new BirthdayPartyErrand(), innkeeper, "Hobitin");
			bool partyShape = (party != null) && (party.steps.Count == BirthdayPartyErrand.Invitations + 4) && (party.steps[0].Kind == ErrandStepKind.Deliver)
				&& (party.steps[BirthdayPartyErrand.Invitations].Kind == ErrandStepKind.Talk) && (party.steps[BirthdayPartyErrand.Invitations + 1].itemDef == ShireStories.FireworksDef)
				&& (party.steps[BirthdayPartyErrand.Invitations + 2].Kind == ErrandStepKind.Fetch) && (party.steps[BirthdayPartyErrand.Invitations + 3].Kind == ErrandStepKind.Wait);
			bool done = false, notBeforeEvening = false;
			if (partyShape && Errands.Accept(player, party)) {
				bool steps = true;
				for (int i = 0; steps && (i < BirthdayPartyErrand.Invitations + 2); i++) {
					steps &= Talk(player, party);
				}
				Item cake = (Item) ((ItemDef) ThingDef.GetByDefname("i_cake")).Create(player.Backpack);
				steps &= Talk(player, party);
				VenueClock.Override = 12;
				notBeforeEvening = !Talk(player, party);
				VenueClock.Override = 20;
				player.RemoveTag(ShireStories.PartyAtTK);
				done = steps && Talk(player, party) && !Errands.Active(player).Contains(party) && cake.IsDeleted;
				VenueClock.Override = null;
			}
			bool guest = player.GetTag(ShireStories.PartyAtTK) is long; //a guest at the party the errand held
			NPC host = Residents.BodyOf(innkeeper);
			bool onceADay = BirthdayPartyErrand.Party(host) == 0;
			Errands.NowOverride = DateTime.UtcNow.AddMinutes(30);
			bool notAfterAGameDay = BirthdayPartyErrand.Party(host) == 0; //a game day (24 minutes) isn't enough
			Errands.NowOverride = DateTime.UtcNow.AddHours(BirthdayPartyErrand.GuestEveryHours + 1);
			bool nextDayAgain = BirthdayPartyErrand.Party(host) == 1; //a real one is
			Errands.NowOverride = null;
			onceADay &= nextDayAgain && notAfterAGameDay;
			Report("theBirthdayPartyIsPreparedAndHeld", partyShape && done && notBeforeEvening);
			Report("everyoneNearIsAGuestOnceADay", guest && onceADay);

			//the silver spoons: the relative caught in the streets, the spoons picked up, brought back
			Fresh(player);
			Errand spoons = Build(player, new SilverSpoonsErrand(), farmer, "Hobitin");
			bool spoonsDone = false;
			if ((spoons != null) && Errands.Accept(player, spoons)) {
				List<Thing> relative = Errands.ThingsOf(player, spoons);
				bool caught = (relative.Count == 1) && (relative[0] is NPC);
				if (caught) {
					Beside(player, relative[0]);
					caught = Errands.UseThing(relative[0], player) && (spoons.index == 1);
				}
				List<Thing> found = Errands.ThingsOf(player, spoons);
				bool picked = caught && (found.Count == SilverSpoonsErrand.Spoons) && found.TrueForAll(t => t is Item);
				foreach (Thing spoon in found) {
					Beside(player, spoon);
					picked &= Errands.UseThing(spoon, player);
				}
				spoonsDone = picked && (spoons.index == 2) && Talk(player, spoons) && !Errands.Active(player).Contains(spoons);
			}
			Report("theSpoonsAreTakenBack", spoonsDone);
		}
	}
}
