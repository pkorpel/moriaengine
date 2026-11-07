using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1256 - the bath house, the theatre and the pits checked on the server; Tests/Protocol/VenueLifeTests.cs drives these
	/// and opens the attendant's dialog through a client. Edoras gets one of each, in the evening.</summary>
	public static class VenueLifeTests {
		private static readonly string[] types = { "bathhouse", "theatre", "pits" };

		private static void Report(string name, bool ok) {
			Console.WriteLine("VENUELIFE_CHECK: " + name + "=" + ok);
		}

		private static CityVenue Venue(Player player, string type) {
			return (CityVenue) player.GetTag(TagKey.Acquire("_venuelife_" + type + "_"));
		}

		private static Item Gem(Player player, string type, string role) {
			return Theatre.GemOf(Venue(player, type), role);
		}

		private static NPC Body(Player player, string type, string role) {
			return Residents.BodyOf(Gem(player, type, role));
		}

		private static void Inside(CityVenue venue, Item gem) {
			int[] spot = ResidentSchedule.SpotIn(venue, Residents.IdOf(gem));
			Residents.BodyOf(gem).Go(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], venue.M));
		}

		private static void Near(Player player, NPC npc) {
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		private static List<string> Offers(Player player, string type, string role) {
			Item gem = Gem(player, type, role);
			List<string> keys = new List<string>();
			foreach (VenueAction action in VenueActions.For(player, Residents.BodyOf(gem), gem)) {
				keys.Add(action.Key);
			}
			return keys;
		}

		private static void Do(Player player, string key, string type, string role) {
			Item gem = Gem(player, type, role);
			VenueActions.Get(key).Do(player, Residents.BodyOf(gem), gem);
		}

		[SteamFunction]
		public static void TestVenueLifeSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				VenueClock.Override = 19;
				Theatre.StepByHand = true;
				List<FoundRoom> rooms = RoomScan.Candidates("Edoras", player.MovementSettings);
				for (int i = 0; i < types.Length; i++) {
					VenueDraft draft = VenueDraft.OfRoom(rooms[i], rooms[i].M, "Edoras");
					draft.SetType(VenueType.Get(types[i]).IndexOf());
					string problem;
					CityVenue venue = draft.Save(out problem);
					if (venue == null) {
						throw new SEException("venue " + types[i] + " wasn't saved: " + problem);
					}
					Residents.FillRoles(venue);
					foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
						NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
						Inside(venue, gem);
					}
					player.SetTag(TagKey.Acquire("_venuelife_" + types[i] + "_"), venue);
				}
				NPC attendant = Body(player, "bathhouse", "attendant");
				Near(player, attendant);
				Console.WriteLine("VENUELIFE_SETUP: attendant=" + attendant.Uid + " bathe=" + VenueActions.ButtonOf(VenueActions.Get("bath_bathe")));
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("VENUELIFE_SETUP_DONE");
			}
		}

		[SteamFunction]
		public static void TestVenueLifeAfterClient(Character self) {
			Player player = (Player) self;
			try {
				Report("theClientsBathMakesFresh", Bathhouse.IsFresh(player));
				player.RemoveTag(Bathhouse.FreshUntilTK);
			} finally {
				Console.WriteLine("VENUELIFE_CLIENT_DONE");
			}
		}

		[SteamFunction]
		public static void TestVenueLifeRules(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			int betsPerHour = VenueLifeSettings.instance.pitBetsPerHour;
			try {
				player.Account.PLevel = 1;
				GoldUtils.Grant(player, 20000);
				Bath(player);
				Play(player);
				Templates(player);
				PitFights(player);
			} finally {
				player.Account.PLevel = plevel;
				VenueLifeSettings.instance.pitBetsPerHour = betsPerHour;
				Errands.NowOverride = null;
				Theatre.PlayOverride = null;
				Pits.RollOverride.Clear();
				VenueClock.Override = 19;
				Console.WriteLine("VENUELIFE_RULES_DONE");
			}
		}

		private static void Bath(Player player) {
			List<string> attendant = Offers(player, "bathhouse", "attendant");
			Report("theBathHouseOffers", attendant.Contains("bath_bathe") && attendant.Contains("bath_rumour") && Offers(player, "bathhouse", "stoker").Contains("bath_wood") &&
				!Offers(player, "bathhouse", "bather").Contains("bath_bathe") && Offers(player, "bathhouse", "bather").Contains("bath_rumour"));

			long before = GoldUtils.CountGoldOn(player);
			Do(player, "bath_bathe", "bathhouse", "attendant");
			long afterBath = GoldUtils.CountGoldOn(player);
			Do(player, "bath_bathe", "bathhouse", "attendant");
			Report("aBathCostsAndFreshens", (before - afterBath == VenueLifeSettings.instance.bathPrice) && Bathhouse.IsFresh(player) && (GoldUtils.CountGoldOn(player) == afterBath));
			bool shown = ActiveEffects.Of(player).Exists(e => e.Name == "Svezest");
			Near(player, Body(player, "bathhouse", "attendant"));
			long freshPrice = Reputation.PriceFor(player, 1000);
			int friendWhenFresh = Bathhouse.FriendshipBonus(player);
			player.SetTag(Bathhouse.FreshUntilTK, Globals.TimeInSeconds - 1);
			long plainPrice = Reputation.PriceFor(player, 1000);
			Report("freshnessShowsOnBoosts", shown && !ActiveEffects.Of(player).Exists(e => e.Name == "Svezest"));
			Report("freshShopsAreCheaper", (freshPrice == Math.Max(1, (plainPrice * (100 - VenueLifeSettings.instance.freshDiscount) + 50) / 100)) && (freshPrice < plainPrice));
			Report("freshPeopleAreWarmer", (friendWhenFresh == 1) && (Bathhouse.FriendshipBonus(player) == 0));

			//the stoker: logs for wages, a cap a real day, a friend for the first load of the day
			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			ItemDef log = (ItemDef) ThingDef.GetByDefname("i_log");
			string stokerId = Residents.IdOf(Gem(player, "bathhouse", "stoker"));
			int friend0 = Residents.Friendship(player, stokerId);
			Item logs = (Item) log.Create(player.Backpack);
			logs.Amount = 5;
			long g0 = GoldUtils.CountGoldOn(player);
			Do(player, "bath_wood", "bathhouse", "stoker");
			bool first = (Errands.CountIn(player.Backpack, log) == 0) && (GoldUtils.CountGoldOn(player) - g0 == 5 * VenueLifeSettings.instance.woodWage) &&
				(Residents.Friendship(player, stokerId) == Math.Min(Residents.MaxFriendship, friend0 + 1));
			Item more = (Item) log.Create(player.Backpack);
			more.Amount = 40;
			Do(player, "bath_wood", "bathhouse", "stoker");
			int left = Errands.CountIn(player.Backpack, log);
			bool capped = (left == 40 - (VenueLifeSettings.instance.woodPerDay - 5)) && (Residents.Friendship(player, stokerId) == Math.Min(Residents.MaxFriendship, friend0 + 1));
			Do(player, "bath_wood", "bathhouse", "stoker");
			bool full = Errands.CountIn(player.Backpack, log) == left;
			Errands.NowOverride = start.AddHours(25);
			Do(player, "bath_wood", "bathhouse", "stoker");
			Report("theStokerPaysForLogsUpToADay", first && capped && full && (Errands.CountIn(player.Backpack, log) == 0));
			Errands.NowOverride = null;
		}

		private static void Play(Player player) {
			CityVenue theatre = Venue(player, "theatre");
			NPC director = Body(player, "theatre", "director");
			Near(player, director);
			List<string> dir = Offers(player, "theatre", "director");
			Report("theDirectorTellsTheHourAndTakesExtras", dir.Contains("theatre_when") && dir.Contains("theatre_extra") && Offers(player, "theatre", "spectator").Contains("theatre_when") &&
				!Offers(player, "theatre", "prompter").Contains("theatre_prompt"));

			//an extra needs Musicianship
			player.SetRealSkillValue((int) SkillName.Musicianship, 0);
			Do(player, "theatre_extra", "theatre", "director");
			bool refused = !Theatre.IsExtra(player, theatre);
			player.SetRealSkillValue((int) SkillName.Musicianship, 500);
			Do(player, "theatre_extra", "theatre", "director");
			Report("anExtraNeedsMusicianship", refused && Theatre.IsExtra(player, theatre) && !Offers(player, "theatre", "director").Contains("theatre_extra"));

			//play 1: a wrong prompt
			theatre.RemoveTag(Theatre.LastPlayTK);
			Theatre.PlayOverride = 0;
			TheatrePlayTimer play = Theatre.StartPlay(theatre);
			for (int i = 0; i < 6; i++) { //lines 0-4, then the forgotten line 5 opens the gap
				play.Step(director);
			}
			bool gap = Theatre.WaitsForPrompt(theatre) && Offers(player, "theatre", "prompter").Contains("theatre_prompt");
			NPC prompter = Body(player, "theatre", "prompter");
			string prompterId = Residents.IdOf(Gem(player, "theatre", "prompter"));
			long g0 = GoldUtils.CountGoldOn(player);
			int f0 = Residents.Friendship(player, prompterId);
			bool wrong = Theatre.Prompt(player, prompter, 0) && !Theatre.WaitsForPrompt(theatre) && (GoldUtils.CountGoldOn(player) == g0) &&
				(Residents.Friendship(player, prompterId) == f0) && (play.line == 6);
			Report("theForgottenLineWaitsForAPrompt", gap);
			Report("aWrongPromptGetsLaughs", wrong && !Theatre.Prompt(player, prompter, 1));
			long beforeEnd = GoldUtils.CountGoldOn(player);
			int steps = 0;
			while (play.Step(director) && (steps < 20)) {
				steps++;
			}
			Report("theExtraIsPaidAtTheEnd", (GoldUtils.CountGoldOn(player) - beforeEnd == VenueLifeSettings.instance.extraWage) && !Theatre.IsExtra(player, theatre));
			director.DeleteTimer(TheatrePlayTimer.Key);

			//play 2: the right prompt pays
			theatre.RemoveTag(Theatre.LastPlayTK);
			Theatre.PlayOverride = 1;
			play = Theatre.StartPlay(theatre);
			for (int i = 0; i < 5; i++) { //lines 0-3, then the forgotten line 4
				play.Step(director);
			}
			g0 = GoldUtils.CountGoldOn(player);
			bool right = Theatre.Prompt(player, prompter, 2) && (GoldUtils.CountGoldOn(player) - g0 == VenueLifeSettings.instance.promptReward) &&
				(Residents.Friendship(player, prompterId) == Math.Min(Residents.MaxFriendship, f0 + 1));
			Report("theRightPromptPays", right);
			director.DeleteTimer(TheatrePlayTimer.Key);

			//play 3: nobody prompts, the prompter whispers after promptLines steps
			theatre.RemoveTag(Theatre.LastPlayTK);
			Theatre.PlayOverride = 2;
			play = Theatre.StartPlay(theatre);
			for (int i = 0; i < 6; i++) {
				play.Step(director);
			}
			bool waiting = play.gapLeft > 0;
			for (int i = 0; i < VenueLifeSettings.instance.promptLines; i++) {
				play.Step(director);
			}
			Report("withoutAPromptThePrompterWhispers", waiting && (play.gapLeft == 0) && (play.line == 6));
			director.DeleteTimer(TheatrePlayTimer.Key);

			//dusk starts a play once, with a player near; noon doesn't
			theatre.RemoveTag(Theatre.LastPlayTK);
			VenueClock.Override = 12;
			Theatre.Tick();
			bool noonQuiet = Theatre.PlayIn(theatre) == null;
			VenueClock.Override = 19.5;
			Theatre.Tick();
			bool duskPlays = Theatre.PlayIn(theatre) != null;
			director.DeleteTimer(TheatrePlayTimer.Key);
			Theatre.Tick();
			Report("aPlayAtDuskOnce", noonQuiet && duskPlays && (Theatre.PlayIn(theatre) == null));
			VenueClock.Override = 19;
		}

		private static void FreshLog(Player player) {
			ErrandLog log = Errands.Log(player);
			foreach (Errand left in new List<Errand>(log.active)) {
				Errands.GiveUp(player, left);
			}
			log.day = -1;
			log.lastKey = "";
			log.streak = 0;
		}

		private static Errand Build(Player player, string key, Item giver) {
			ErrandTemplate template = ErrandTemplates.Get(key);
			Errand errand = new Errand { key = key, name = template.Name, giverId = Residents.IdOf(giver), city = "Edoras", tier = (int) template.Tier };
			return template.TryBuild(new ErrandContext(player, giver, "Edoras"), errand) ? errand : null;
		}

		private static void Templates(Player player) {
			Item director = Gem(player, "theatre", "director");
			CityVenue theatre = Venue(player, "theatre");
			Report("onlyTheDirectorAsks", (Build(player, "missingactor", Gem(player, "theatre", "actor")) == null) && (Build(player, "prop", Gem(player, "bathhouse", "attendant")) == null));

			//the missing actor, done twice: fresh from the bath it warms the director one point more
			int[] gained = new int[2];
			for (int round = 0; round < 2; round++) {
				FreshLog(player);
				if (round == 0) {
					player.SetTag(Bathhouse.FreshUntilTK, Globals.TimeInSeconds + 600);
				} else {
					player.RemoveTag(Bathhouse.FreshUntilTK);
				}
				Errand missing = Build(player, "missingactor", director);
				Errands.Accept(player, missing);
				Item actor = Residents.ById(missing.steps[0].residentId);
				int before = Residents.Friendship(player, Residents.IdOf(director));
				Near(player, Residents.BodyOf(actor));
				Errands.Do(player, missing, Residents.BodyOf(actor));
				Near(player, Residents.BodyOf(director));
				Errands.Do(player, missing, Residents.BodyOf(director));
				gained[round] = missing.IsDone ? Residents.Friendship(player, Residents.IdOf(director)) - before : -99;
				if (round == 0) {
					Report("theMissingActorIsATheatreActor", (Residents.RoleKeyOf(actor) == "actor") && (Residents.WorkplaceOf(actor) == theatre));
				}
			}
			Report("aFreshErrandWarmsTheGiverMore", (gained[0] == gained[1] + 1) && (gained[1] >= 1));

			FreshLog(player);
			Errand prop = Build(player, "prop", director);
			Errands.Accept(player, prop);
			Item placed = (prop == null) ? null : prop.steps[0].placed;
			Report("aForgottenPropLiesElsewhere", (placed != null) && (VenueRegistry.At(placed) != null) && (VenueRegistry.At(placed) != theatre));
			FreshLog(player);
		}

		private static void PitFights(Player player) {
			NPC pitmaster = Body(player, "pits", "pitmaster");
			Near(player, pitmaster);
			string[] names = Pits.Fighters(Venue(player, "pits"));
			Report("thePitsHaveTwoFighters", Offers(player, "pits", "pitmaster").Contains("pits_bet") && (names.Length == 2) && (names[0] != names[1]));
			int bet = VenueLifeSettings.instance.pitBetMedium;
			long[] gained = new long[2];
			for (int i = 0; i < 2; i++) {
				foreach (int r in new[] { 6, 1, 6, 1 }) { //fighter 0 hits 5 twice: fighter 1 falls in two rounds
					Pits.RollOverride.Enqueue(r);
				}
				long g = GoldUtils.CountGoldOn(player);
				Pits.Bet(player, pitmaster, i, bet);
				gained[i] = GoldUtils.CountGoldOn(player) - g;
			}
			Report("aWinningBetPaysDouble", (gained[0] == bet) && (gained[1] == -bet));
			VenueLifeSettings.instance.pitBetsPerHour = 3;
			bool third = Pits.Bet(player, pitmaster, 0, bet);
			long capped = GoldUtils.CountGoldOn(player);
			bool fourth = Pits.Bet(player, pitmaster, 0, bet);
			player.Account.PLevel = 7;
			bool gm = Pits.Bet(player, pitmaster, 0, bet);
			player.Account.PLevel = 1;
			Report("betsAreCappedAndGmsDontBet", third && !fourth && (GoldUtils.CountGoldOn(player) == capped) && !gm);
		}

		/// <summary>The real timers: at dusk, with the player near, the residents' scheduler starts a play on its own (within its
		/// 20 s tick), and the play runs to its end at a line a second.</summary>
		[SteamFunction]
		public static void TestVenueLifeRealPlay(Character self) {
			Player player = (Player) self;
			try {
				CityVenue theatre = Venue(player, "theatre");
				Theatre.StepByHand = false;
				VenueLifeSettings.instance.playLineSeconds = 1;
				VenueClock.Override = 19.5;
				theatre.RemoveTag(Theatre.LastPlayTK);
				Near(player, Body(player, "theatre", "director"));
				Report("noPlayYet", Theatre.PlayIn(theatre) == null);
			} finally {
				Console.WriteLine("VENUELIFE_REALPLAY_DONE");
			}
		}

		[SteamFunction]
		public static void TestVenueLifeRealPlayState(Character self) {
			Player player = (Player) self;
			try {
				CityVenue theatre = Venue(player, "theatre");
				bool started = theatre.GetTag(Theatre.LastPlayTK) != null;
				bool ended = started && (Theatre.PlayIn(theatre) == null);
				Console.WriteLine("VENUELIFE_PLAY: started=" + (started ? 1 : 0) + " ended=" + (ended ? 1 : 0));
				if (ended) {
					Report("theSchedulerStartedAPlayThatRanToItsEnd", true);
					VenueLifeSettings.instance.playLineSeconds = 5;
				}
			} finally {
				Console.WriteLine("VENUELIFE_REALPLAY_STATE");
			}
		}
	}
}
