using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1293 - the errands with things in the world (the Things step) on the server: each template's things placed, caught,
	/// killed or used, and gone with the errand. Tests/Protocol/ThingErrandTests.cs takes the thief through a client and catches him
	/// with a double click first.</summary>
	public static class ThingErrandTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("THINGERRANDS_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string type) {
			return TagKey.Acquire("_thingerrands_" + type + "_");
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

		/// <summary>A tavern, market and temple in Edoras, a mess hall in the war camp; only the thief offered; the hero beside the
		/// merchant.</summary>
		[SteamFunction]
		public static void TestThingErrandsSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D edoras = WorldEvents.CentreOf("Edoras");
				string[] types = { "tavern", "market", "temple" };
				List<int[]> spots = Spots(edoras, types.Length);
				for (int i = 0; i < types.Length; i++) {
					player.SetTag(VenueKey(types[i]), OpenAir("Edoras", types[i], spots[i], edoras.M));
				}
				Point4D camp = WorldEvents.CentreOf("Barad-dur");
				player.SetTag(VenueKey("camp"), OpenAir("Barad-dur", "messhall", Spots(camp, 1)[0], camp.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new ThiefErrand());
				player.RemoveTag(Errands.LogTK);
				NPC merchant = Residents.BodyOf(Gem(player, "market", "merchant"));
				player.Go(new Point4D(merchant.X, merchant.Y, merchant.Z, merchant.M));
				Console.WriteLine("THINGERRANDS_SETUP: merchant=" + merchant.Uid);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The thief of the errand the client took held still, and the hero right beside him.</summary>
		[SteamFunction]
		public static void TestThingErrandsBesideThief(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "thief");
			NPC thief = (errand == null) ? null : Errands.ThingsOf(errand.Current).Find(t => t is NPC) as NPC;
			if (thief == null) {
				Console.WriteLine("THINGERRANDS_THIEF: uid=0");
				return;
			}
			NPCAiTimer.Freeze(thief, 60);
			thief.RemoveTimer(ErrandThingFleeTimer.Key);
			player.Go(new Point4D(thief.X, thief.Y, thief.Z, thief.M));
			Console.WriteLine("THINGERRANDS_THIEF: uid=" + thief.Uid);
		}

		[SteamFunction]
		public static void TestThingErrandsState(Character self) {
			Errand errand = Errands.Active((Player) self).Find(e => e.key == "thief");
			Console.WriteLine("THINGERRANDS_STATE: thief=" + ((errand == null) ? -1 : errand.index));
		}

		private static Item Gem(Player player, string type, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(type));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if ((role == null) || (Residents.RoleKeyOf(gem) == role)) {
					return gem;
				}
			}
			return null;
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city = "Edoras") {
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

		private static bool Talk(Player player, Errand errand) {
			NPC body = Residents.BodyOf(Residents.ById(errand.Current.residentId));
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			return Errands.Do(player, errand, body);
		}

		private static bool Plain(Errand errand) {
			List<string> texts = new List<string> { errand.name, errand.offer, errand.thanks };
			foreach (ErrandStep step in errand.steps) {
				texts.AddRange(new[] { step.task, step.say, step.reply });
			}
			foreach (string text in texts) {
				foreach (char c in text ?? "") {
					if (c > 127) {
						return false;
					}
				}
			}
			return true;
		}

		[SteamFunction]
		public static void TestThingErrandsChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				ThingErrands.Bootstrap();
				Item innkeeper = Gem(player, "tavern", "innkeeper"), merchant = Gem(player, "market", "merchant"), campCook = Gem(player, "camp", null);
				CityVenue tavern = (CityVenue) player.GetTag(VenueKey("tavern"));
				List<Errand> built = new List<Errand>();

				//the client's thief, caught by a double click: the errand went on to bringing the purse back
				Errand caught = Errands.Active(player).Find(e => e.key == "thief");
				Report("thiefCaughtByClick", (caught != null) && (caught.index == 1) && Errands.ThingsOf(caught.steps[0]).Count == 0 && Talk(player, caught) && caught.IsDone);

				//the runaway: the cat in another venue; from afar she can't be caught; the first catch she slips to another venue; the second holds
				Fresh(player);
				Errand runaway = Build(player, new RunawayErrand(), innkeeper);
				bool runOk = (runaway != null) && (runaway.steps[0].Kind == ErrandStepKind.Things) && (runaway.steps[0].venueId != tavern.VenueId);
				if (runOk) {
					Errands.Accept(player, runaway);
					NPC cat = Errands.ThingsOf(runaway.Current).Find(t => t is NPC) as NPC;
					runOk &= (cat != null) && (cat.GetTimer(ErrandThingFleeTimer.Key) != null) && (VenueRegistry.At(cat) == VenueRegistry.ById(runaway.steps[0].venueId));
					NPCAiTimer.Freeze(cat, 60);
					player.Go(new Point4D(cat.X + 3, cat.Y, cat.Z, cat.M));
					runOk &= !Errands.UseThing(cat, player) && (runaway.index == 0);
					Beside(player, cat);
					runOk &= !Errands.UseThing(cat, Residents.BodyOf(innkeeper)) && (cat.GetTag(ThingErrands.MovedTK) == null); //not anyone else's to catch, even with its player beside
					Point4D was = new Point4D(cat.X, cat.Y, cat.Z, cat.M);
					runOk &= Errands.UseThing(cat, player) && !cat.IsDeleted && (cat.GetTag(ThingErrands.MovedTK) != null) && (runaway.index == 0)
						&& (Point2D.GetSimpleDistance(cat, was) > 2);
					if (cat.IsDeleted) {
						runOk = false; //caught at the first try: she didn't slip away
					} else {
						Beside(player, cat);
						runOk &= Errands.UseThing(cat, player) && cat.IsDeleted && (runaway.index == 1) && Talk(player, runaway) && runaway.IsDone;
					}
				}
				Report("runaway", runOk);
				built.Add(runaway);

				//given up, the errand's things go with it
				Fresh(player);
				Errand dropped = Build(player, new RunawayErrand(), innkeeper);
				Errands.Accept(player, dropped);
				List<Thing> left = Errands.ThingsOf(dropped.Current);
				Errands.GiveUp(player, dropped);
				Report("thingsGoWithTheErrand", (left.Count == 1) && left.TrueForAll(t => t.IsDeleted));

				//the lamplighter: four lamps in the streets; a lit one again counts nothing; all lit, on to the giver
				Fresh(player);
				Errand lamps = Build(player, new LamplighterErrand(), innkeeper);
				Errands.Accept(player, lamps);
				List<Thing> placed = Errands.ThingsOf(lamps.Current);
				bool lampOk = (placed.Count == LamplighterErrand.Lamps) && placed.TrueForAll(t => t is Item) && ((lamps.Expires - DateTime.UtcNow).TotalMinutes <= 15.5);
				if (lampOk) {
					Beside(player, placed[0]);
					Errands.UseThing(placed[0], player);
					Errands.UseThing(placed[0], player);
					lampOk &= (lamps.Current.progress == 1) && (((Item) placed[0]).Model == ThingErrands.LitLamp);
					for (int i = 1; i < placed.Count; i++) {
						Beside(player, placed[i]);
						Errands.UseThing(placed[i], player);
					}
					lampOk &= (lamps.index == 1) && Talk(player, lamps) && lamps.IsDone && placed.TrueForAll(t => t.IsDeleted);
				}
				Report("lamplighter", lampOk);
				built.Add(lamps);

				//the cellar: rats in the giver's venue; one killed by nobody doesn't count; killed by the player they do
				Fresh(player);
				Errand cellar = Build(player, new CellarErrand(), innkeeper);
				Errands.Accept(player, cellar);
				List<Thing> rats = Errands.ThingsOf(cellar.Current);
				bool ratOk = (rats.Count == CellarErrand.Rats) && rats.TrueForAll(r => VenueRegistry.At(r) == tavern);
				if (ratOk) {
					((NPC) rats[0]).CauseDeath(null);
					ratOk &= cellar.Current.progress == 0;
					Errands.GiveUp(player, cellar);
					Fresh(player);
					cellar = Build(player, new CellarErrand(), innkeeper);
					Errands.Accept(player, cellar);
					foreach (Thing rat in Errands.ThingsOf(cellar.Current)) {
						Beside(player, rat);
						((NPC) rat).CauseDeath(player);
					}
					ratOk &= (cellar.index == 1) && Talk(player, cellar) && cellar.IsDone && (Build(player, new CellarErrand(), Gem(player, "temple", "priest")) == null);
				}
				Report("cellar", ratOk);
				built.Add(cellar);

				//stop thief: he runs; struck down counts as caught
				Fresh(player);
				Errand thief = Build(player, new ThiefErrand(), merchant);
				Errands.Accept(player, thief);
				NPC pickpocket = Errands.ThingsOf(thief.Current).Find(t => t is NPC) as NPC;
				bool thiefOk = (pickpocket != null) && (pickpocket.GetTimer(ErrandThingFleeTimer.Key) != null);
				if (pickpocket != null) {
					Beside(player, pickpocket);
					pickpocket.CauseDeath(player);
					thiefOk &= (thief.index == 1) && Talk(player, thief) && thief.IsDone;
				}
				Report("thief", thiefOk);
				built.Add(thief);

				//the missing shipment: the cart on the road out of the city; searched, the goods to carry back
				Fresh(player);
				Errand shipment = Build(player, new ShipmentErrand(), merchant);
				Errands.Accept(player, shipment);
				Item cart = Errands.ThingsOf(shipment.Current).Find(t => t is Item) as Item;
				Point4D centre = WorldEvents.CentreOf("Edoras");
				bool shipOk = (cart != null) && (Point2D.GetSimpleDistance(cart, centre) >= 10) && (Build(player, new ShipmentErrand(), innkeeper) == null);
				if (cart != null) {
					Beside(player, cart);
					Errands.UseThing(cart, player);
					shipOk &= (shipment.index == 1) && (Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname(ThingErrands.GoodsDef)) == 0) //the goods are the errand's
						&& Talk(player, shipment) && shipment.IsDone;
				}
				Report("shipment", shipOk);
				built.Add(shipment);

				//press gang: the war camp's only; drunks in the mess hall, each dragged off
				Fresh(player);
				player.Realm = Realm.Mordor;
				Errand gang = Build(player, new PressGangErrand(), campCook, "Barad-dur");
				bool gangOk = (gang != null) && (Build(player, new PressGangErrand(), innkeeper) == null);
				if (gang != null) {
					Errands.Accept(player, gang);
					List<Thing> drunks = Errands.ThingsOf(gang.Current);
					gangOk &= drunks.Count == PressGangErrand.Drunks;
					foreach (Thing drunk in drunks) {
						Beside(player, drunk);
						Errands.UseThing(drunk, player);
					}
					gangOk &= (gang.index == 1) && drunks.TrueForAll(d => d.IsDeleted);
				}
				player.Realm = Realm.Gondor;
				Report("pressgang", gangOk);
				built.Add(gang);

				//not the war camp's (but press gang); every text plain
				ErrandTemplate[] cityOnes = { new RunawayErrand(), new LamplighterErrand(), new CellarErrand(), new ThiefErrand(), new ShipmentErrand() };
				Report("notInTheCamp", (Build(player, new LetterErrand(), campCook, "Barad-dur") != null) && Array.TrueForAll(cityOnes, t => Build(player, t, campCook, "Barad-dur") == null));
				Report("plainCzech", built.TrueForAll(e => (e != null) && Plain(e)));
			} finally {
				player.Realm = Realm.Gondor;
				Fresh(player);
				account.PLevel = plevel;
				foreach (ErrandTemplate template in removed) {
					if (ErrandTemplates.Get(template.Key) == null) {
						ErrandTemplates.Register(template);
					}
				}
				Console.WriteLine("THINGERRANDS_CHAIN_DONE");
			}
		}
	}
}
