using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1284 - the world events on the server: the orc raid and the caravan, won and lost, their outcomes on prices and
	/// the criers, leftovers after a restart, and the scheduler. Tests/Protocol/WorldEventsTests.cs starts a raid with a GM's
	/// .udalost through a client and reads the journal's Udalosti tab first.</summary>
	public static class WorldEventsTests {
		private static void Report(string name, bool ok) {
			Console.WriteLine("WORLDEVENTS_CHECK: " + name + "=" + ok);
		}

		private static void GoTo(Player player, Point4D at) {
			player.Go(at);
		}

		/// <summary>No events or outcomes; a Gondor hero (still a GM, for .udalost) at Edoras' centre.</summary>
		[SteamFunction]
		public static void TestWorldEventsSetup(Character self) {
			Player player = (Player) self;
			WorldEvents.ResetForTests();
			CityNews.Clear();
			player.Realm = Realm.Gondor;
			GoTo(player, WorldEvents.CentreOf("Edoras"));
			Console.WriteLine("WORLDEVENTS_SETUP_DONE");
		}

		[SteamFunction]
		public static void TestWorldEventsState(Character self) {
			IList<WorldEvent> running = WorldEvents.Running;
			Console.WriteLine("WORLDEVENTS_STATE: running=" + running.Count + ((running.Count > 0) ? " kind=" + running[0].Kind.Key + " city=" + running[0].City : "")
				+ (((running.Count > 0) && (running[0] is Beacons beacons)) ? " lit=" + beacons.Lit : "")
				+ (((running.Count > 0) && (running[0] is Fire fire)) ? " burning=" + fire.Burning.Count : "")
				+ (((running.Count > 0) && (running[0] is Fever fever)) ? " sick=" + fever.Sick.Count : "")
				+ (((running.Count > 0) && (running[0] is Festival feast)) ? " fed=" + feast.Fed.Count + " rounds=" + feast.Rounds : ""));
		}

		/// <summary>.TestWorldEventsBeacon n: the hero stands beside the running chain's beacon n (from 0), and its uid is printed.</summary>
		[SteamFunction]
		public static void TestWorldEventsBeacon(Character self, ScriptArgs sa) {
			int index = int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			Beacons beacons = (WorldEvents.Running.Count > 0) ? WorldEvents.Running[0] as Beacons : null;
			if ((beacons == null) || (index >= beacons.Items.Count)) {
				Console.WriteLine("WORLDEVENTS_BEACON: none uid=0");
				return;
			}
			Item beacon = beacons.Items[index];
			self.Go(new Point4D(beacon.X + 1, beacon.Y, beacon.Z, beacon.M));
			Console.WriteLine("WORLDEVENTS_BEACON: uid=" + beacon.Uid);
		}

		[SteamFunction]
		public static void TestWorldEventsJournal(Character self) {
			QuestJournal.Open((Player) self, D_QuestJournal.SectionEvents);
			Console.WriteLine("WORLDEVENTS_JOURNAL_DONE");
		}

		private static void KillAll(WorldEvent e, Player player, Predicate<NPC> which) {
			foreach (NPC npc in e.Npcs.ToArray()) {
				if (!npc.IsDeleted && !npc.Flag_Dead && ((which == null) || which(npc))) {
					player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
					npc.CauseDeath(player);
				}
			}
		}

		private static readonly TagKey houseTK = TagKey.Acquire("_worldeventtest_house_");

		/// <summary>#1286 - a tavern in Edoras with its people (frozen), then a fire there; the hero beside its first fire.</summary>
		[SteamFunction]
		public static void TestWorldEventsCitySetup(Character self) {
			Player player = (Player) self;
			WorldEvents.ResetForTests();
			CityNews.Clear();
			player.Realm = Realm.Gondor;
			CityVenue house = player.GetTag(houseTK) as CityVenue;
			if ((house == null) || house.IsDeleted) {
				byte plevel = player.Account.PLevel;
				player.Account.PLevel = 7;
				try {
					Point4D p = WorldEvents.CentreOf("Edoras");
					int[] spot = WorldSpots.NearestStandable(Map.GetMap(p.M), p.X + 8, p.Y, 8);
					VenueDraft draft = new VenueDraft();
					draft.SetType(VenueType.Get("tavern").IndexOf());
					draft.M = p.M;
					draft.City = "Edoras";
					draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
					house = draft.Save(out string problem);
					if (house == null) {
						throw new SEException("The test tavern wasn't saved: " + problem);
					}
					Residents.FillRoles(house);
				} finally {
					player.Account.PLevel = plevel;
				}
				player.SetTag(houseTK, house);
			}
			foreach (Item gem in Residents.OfVenue(house.VenueId)) {
				NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
			}
			Fire fire = WorldEvents.Start(WorldEvents.KindOf("pozar"), "Edoras") as Fire;
			Item first = fire.Burning[0];
			player.Go(new Point4D(first.X + 1, first.Y, first.Z, first.M));
			player.Stam = 100;
			Console.WriteLine("WORLDEVENTS_CITY: fire=" + first.Uid + " burning=" + fire.Burning.Count + " house=" + ((fire.House == house) ? 1 : 0));
		}

		/// <summary>#1286 - the fire put away, a fever in Edoras; the hero with a bandage and a ginseng beside the first sick one.</summary>
		[SteamFunction]
		public static void TestWorldEventsFeverStart(Character self) {
			Player player = (Player) self;
			WorldEvents.ResetForTests();
			Fever fever = WorldEvents.Start(WorldEvents.KindOf("horecka"), "Edoras") as Fever;
			ItemDef bandage = (ItemDef) ThingDef.GetByDefname(FeverKind.BandageDef), herb = (ItemDef) ThingDef.GetByDefname(FeverKind.HerbDef);
			Errands.Consume(player.Backpack, bandage, Errands.CountIn(player.Backpack, bandage)); //a new character's kit may hold some
			Errands.Consume(player.Backpack, herb, Errands.CountIn(player.Backpack, herb));
			ThingDef.GetByDefname(FeverKind.BandageDef).Create(player.Backpack);
			ThingDef.GetByDefname(FeverKind.HerbDef).Create(player.Backpack);
			NPC sick = fever.Sick[0];
			player.Go(new Point4D(sick.X + 1, sick.Y, sick.Z, sick.M));
			Console.WriteLine("WORLDEVENTS_FEVER: sick=" + fever.Sick.Count + " uid=" + sick.Uid);
		}

		/// <summary>#1286 - fire and fever on the server, after the client's blows and cure.</summary>
		[SteamFunction]
		public static void TestWorldEventsCityChain(Character self) {
			Player player = (Player) self;
			WorldEventSettings s = WorldEventSettings.instance;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			int fireMax = s.fireMax;
			CityVenue house = player.GetTag(houseTK) as CityVenue;
			try {
				account.PLevel = 1;
				ItemDef bandage = (ItemDef) ThingDef.GetByDefname(FeverKind.BandageDef), herb = (ItemDef) ThingDef.GetByDefname(FeverKind.HerbDef);

				//the fever the client began curing: one cure took both things; the others cured here, the curer paid and remembered
				Fever fever = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as Fever : null;
				bool clientCured = (fever != null) && (fever.Curers.Count == 0) && (Errands.CountIn(player.Backpack, bandage) == 0)
					&& (Errands.CountIn(player.Backpack, herb) == 0); //the GM's cure counts for the sick, not the pay
				bool refusedEmpty = false, curedHere = false;
				long gold = player.Gold;
				int cures = 0;
				if (fever != null) {
					NPC next = fever.Sick[0];
					player.Go(new Point4D(next.X + 1, next.Y, next.Z, next.M));
					int before = fever.Sick.Count;
					refusedEmpty = !Fever.TryCure(next, player) && (fever.Sick.Count == before) && Errands.CanTalk(player, next); //near, but nothing to cure with: the usual talk
					string id = Residents.IdOf(Residents.GemOf(next));
					int friendship = Residents.Friendship(player, id);
					foreach (NPC one in fever.Sick.ToArray()) {
						bandage.Create(player.Backpack);
						herb.Create(player.Backpack);
						player.Go(new Point4D(one.X + 1, one.Y, one.Z, one.M));
						if (Fever.TryCure(one, player) && !fever.Sick.Contains(one)) {
							cures++;
						}
					}
					curedHere = (cures == before) && (Residents.Friendship(player, id) == friendship); //friendship only when it's beaten
					WorldEvents.Tick();
					curedHere &= Residents.Friendship(player, id) == Math.Min(Residents.MaxFriendship, friendship + s.feverFriendship);
				}
				Report("feverCured", clientCured && refusedEmpty && curedHere && fever.Ended && fever.Won
					&& (player.Gold - gold == s.feverGold * cures) && CityNews.Of("Edoras").Exists(t => t.Contains("ustoupila")));

				WorldEvents.ResetForTests();
				Fever untreated = WorldEvents.Start(WorldEvents.KindOf("horecka"), "Edoras") as Fever;
				List<NPC> sick = (untreated == null) ? new List<NPC>() : new List<NPC>(untreated.Sick);
				if (untreated != null) {
					untreated.EndsAt = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				Report("feverLost", (untreated != null) && untreated.Ended && !untreated.Won && (sick.Count > 0) && sick.TrueForAll(n => !n.IsDeleted)
					&& !Fever.TryCure(sick[0], player) && CityNews.Of("Edoras").Exists(t => t.Contains("svou dan")));

				//a sick one killed before the cure: the fever isn't beaten, whatever the others' cures
				WorldEvents.ResetForTests();
				Fever deadly = WorldEvents.Start(WorldEvents.KindOf("horecka"), "Edoras") as Fever;
				gold = player.Gold;
				if ((deadly != null) && (deadly.Sick.Count >= 2)) {
					deadly.Sick[0].CauseDeath(null);
					WorldEvents.Tick();
					foreach (NPC one in deadly.Sick.ToArray()) {
						bandage.Create(player.Backpack);
						herb.Create(player.Backpack);
						player.Go(new Point4D(one.X + 1, one.Y, one.Z, one.M));
						Fever.TryCure(one, player);
					}
				}
				WorldEvents.Tick();
				Report("feverDeaths", (deadly != null) && deadly.Ended && !deadly.Won && (deadly.Lost == 1) && (player.Gold == gold)
					&& CityNews.Of("Edoras").Exists(t => t.Contains("ne vsichni")));

				//the fire: the house's, blows cost stamina, a tired hero can't, from afar nobody can; it spreads; all out pays and is remembered
				WorldEvents.ResetForTests();
				Fire fire = WorldEvents.Start(WorldEvents.KindOf("pozar"), "Edoras") as Fire;
				bool atHouse = (fire != null) && (fire.House == house) && (fire.Burning.Count == s.fireStart) && fire.Describe().Contains(house.Title);
				E_EventFire click = SingletonScript<E_EventFire>.Instance;
				bool blows = false;
				if (fire != null) {
					Item one = fire.Burning[0];
					player.Go(new Point4D(one.X + E_EventFire.Reach + 2, one.Y, one.Z, one.M));
					player.Stam = 100;
					click.On_DClick(one, player);
					bool farRefused = player.Stam == 100;
					player.Go(new Point4D(one.X + 1, one.Y, one.Z, one.M));
					player.Stam = (short) (s.fireBeatStamina - 1);
					click.On_DClick(one, player);
					bool tiredRefused = !fire.Helpers.ContainsKey(player);
					player.Stam = 100;
					for (int i = 0; i < s.fireBeats; i++) {
						click.On_DClick(one, player);
					}
					blows = farRefused && tiredRefused && one.IsDeleted && (fire.Helpers[player] == s.fireBeats) && (player.Stam == 100 - (s.fireBeats * s.fireBeatStamina));
				}
				int burning = (fire == null) ? 0 : fire.Burning.Count;
				if (fire != null) {
					fire.nextSpread = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				bool spread = (fire != null) && (fire.Burning.Count == burning + 1);
				Report("fireBlows", atHouse && blows && spread);

				Item member = null;
				if (house != null) {
					foreach (Item gem in Residents.OfVenue(house.VenueId)) {
						member = member ?? gem;
					}
				}
				int houseFriend = (member == null) ? 0 : Residents.Friendship(player, Residents.IdOf(member));
				gold = player.Gold;
				if (fire != null) {
					foreach (Item one in fire.Burning) {
						player.Go(new Point4D(one.X + 1, one.Y, one.Z, one.M));
						for (int i = 0; i < s.fireBeats; i++) {
							player.Stam = 100;
							fire.Beat(one, player);
						}
					}
				}
				WorldEvents.Tick();
				Report("fireOut", (fire != null) && fire.Ended && fire.Won && (player.Gold - gold == s.fireGold) && (member != null)
					&& (Residents.Friendship(player, Residents.IdOf(member)) == Math.Min(Residents.MaxFriendship, houseFriend + s.fireFriendship)) && CityNews.Of("Edoras").Exists(t => t.Contains("uhasit")));

				//a blow or two isn't enough to be paid
				WorldEvents.ResetForTests();
				Fire brief = WorldEvents.Start(WorldEvents.KindOf("pozar"), "Edoras") as Fire;
				gold = player.Gold;
				if (brief != null) {
					Item one = brief.Burning[0];
					player.Go(new Point4D(one.X + 1, one.Y, one.Z, one.M));
					player.Stam = 100;
					brief.Beat(one, player);
					foreach (Item other in brief.Burning) {
						other.Delete();
					}
				}
				WorldEvents.Tick();
				Report("fireOneBlowUnpaid", (brief != null) && brief.Won && (brief.Helpers[player] == 1) && (player.Gold == gold));

				//time up: the house burns down
				WorldEvents.ResetForTests();
				Fire slow = WorldEvents.Start(WorldEvents.KindOf("pozar"), "Edoras") as Fire;
				if (slow != null) {
					slow.EndsAt = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				bool timedOut = (slow != null) && slow.Ended && !slow.Won;

				//too many fires: the house burns down
				WorldEvents.ResetForTests();
				Fire lost = WorldEvents.Start(WorldEvents.KindOf("pozar"), "Edoras") as Fire;
				if (lost != null) {
					s.fireMax = s.fireStart + 1; //the least it can be: one spread over the start
					lost.Spread();
				}
				WorldEvents.Tick();
				Report("fireLost", timedOut && (lost != null) && lost.Ended && !lost.Won && lost.Items.TrueForAll(f => f.IsDeleted) && CityNews.Of("Edoras").Exists(t => t.Contains("spaleniste")));
			} finally {
				s.fireMax = fireMax;
				account.PLevel = plevel;
				WorldEvents.ResetForTests();
			}
			Console.WriteLine("WORLDEVENTS_CITY_DONE");
		}

		private static readonly DateTime Midsummer = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

		/// <summary>#1289 - midsummer's day: the calendar starts Minas Tirith's festival for the hero there (a player for a moment:
		/// GMs don't count as guests); the hero beside its host.</summary>
		[SteamFunction]
		public static void TestWorldEventsFestivalSetup(Character self) {
			Player player = (Player) self;
			WorldEvents.ResetForTests();
			CityNews.Clear();
			Errands.NowOverride = Midsummer;
			player.Realm = Realm.Gondor;
			GoTo(player, WorldEvents.CentreOf("Minas_Tirith"));
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1;
			int started = WorldEvents.TryStartByCalendar();
			player.Account.PLevel = plevel;
			Festival feast = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as Festival : null;
			if (feast == null) {
				Console.WriteLine("WORLDEVENTS_FESTIVAL: none started=" + started + " host=0");
				return;
			}
			player.Go(new Point4D(feast.Host.X + 1, feast.Host.Y, feast.Host.Z, feast.Host.M));
			Console.WriteLine("WORLDEVENTS_FESTIVAL: started=" + started + " host=" + feast.Host.Uid);
		}

		/// <summary>#1289 - the hero right beside the runaway animal (held still for the client's click).</summary>
		[SteamFunction]
		public static void TestWorldEventsFestivalAnimal(Character self) {
			Festival feast = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as Festival : null;
			NPC animal = (feast == null) ? null : feast.Animal;
			if (animal == null) {
				Console.WriteLine("WORLDEVENTS_ANIMAL: uid=0");
				return;
			}
			NPCAiTimer.Freeze(animal, 60);
			self.Go(new Point4D(animal.X + 1, animal.Y, animal.Z, animal.M));
			Console.WriteLine("WORLDEVENTS_ANIMAL: uid=" + animal.Uid);
		}

		/// <summary>#1289 - festivals on the server, after the client's food and catch.</summary>
		[SteamFunction]
		public static void TestWorldEventsFestivalChain(Character self) {
			Player player = (Player) self;
			WorldEventSettings s = WorldEventSettings.instance;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			int chance = s.startChance;
			try {
				Point4D tirith = WorldEvents.CentreOf("Minas_Tirith");
				FestivalKind midsummer = (FestivalKind) WorldEvents.KindOf("slunovrat"), yule = (FestivalKind) WorldEvents.KindOf("yule"), eye = (FestivalKind) WorldEvents.KindOf("den_oka");
				Festival feast = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as Festival : null;
				ItemDef cake = (ItemDef) ThingDef.GetByDefname(midsummer.FoodDef);

				//the food the client asked for: one each
				bool food = (feast != null) && feast.Fed.Contains(player) && (Errands.CountIn(player.Backpack, cake) == 1);
				if (feast != null) {
					food &= !feast.Feed(player) && (Errands.CountIn(player.Backpack, cake) == 1);
				}
				Report("festivalFood", food);

				//the game: the client's catch (a GM's: no prize), then a player's; not from afar; a killed one isn't caught; the last round
				account.PLevel = 1;
				bool game = (feast != null) && (feast.Rounds == 1) && (feast.Animal != null) && (feast.Catches.Count == 0);
				long gold = player.Gold;
				if (game) {
					E_FestivalAnimal click = SingletonScript<E_FestivalAnimal>.Instance;
					NPC animal = feast.Animal;
					NPCAiTimer.Freeze(animal, 60);
					Point4D at = new Point4D(animal.X, animal.Y, animal.Z, animal.M); //kept: a broken reach rule would catch it from afar
					player.Go(new Point4D(at.X + 3, at.Y, at.Z, at.M));
					click.On_DClick(animal, player);
					game &= feast.Rounds == 1;
					player.Go(new Point4D(at.X + 1, at.Y, at.Z, at.M));
					click.On_DClick(animal, player);
					game &= (feast.Rounds == 2) && animal.IsDeleted && (feast.Catches[player] == 1) && (player.Gold - gold == s.festivalPrizeGold)
						&& (feast.Animal != null) && (feast.Animal != animal);
					feast.Animal.CauseDeath(null);
					WorldEvents.Tick();
					game &= (feast.Rounds == 3) && (feast.Animal != null) && (feast.Catches[player] == 1);
					feast.Rounds = s.festivalRounds - 1;
					NPC last = feast.Animal;
					player.Go(new Point4D(last.X + 1, last.Y, last.Z, last.M));
					feast.Catch(player);
					game &= (feast.Animal == null) && (feast.Rounds == s.festivalRounds);
				}
				Report("festivalGame", game);

				//cheaper while it lasts; at the end, the guests get a little experience
				bool cheaper = WorldEvents.PricePercent("Minas_Tirith") == -s.festivalDiscount;
				int levelWas = player.Level;
				player.Level = 100;
				player.Experience = 0;
				if (feast != null) {
					feast.EndsAt = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				bool guestPaid = player.Experience == s.festivalExp;
				player.Level = levelWas;
				player.Experience = 0;
				Report("festivalEnds", cheaper && guestPaid && (feast != null) && feast.Ended && feast.Won && feast.Host.IsDeleted
					&& CityNews.Of("Minas_Tirith").Exists(t => t.Contains("skoncila")));

				//the calendar: the days (Yule over the new year), once a day per place, and festivals don't hold up other events
				bool calendar = midsummer.InWindow(new DateTime(2026, 6, 20)) && midsummer.InWindow(new DateTime(2026, 6, 26)) && !midsummer.InWindow(new DateTime(2026, 6, 27))
					&& yule.InWindow(new DateTime(2026, 12, 31)) && yule.InWindow(new DateTime(2027, 1, 2)) && !yule.InWindow(new DateTime(2027, 1, 3)) && !yule.InWindow(new DateTime(2026, 12, 19))
					&& eye.IsFor("Barad-dur") && !eye.IsFor("Edoras") && midsummer.StartsByCalendar && !midsummer.CountsTowardLimit;
				WorldEvents.ResetForTests();
				GoTo(player, tirith);
				calendar &= (WorldEvents.TryStartByCalendar() == 1) && WorldEvents.HeldToday(midsummer, "Minas_Tirith");
				foreach (WorldEvent e in new List<WorldEvent>(WorldEvents.Running)) {
					WorldEvents.Stop(e);
				}
				calendar &= WorldEvents.TryStartByCalendar() == 0; //held today
				Errands.NowOverride = Midsummer.AddDays(1);
				calendar &= WorldEvents.TryStartByCalendar() == 1; //the next day
				Errands.NowOverride = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
				WorldEvents.ResetForTests();
				calendar &= WorldEvents.TryStartByCalendar() == 0; //no festival's day
				Errands.NowOverride = Midsummer;
				WorldEvents.Start(midsummer, "Minas_Tirith");
				GoTo(player, WorldEvents.CentreOf("Hobitin"));
				s.startChance = 100;
				WorldEvents.autoStartOnTestServer = true;
				WorldEvents.ClearScheduleForTests();
				VenueClock.Override = 12;
				WorldEvents.Tick();
				VenueClock.Override = null;
				calendar &= new List<WorldEvent>(WorldEvents.Running).Exists(e => !e.Kind.StartsByCalendar && (e.City == "Hobitin")); //the feast didn't fill the cap
				Report("festivalCalendar", calendar);

				//Mordor's day of the Eye: an orc host, a rat; a Gondor player catching it wins nothing
				WorldEvents.ResetForTests();
				Errands.NowOverride = new DateTime(2026, 3, 22, 12, 0, 0, DateTimeKind.Utc);
				Festival eyeDay = WorldEvents.Start(eye, "Barad-dur") as Festival;
				gold = player.Gold;
				if ((eyeDay != null) && (eyeDay.Animal != null)) {
					NPC rat = eyeDay.Animal;
					player.Go(new Point4D(rat.X + 1, rat.Y, rat.Z, rat.M));
					eyeDay.Catch(player);
				}
				Report("festivalMordor", (eyeDay != null) && (eyeDay.Host.Def == ThingDef.GetByDefname(FestivalKind.OrcHostDef)) && (eyeDay.Rounds == 1)
					&& (eyeDay.Catches.Count == 0) && (player.Gold == gold) && eye.IsTime());
			} finally {
				Errands.NowOverride = null;
				VenueClock.Override = null;
				WorldEvents.autoStartOnTestServer = false;
				s.startChance = chance;
				account.PLevel = plevel;
				WorldEvents.ResetForTests();
			}
			Console.WriteLine("WORLDEVENTS_FESTIVAL_DONE");
		}

		/// <summary>#1286 - the corsairs, the beacons (the client lit the first) and the troll.</summary>
		[SteamFunction]
		public static void TestWorldEventsOutdoorChain(Character self) {
			Player player = (Player) self;
			WorldEventSettings s = WorldEventSettings.instance;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				Point4D edoras = WorldEvents.CentreOf("Edoras");
				WorldEventKind corsairKind = WorldEvents.KindOf("korzari"), beaconsKind = WorldEvents.KindOf("majaky"), trollKind = WorldEvents.KindOf("trol");

				//the beacons the client lit: one by the GM (no lighter), the rest in order by a player; out of order refused; the realm's bonus
				Beacons beacons = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as Beacons : null;
				bool chain = (beacons != null) && (beacons.Items.Count == s.beaconCount) && (beacons.Lit == 1) && (beacons.Lighters.Count == 0)
					&& (beacons.Items[0].Model == Beacons.LitModel) && (beacons.Items[1].Model == Beacons.UnlitModel);
				if (beacons != null) {
					int[] previous = { beacons.Centre.X, beacons.Centre.Y };
					for (int i = 0; i < beacons.Items.Count; i++) {
						int[] here = { beacons.Items[i].X, beacons.Items[i].Y };
						chain &= WorldSpots.Distance(here, previous[0], previous[1]) >= s.beaconSpacing / 2; //each half a spacing on, at least
						previous = here;
					}
					Item last = beacons.Items[beacons.Items.Count - 1];
					chain &= WorldSpots.Distance(new[] { last.X, last.Y }, beacons.Centre.X, beacons.Centre.Y) >= s.beaconSpacing; //the chain leads away
					account.PLevel = 1;
					//the double click: not from afar, not by Mordor, not by a ghost
					E_EventBeacon click = SingletonScript<E_EventBeacon>.Instance;
					Item second = beacons.Items[1];
					player.Go(new Point4D(second.X + E_EventBeacon.Reach + 2, second.Y, second.Z, second.M));
					click.On_DClick(second, player);
					player.Go(new Point4D(second.X + 1, second.Y, second.Z, second.M));
					player.Realm = Realm.Mordor;
					click.On_DClick(second, player);
					player.Realm = Realm.Gondor;
					chain &= (beacons.Lit == 1);
					click.On_DClick(second, player);
					chain &= (beacons.Lit == 2) && beacons.Lighters.Contains(player);
					chain &= !beacons.Light(beacons.Items[3], player) && (beacons.Lit == 2);
					for (int i = 2; i < beacons.Items.Count; i++) {
						beacons.Light(beacons.Items[i], player);
					}
				}
				long gold = player.Gold;
				WorldEvents.Tick();
				int levelWas = player.Level;
				player.Level = 100; //a gain of 110 can't level up here
				player.Experience = 0;
				LevelingSystem.AddExperience(player, 100);
				bool bonusPaid = player.Experience == 100 + (100 * s.beaconBonusPercent / 100);
				player.Experience = 500;
				LevelingSystem.AddExperience(player, -100);
				bonusPaid &= player.Experience == 400; //a loss isn't raised
				player.Level = levelWas;
				player.Experience = 0;
				Report("beaconsLit", chain && beacons.Ended && beacons.Won && (player.Gold - gold == s.beaconGold) && bonusPaid
					&& (WorldEvents.ExperienceBonusPercent(Realm.Gondor) == s.beaconBonusPercent) && (WorldEvents.ExperienceBonusPercent(Realm.Mordor) == 0)
					&& beacons.Items.TrueForAll(b => b.IsDeleted) && CityNews.Of("Minas_Tirith").Exists(t => t.Contains("zaplaly az")));

				WorldEvents.ResetForTests();
				WorldEvent late = WorldEvents.Start(beaconsKind, "Minas_Tirith");
				if (late != null) {
					late.EndsAt = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				Report("beaconsLost", (late != null) && late.Ended && !late.Won && late.Items.TrueForAll(b => b.IsDeleted)
					&& (WorldEvents.ExperienceBonusPercent(Realm.Gondor) == 0) && CityNews.Of("Minas_Tirith").Exists(t => t.Contains("nezaplaly")));

				//corsairs land at Pelargir (Dol Amroth's), a captain first; not beaten, the harbour closes
				WorldEvents.ResetForTests();
				GoTo(player, edoras);
				Point4D pelargir = SteamEngine.Regions.StaticRegion.GetByDefname("a_Pelargir").P; //Pelargir's own spawn point, not the kind's say
				Warband corsairs = WorldEvents.Start(corsairKind, CorsairKind.City) as Warband;
				bool landed = (corsairs != null) && (corsairs.Npcs.Count == ((WarbandKind) corsairKind).SizeFor(0)) && (Point2D.GetSimpleDistance(corsairs.Centre, pelargir) <= 4)
					&& (corsairs.Npcs[0].Def == ThingDef.GetByDefname(CorsairKind.CaptainDef)) && corsairs.Describe().Contains("Pelargir")
					&& corsairKind.IsFor("Dol_Amroth") && !corsairKind.IsFor("Umbar") && (corsairKind.Places.Count == 1)
					&& (NPCAiTimer.GetFor(corsairs.Npcs[1]).homeX == corsairs.Centre.X);
				if (corsairs != null) {
					corsairs.EndsAt = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				Report("corsairs", landed && corsairs.Ended && !corsairs.Won && (WorldEvents.PricePercent(CorsairKind.City) == s.corsairLostPercent)
					&& CityNews.Of(CorsairKind.City).Exists(t => t.Contains("zavreny")));

				//the troll: at night, on Gondor's roads; still fought at sunrise it turns to stone, which stays a while
				WorldEvents.ResetForTests();
				VenueClock.Override = 23;
				bool night = trollKind.IsTime();
				VenueClock.Override = 12;
				bool noon = trollKind.IsTime();
				Troll troll = WorldEvents.Start(trollKind, "Edoras") as Troll;
				bool stands = night && !noon && trollKind.IsFor("Hobitin") && !trollKind.IsFor("Harad") && (troll != null) && (troll.Beast != null)
					&& (Point2D.GetSimpleDistance(troll.Beast, edoras) >= Troll.MinDistance);
				gold = player.Gold;
				if (troll != null) {
					GoTo(player, new Point4D(troll.Beast.X + 1, troll.Beast.Y, troll.Beast.Z, troll.Beast.M));
					FightMemory.Record(troll.Beast, player);
				}
				VenueClock.Override = 7;
				WorldEvents.Tick();
				NPC stone = (troll == null) ? null : troll.Stone;
				bool turned = stands && troll.Ended && troll.Won && troll.Beast.IsDeleted && (stone != null) && !stone.IsDeleted && stone.Flag_Invulnerable
					&& NPCAiTimer.IsFrozen(stone) && !troll.Npcs.Contains(stone) && (stone.GetTag(WorldEvent.EventTK) == null)
					&& (stone.Def == ThingDef.GetByDefname(TrollKind.StoneDef)) && (player.Gold - gold == s.trollGold) && CityNews.Of("Edoras").Exists(t => t.Contains("kamenny"));
				//its time up, a landmark goes; after a restart (landmarks forgotten), the stone goes with the leftovers
				NPC marker = (NPC) ((NPCDef) ThingDef.GetByDefname(TrollKind.StoneDef)).Create(edoras.X + 3, edoras.Y, edoras.Z, edoras.M);
				WorldEvents.AddLandmark(marker, 1);
				marker.SetTag(WorldEvents.StoneUntilTK, Globals.TimeInSeconds - 1);
				WorldEvents.Tick();
				bool expired = marker.IsDeleted && (stone != null) && !stone.IsDeleted;
				WorldEvents.ForgetLandmarksForTests();
				WorldEvents.PretendBootForTests();
				WorldEvents.Tick();
				Report("trollStone", turned && expired && stone.IsDeleted);

				//a GM's start in the morning waits for the next sunrise; a GM taking the troll away tells nothing
				WorldEvents.ResetForTests();
				CityNews.Clear();
				VenueClock.Override = 7;
				Troll early = WorldEvents.Start(trollKind, "Edoras") as Troll;
				WorldEvents.Tick();
				bool waits = (early != null) && !early.Ended;
				VenueClock.Override = 23;
				WorldEvents.Tick();
				VenueClock.Override = 7;
				WorldEvents.Tick();
				waits &= early.Ended && !early.Won;
				WorldEvents.ResetForTests();
				CityNews.Clear();
				VenueClock.Override = 23;
				Troll taken = WorldEvents.Start(trollKind, "Edoras") as Troll;
				if (taken != null) {
					taken.Beast.Delete();
				}
				WorldEvents.Tick();
				Report("trollDawnAndRemoval", waits && (taken != null) && taken.Ended && !taken.Won && (CityNews.Of("Edoras").Count == 1));

				//nobody fought it: it walks off at dawn, no stone, no pay
				WorldEvents.ResetForTests();
				GoTo(player, edoras);
				VenueClock.Override = 23;
				Troll alone = WorldEvents.Start(trollKind, "Hobitin") as Troll;
				gold = player.Gold;
				VenueClock.Override = 7;
				WorldEvents.Tick();
				Report("trollLeft", (alone != null) && alone.Ended && !alone.Won && (alone.Stone == null) && (player.Gold == gold)
					&& CityNews.Of("Hobitin").Exists(t => t.Contains("odesel")));

				//killed before dawn: its killer is paid
				WorldEvents.ResetForTests();
				VenueClock.Override = 23;
				Troll slain = WorldEvents.Start(trollKind, "Edoras") as Troll;
				gold = player.Gold;
				if (slain != null) {
					KillAll(slain, player, null);
				}
				WorldEvents.Tick();
				Report("trollKilled", (slain != null) && slain.Ended && slain.Won && slain.Heroes.ContainsKey(player) && (player.Gold - gold == s.trollGold));
			} finally {
				VenueClock.Override = null;
				account.PLevel = plevel;
				WorldEvents.ResetForTests();
			}
			Console.WriteLine("WORLDEVENTS_OUTDOOR_DONE");
		}

		[SteamFunction]
		public static void TestWorldEventsChain(Character self) {
			Player player = (Player) self;
			WorldEventSettings s = WorldEventSettings.instance;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			bool enabled = s.enabled;
			int chance = s.startChance;
			try {
				WorldEventKind raidKind = WorldEvents.KindOf("najezd"), caravanKind = WorldEvents.KindOf("karavana");
				Func<int, int> raidSize = ((WarbandKind) raidKind).SizeFor;

				//the rules: a raid only on Gondor's cities and at dusk, its size by the players there
				VenueClock.Override = 18;
				bool dusk = raidKind.IsTime();
				VenueClock.Override = 12;
				bool noon = raidKind.IsTime();
				VenueClock.Override = null;
				Report("raidRules", raidKind.IsFor("Edoras") && raidKind.IsFor("Hobitin") && !raidKind.IsFor("Barad-dur") && !raidKind.IsFor("Harad")
					&& dusk && !noon && (raidSize(0) == s.raidBase) && (raidSize(2) == s.raidBase + (2 * s.raidPerPlayer))
					&& (raidSize(100) == s.raidMax) && caravanKind.IsFor("Umbar") && caravanKind.IsFor("Barad-dur")
					&& (Caravan.BanditsFor(1) == s.banditBase + s.banditPerPlayer));

				//the raid the client's .udalost started: a GM isn't counted, so the smallest warband, a chief first, making for the centre
				Point4D edoras = WorldEvents.CentreOf("Edoras");
				Warband raid = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as Warband : null;
				bool started = (raid != null) && (raid.City == "Edoras") && (raid.Npcs.Count == raidSize(0))
					&& (raid.Npcs[0].Def == ThingDef.GetByDefname(OrcRaidKind.ChiefDef));
				if (raid != null) {
					foreach (NPC raider in raid.Npcs) {
						NPCAiTimer ai = NPCAiTimer.GetFor(raider);
						started &= (ai != null) && ai.NeverSleeps && (ai.homeX == edoras.X) && (ai.homeY == edoras.Y);
					}
				}
				Report("raidStarted", started);

				//beaten: the defender is paid for the raid and each kill, Edoras feasts and the criers tell it
				account.PLevel = 1; //a player: counted near the city, paid like one
				long gold = player.Gold;
				int standing = Reputation.Of(player, "Edoras");
				if (raid != null) {
					KillAll(raid, player, null);
				}
				WorldEvents.Tick();
				bool won = (raid != null) && raid.Ended && raid.Won && (WorldEvents.Running.Count == 0) && raid.Heroes.ContainsKey(player)
					&& (raid.Heroes[player] == raidSize(0));
				Report("raidWon", won && (player.Gold - gold == s.raidGold + (s.raidKillGold * raidSize(0)))
					&& (Reputation.Of(player, "Edoras") > standing) && (WorldEvents.PricePercent("Edoras") == s.raidWonPercent)
					&& CityNews.Of("Edoras").Exists(t => t.Contains("odrazili")) && WorldEvents.RecentOutcomes().Exists(t => t.Contains("odrazili")));

				//lost: time runs out, the raiders leave and Minas Tirith's prices rise (through Reputation.PriceFor)
				WorldEvents.ResetForTests();
				GoTo(player, WorldEvents.CentreOf("Minas_Tirith"));
				long before = Reputation.PriceFor(player, 1000);
				WorldEvent lost = WorldEvents.Start(raidKind, "Minas_Tirith");
				bool lostStarted = (lost != null) && (lost.Npcs.Count == raidSize(1));
				//nobody of the enemy realm, and no GM, counts as the city's or is credited for a raider
				bool noStrangers = lost != null;
				if (lost != null) {
					player.Realm = Realm.Mordor;
					noStrangers &= !WorldEvents.PlayersNear("Minas_Tirith").Contains(player);
					KillAll(lost, player, n => n == lost.Npcs[1]);
					player.Realm = Realm.Gondor;
					account.PLevel = 4;
					noStrangers &= !WorldEvents.PlayersNear("Minas_Tirith").Contains(player);
					KillAll(lost, player, n => n == lost.Npcs[2]);
					account.PLevel = 1;
					noStrangers &= (lost.Heroes.Count == 0) && WorldEvents.PlayersNear("Minas_Tirith").Contains(player);
					lost.EndsAt = Globals.TimeInSeconds;
				}
				Report("noStrangersCredited", noStrangers);
				WorldEvents.Tick();
				Report("raidLost", lostStarted && lost.Ended && !lost.Won && lost.Npcs.TrueForAll(n => n.IsDeleted)
					&& (WorldEvents.PricePercent("Minas_Tirith") == s.raidLostPercent)
					&& (Reputation.PriceFor(player, 1000) == (before * (100 + s.raidLostPercent) + 50) / 100)
					&& CityNews.Of("Minas_Tirith").Exists(t => t.Contains("vyplenili")));

				//a GM's start refuses a city the kind isn't for, and a second event in a busy city
				Report("startRefused", (WorldEvents.Start(raidKind, "Harad") == null) && (WorldEvents.Start(caravanKind, "Minas_Tirith") != null)
					&& (WorldEvents.Start(raidKind, "Minas_Tirith") == null));
				WorldEvents.ResetForTests();

				//the caravan: merchant and pack animals on the road, waypoint by waypoint; bandits at halfway, the merchant waits out the
				//fight, arrives, escorts paid
				GoTo(player, edoras);
				Caravan caravan = WorldEvents.Start(caravanKind, "Edoras") as Caravan;
				bool onRoad = (caravan != null) && (caravan.Merchant != null) && (caravan.Merchant.PetGoalPoint != null)
					&& (caravan.Npcs.FindAll(n => n.Def == ThingDef.GetByDefname(CaravanKind.PackDef)).TrueForAll(n => n.FollowTarget == caravan.Merchant))
					&& (caravan.Npcs.Count == 1 + s.caravanPack) && (Point2D.GetSimpleDistance(caravan.Merchant, edoras) > Caravan.ArrivedWithin);
				bool ambushed = false, waited = false, walked = false, packNoDeed = false;
				if (caravan != null) {
					GoTo(player, WorldEvents.CentreOf("Minas_Tirith")); //far from the road: no escort
					List<int> path = new List<int>();
					for (int i = 0; !caravan.Ambushed && (i < 40); i++) {
						int[] w = caravan.Waypoint;
						caravan.Merchant.Go(new Point4D(w[0], w[1], w[2], edoras.M));
						path.Add(caravan.next);
						WorldEvents.Tick();
					}
					walked = caravan.Ambushed && (path.Count >= 2) && (path[0] < caravan.ambushAt) && (path[path.Count - 1] >= caravan.ambushAt)
						&& (path[path.Count - 2] < caravan.ambushAt) && (caravan.Escorts.Count == 0);
					List<NPC> bandits = caravan.Npcs.FindAll(n => n.Def == ThingDef.GetByDefname(CaravanKind.BanditDef));
					ambushed = (bandits.Count == Caravan.BanditsFor(1)) && bandits.TrueForAll(b => NPCAiTimer.GetFor(b).Target == caravan.Merchant);
					foreach (NPC bandit in bandits) {
						NPCAiTimer.Freeze(bandit, 60); //the test's merchant mustn't die while the check runs
					}
					int[] waypoint = caravan.Waypoint;
					caravan.Merchant.Go(new Point4D(waypoint[0], waypoint[1], waypoint[2], edoras.M)); //at his waypoint: only the fight holds him
					int next = caravan.next;
					WorldEvents.Tick();
					waited = (caravan.next == next) && caravan.Describe().Contains("prepadli");
					KillAll(caravan, player, n => n.Def == ThingDef.GetByDefname(CaravanKind.PackDef)); //a pack horse's killer is no hero
					packNoDeed = !caravan.Heroes.ContainsKey(player);
					KillAll(caravan, player, n => n.Def == ThingDef.GetByDefname(CaravanKind.BanditDef));
				}
				Report("caravanAmbush", onRoad && walked && ambushed && waited && packNoDeed);
				gold = player.Gold;
				if (caravan != null) {
					GoTo(player, edoras);
					caravan.Merchant.Go(edoras);
				}
				WorldEvents.Tick();
				Report("caravanArrived", (caravan != null) && caravan.Ended && caravan.Won && (player.Gold - gold == s.caravanGold)
					&& (WorldEvents.PricePercent("Edoras") == s.caravanPercent) && CityNews.Of("Edoras").Exists(t => t.Contains("dorazila")));

				//one who only waited in the square for it isn't paid
				WorldEvents.ResetForTests();
				GoTo(player, WorldEvents.CentreOf("Minas_Tirith"));
				Caravan waitedFor = WorldEvents.Start(caravanKind, "Edoras") as Caravan;
				gold = player.Gold;
				if (waitedFor != null) {
					waitedFor.Ambush();
					foreach (NPC bandit in waitedFor.Npcs.FindAll(n => n.Def == ThingDef.GetByDefname(CaravanKind.BanditDef))) {
						bandit.CauseDeath(null);
					}
					GoTo(player, edoras);
					waitedFor.Merchant.Go(edoras);
				}
				WorldEvents.Tick();
				Report("bystanderUnpaid", (waitedFor != null) && waitedFor.Won && (player.Gold == gold));

				//lost: the merchant dies, the caravan goes, nothing gets cheaper
				WorldEvents.ResetForTests();
				Caravan doomed = WorldEvents.Start(caravanKind, "Hobitin") as Caravan;
				if (doomed != null) {
					doomed.Merchant.CauseDeath(null);
				}
				WorldEvents.Tick();
				Report("caravanLost", (doomed != null) && doomed.Ended && !doomed.Won && doomed.Npcs.TrueForAll(n => n.IsDeleted)
					&& (WorldEvents.PricePercent("Hobitin") == 0) && CityNews.Of("Hobitin").Exists(t => t.Contains("ztratila")));

				//after a restart: the first tick clears an event's NPC with no event under way, and waits the gap before starting one
				WorldEvents.ResetForTests();
				NPC stray = (NPC) ((NPCDef) ThingDef.GetByDefname(OrcRaidKind.RaiderDef)).Create(edoras.X + 2, edoras.Y, edoras.Z, edoras.M);
				stray.SetTag(WorldEvent.EventTK, "najezd/Edoras/999999");
				GoTo(player, edoras);
				s.startChance = 100;
				WorldEvents.autoStartOnTestServer = true;
				WorldEvents.PretendBootForTests();
				WorldEvents.Tick();
				Report("leftoversCleared", stray.IsDeleted && (WorldEvents.Running.Count == 0));

				//the scheduler: a test server starts nothing unless a test allows it; off starts nothing; on, with the hour not dusk,
				//no raid but another event starts where a player is; one at a time
				WorldEvents.ClearScheduleForTests();
				WorldEvents.autoStartOnTestServer = false;
				WorldEvents.Tick();
				bool testServerQuiet = WorldEvents.Running.Count == 0;
				WorldEvents.autoStartOnTestServer = true;
				WorldEvents.ClearScheduleForTests();
				s.enabled = false;
				WorldEvents.Tick();
				bool offQuiet = WorldEvents.Running.Count == 0;
				s.enabled = true;
				WorldEvents.ClearScheduleForTests();
				VenueClock.Override = 12;
				WorldEvents.Tick();
				bool onStarts = (WorldEvents.Running.Count == 1) && (WorldEvents.Running[0].Kind != raidKind) && !WorldEvents.Running[0].Kind.StartsByCalendar
					&& (WorldEvents.Running[0].City == "Edoras"); //a caravan, fire or fever where the player is; no raid at noon
				WorldEvents.ClearScheduleForTests();
				WorldEvents.Tick();
				VenueClock.Override = null;
				bool oneAtATime = WorldEvents.Running.Count == 1;
				Report("scheduler", testServerQuiet && offQuiet && onStarts && oneAtATime);
				WorldEvents.ResetForTests();
			} finally {
				WorldEvents.autoStartOnTestServer = false;
				VenueClock.Override = null;
				s.enabled = enabled;
				s.startChance = chance;
				account.PLevel = plevel;
				WorldEvents.ResetForTests();
			}
			Console.WriteLine("WORLDEVENTS_CHAIN_DONE");
		}
	}
}
