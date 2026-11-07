using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1260 - the harbour, the stable, the market, the bakery and the barracks checked on the server;
	/// Tests/Protocol/TradeVenueTests.cs drives these and unloads a ship through a client. Edoras gets one of each.</summary>
	public static class TradeVenueTests {
		private static readonly string[] types = { "harbour", "stable", "market", "bakery", "barracks" };

		private static void Report(string name, bool ok) {
			Console.WriteLine("TRADEVENUE_CHECK: " + name + "=" + ok);
		}

		private static CityVenue Venue(Player player, string type) {
			return (CityVenue) player.GetTag(TagKey.Acquire("_tradevenue_" + type + "_"));
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
			Near(player, Residents.BodyOf(gem));
			VenueActions.Get(key).Do(player, Residents.BodyOf(gem), gem);
		}

		[SteamFunction]
		public static void TestTradeVenueSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				VenueClock.Override = 12;
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
					player.SetTag(TagKey.Acquire("_tradevenue_" + types[i] + "_"), venue);
				}
				player.Stam = 50;
				NPC master = Body(player, "harbour", "harbourmaster");
				Near(player, master);
				Console.WriteLine("TRADEVENUE_SETUP: master=" + master.Uid + " unload=" + VenueActions.ButtonOf(VenueActions.Get("harbour_unload")));
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("TRADEVENUE_SETUP_DONE");
			}
		}

		[SteamFunction]
		public static void TestTradeVenueAfterClient(Character self) {
			Player player = (Player) self;
			try {
				Report("theClientUnloadedTheShip", player.GetTag(VenueLabour.AtKey("harbour")) != null);
				VenueLabour.Forget(player, "harbour");
			} finally {
				Console.WriteLine("TRADEVENUE_CLIENT_DONE");
			}
		}

		[SteamFunction]
		public static void TestTradeVenueRules(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			try {
				player.Account.PLevel = 1;
				GoldUtils.Grant(player, 50000);
				foreach (string kind in new[] { "harbour", "stable", "bakery" }) { //a clean day, whatever ran before on this server
					VenueLabour.Forget(player, kind);
				}
				player.RemoveTag(VenueStay.DoneKey("stall"));
				player.RemoveTag(VenueStay.DoneKey("sentry"));
				Enemy(player);
				Labour(player);
				Stays(player);
				Archery(player);
				Errand(player);
			} finally {
				player.Account.PLevel = plevel;
				Errands.NowOverride = null;
				VenueClock.Override = 12;
				player.DeleteTimer(VenueStayTimer.Key);
				Console.WriteLine("TRADEVENUE_RULES_DONE");
			}
		}

		//the city's enemy gets nothing from its people, as with errands and shops
		private static void Enemy(Player player) {
			int was = Reputation.Of(player, "Edoras");
			Reputation.Set(player, "Edoras", Reputation.Min);
			bool enemy = (Reputation.Level(player, "Edoras") == ReputationLevel.Nepritel) && (Offers(player, "harbour", "harbourmaster").Count == 0) &&
				(Offers(player, "barracks", "captain").Count == 0);
			Reputation.Set(player, "Edoras", was);
			Report("theCitysEnemyGetsNothing", enemy && (Offers(player, "harbour", "harbourmaster").Count > 0));
		}

		private static void Labour(Player player) {
			List<string> master = Offers(player, "harbour", "harbourmaster"), fisher = Offers(player, "harbour", "fisher");
			Report("eachVenueOffersItsWork", master.Contains("harbour_unload") && Offers(player, "harbour", "docker").Contains("harbour_unload") && !fisher.Contains("harbour_unload") &&
				Offers(player, "stable", "groom").Contains("stable_chores") && Offers(player, "bakery", "baker").Contains("bakery_dough") &&
				Offers(player, "barracks", "captain").Contains("barracks_archery") && Offers(player, "barracks", "guard").Contains("barracks_archery"));

			//each kind of work rests on its own: the docks, then the stable at once
			Errands.NowOverride = DateTime.UtcNow;
			player.Stam = 50;
			long g0 = GoldUtils.CountGoldOn(player);
			Do(player, "harbour_unload", "harbour", "docker");
			long g1 = GoldUtils.CountGoldOn(player);
			Do(player, "harbour_unload", "harbour", "docker");
			long g2 = GoldUtils.CountGoldOn(player);
			Do(player, "stable_chores", "stable", "groom");
			long g3 = GoldUtils.CountGoldOn(player);
			Do(player, "bakery_dough", "bakery", "baker");
			int wage = VenueLifeSettings.instance.labourWage;
			Report("eachKindOfWorkRestsOnItsOwn", (g1 - g0 == wage) && (g2 == g1) && (g3 - g2 == wage) && (GoldUtils.CountGoldOn(player) - g3 == wage) &&
				(player.Stam == 50 - (3 * VenueLifeSettings.instance.labourStamina)));
			//the day's cap counts all kinds together: three turns of three kinds are three of the day's
			int perDay = VenueLifeSettings.instance.labourPerDay;
			VenueLifeSettings.instance.labourPerDay = 3;
			Errands.NowOverride = DateTime.UtcNow.AddMinutes(VenueLifeSettings.instance.labourMinutes + 1);
			player.Stam = 50;
			long g4 = GoldUtils.CountGoldOn(player);
			Do(player, "harbour_unload", "harbour", "docker");
			Report("theDailyCapCoversAllKinds", GoldUtils.CountGoldOn(player) == g4);
			VenueLifeSettings.instance.labourPerDay = perDay;
			Errands.NowOverride = null;
			player.Stam = 50;
		}

		private static void Stays(Player player) {
			VenueClock.Override = 12;
			bool stallByDay = Offers(player, "market", "merchant").Contains("market_stall") && !Offers(player, "barracks", "guard").Contains("barracks_sentry");
			VenueClock.Override = 23;
			bool sentryByNight = Offers(player, "barracks", "guard").Contains("barracks_sentry") && !Offers(player, "market", "merchant").Contains("market_stall");
			Report("theStallByDayTheSentryByNight", stallByDay && sentryByNight);

			//the stall: stay, get paid; one stay at a time; once a day
			VenueClock.Override = 12;
			long g0 = GoldUtils.CountGoldOn(player);
			Do(player, "market_stall", "market", "merchant");
			VenueStayTimer stall = player.GetTimer(VenueStayTimer.Key) as VenueStayTimer;
			Item guard = Gem(player, "barracks", "guard");
			Near(player, Residents.BodyOf(guard)); //inside the barracks: only the stall under way can refuse it
			bool oneAtATime = !VenueStay.Begin(player, Residents.BodyOf(guard), guard, "sentry") && (player.GetTimer(VenueStayTimer.Key) == stall);
			Near(player, Body(player, "market", "merchant"));
			int half = VenueLifeSettings.instance.stallSeconds / 2;
			bool goesOn = (stall != null) && stall.Step(player, half);
			bool ends = (stall != null) && !stall.Step(player, VenueLifeSettings.instance.stallSeconds - half);
			player.DeleteTimer(VenueStayTimer.Key);
			bool paid = GoldUtils.CountGoldOn(player) - g0 == VenueLifeSettings.instance.stallWage;
			Do(player, "market_stall", "market", "merchant");
			Report("aStallIsPaidAtTheEndOnceADay", oneAtATime && goesOn && ends && paid && (player.GetTimer(VenueStayTimer.Key) == null));

			//the night sentry: leaving breaks it with nothing; then wages and the city's word, not a hero's deed
			VenueClock.Override = 23;
			long gLeft = GoldUtils.CountGoldOn(player);
			Do(player, "barracks_sentry", "barracks", "guard");
			VenueStayTimer left = player.GetTimer(VenueStayTimer.Key) as VenueStayTimer;
			player.Go(new Point4D((ushort) (player.X + 40), player.Y, player.Z, player.M));
			bool broken = (left != null) && !left.Step(player, VenueStayTimer.StepSeconds);
			player.DeleteTimer(VenueStayTimer.Key);
			Report("leavingBreaksASentry", broken && (GoldUtils.CountGoldOn(player) == gLeft) && !VenueStay.DoneToday(player, "sentry"));
			player.RemoveTag(Reputation.CityDeedKey("Edoras"));
			int rep0 = Reputation.Of(player, "Edoras");
			long g1 = GoldUtils.CountGoldOn(player);
			Do(player, "barracks_sentry", "barracks", "guard");
			VenueStayTimer sentry = player.GetTimer(VenueStayTimer.Key) as VenueStayTimer;
			bool done = (sentry != null) && !sentry.Step(player, VenueLifeSettings.instance.sentrySeconds);
			player.DeleteTimer(VenueStayTimer.Key);
			Report("aSentryPaysAndRaisesTheCity", done && (GoldUtils.CountGoldOn(player) - g1 == VenueLifeSettings.instance.sentryWage) &&
				(Reputation.Of(player, "Edoras") == rep0 + VenueLifeSettings.instance.sentryReputation) && (player.GetTag(Reputation.CityDeedKey("Edoras")) == null));
			VenueClock.Override = 12;
		}

		private static void Archery(Player player) {
			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			player.RemoveTag(TradeVenues.ArcheryAtTK);
			player.SetRealSkillValue((int) SkillName.Archery, 0);
			Do(player, "barracks_archery", "barracks", "captain");
			bool untrained = player.GetTag(TradeVenues.ArcheryAtTK) == null;
			player.SetRealSkillValue((int) SkillName.Archery, 1000);
			int exp0 = player.Experience;
			Do(player, "barracks_archery", "barracks", "captain");
			bool allHit = player.Experience - exp0 == TradeVenues.Shots * VenueLifeSettings.instance.archeryExp;
			int exp1 = player.Experience;
			Do(player, "barracks_archery", "barracks", "captain");
			bool rests = player.Experience == exp1;
			Errands.NowOverride = start.AddMinutes(VenueLifeSettings.instance.archeryMinutes + 1);
			Do(player, "barracks_archery", "barracks", "captain");
			Report("targetPracticeHitsByArcheryAndRests", untrained && allHit && rests && (player.Experience - exp1 == TradeVenues.Shots * VenueLifeSettings.instance.archeryExp));
			Errands.NowOverride = null;
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

		private static bool Step(Player player, Errand errand, int choice = -1) {
			Item gem = Residents.ById(errand.Current.residentId);
			Near(player, Residents.BodyOf(gem));
			return Errands.Do(player, errand, Residents.BodyOf(gem), choice);
		}

		private static void Errand(Player player) {
			Item master = Gem(player, "harbour", "harbourmaster"), merchant = Gem(player, "market", "merchant");
			Item baker = Gem(player, "bakery", "baker"), captain = Gem(player, "barracks", "captain");
			Report("eachErrandOnlyFromItsRole", (Build(player, "cargolist", Gem(player, "harbour", "docker")) == null) && (Build(player, "counterfeit", Gem(player, "market", "crier")) == null) &&
				(Build(player, "breadround", master) == null) && (Build(player, "patrol", Gem(player, "barracks", "guard")) == null));

			//the fish order
			FreshLog(player);
			Errand fish = Build(player, "fishorder", Gem(player, "harbour", "fisher"));
			Errands.Accept(player, fish);
			Item catchOf = (Item) ((ItemDef) ThingDef.GetByDefname("i_fish_big_1")).Create(player.Backpack);
			catchOf.Amount = 3;
			Report("aFishOrderWantsThreeFish", (fish.steps[0].count == 3) && Step(player, fish) && fish.IsDone);

			//smuggling and counterfeits: the hint names the right answer, which pays in full
			bool hinted = true, sharesRight = true;
			foreach (string key in new[] { "cargolist", "counterfeit" }) {
				Item giver = (key == "cargolist") ? master : merchant;
				int[] share = new int[2];
				for (int round = 0; round < 2; round++) {
					FreshLog(player);
					Errand puzzle = Build(player, key, giver);
					Errands.Accept(player, puzzle);
					ErrandStep ask = puzzle.steps[1];
					string answer = ask.choices[ask.count];
					string hint = puzzle.steps[0].reply;
					hinted &= (key == "cargolist") ? hint.Contains(answer.Replace("Bedna ", "").Trim('"') + "\" byla tezka") : hint.Contains(answer + " mel");
					Step(player, puzzle);
					Step(player, puzzle, (round == 0) ? ask.count : (ask.count + 1) % 3);
					share[round] = puzzle.IsDone ? ErrandTemplates.Get(key).PayPercent(puzzle) : -1;
				}
				sharesRight &= (share[0] == 100) && (share[1] == 25);
			}
			Report("theHintNamesTheRightAnswer", hinted);
			Report("theRightAnswerPaysInFull", sharesRight);

			//the counterfeit's three payers have three names: they're the choices
			bool distinct = true;
			for (int i = 0; i < 10; i++) {
				Errand coins = Build(player, "counterfeit", merchant);
				List<string> names = coins.steps[1].choices;
				distinct &= (names.Count == 3) && (names[0] != names[1]) && (names[1] != names[2]) && (names[0] != names[2]);
			}
			Report("thePayersHaveThreeNames", distinct);

			//the bread round: three homes, twenty minutes; the errand's loaf keeps apart from the player's own
			FreshLog(player);
			Item own = (Item) ((ItemDef) ThingDef.GetByDefname("i_bread_loaf")).Create(player.Backpack);
			own.Amount = 2;
			Errand bread = Build(player, "breadround", baker);
			DateTime before = DateTime.UtcNow;
			Errands.Accept(player, bread);
			Item loaf = bread.steps[0].placed;
			player.Backpack.TryStackToAnyInside(loaf);
			player.Backpack.TryStackToAnyInside(own);
			Report("theErrandsLoafKeepsApart", (loaf != null) && !loaf.IsDeleted && (loaf.Amount == 1) && !own.IsDeleted && (own.Amount == 2));
			bool three = (bread.steps.Count == 3) && (bread.steps[0].residentId != bread.steps[1].residentId) && (bread.steps[1].residentId != bread.steps[2].residentId);
			bool short20 = Math.Abs((bread.Expires - before).TotalMinutes - 20) < 1;
			bool all = Step(player, bread) && Step(player, bread) && Step(player, bread);
			Report("theBreadRoundIsThreeHomesInTwentyMinutes", three && short20 && all && bread.IsDone && !own.IsDeleted && (own.Amount == 2));
			own.Delete();

			//the patrol: the recruit walks along
			FreshLog(player);
			Errand patrol = Build(player, "patrol", captain);
			Errands.Accept(player, patrol);
			Item recruit = Residents.ById(patrol.steps[0].residentId);
			NPC recruitBody = Residents.BodyOf(recruit);
			Step(player, patrol);
			bool follows = recruitBody.FollowTarget == player;
			Item visit = Residents.ById(patrol.steps[1].residentId);
			NPC visitBody = Residents.BodyOf(visit);
			recruitBody.Go(new Point4D(visitBody.X, visitBody.Y, visitBody.Z, visitBody.M));
			bool there = Step(player, patrol);
			NPC captainBody = Residents.BodyOf(captain);
			recruitBody.Go(new Point4D(captainBody.X, captainBody.Y, captainBody.Z, captainBody.M));
			bool back = Step(player, patrol);
			Report("aRecruitWalksThePatrol", (Residents.RoleKeyOf(recruit) == "recruit") && follows && there && back && patrol.IsDone && (recruitBody.FollowTarget == null));
			FreshLog(player);
		}
	}
}
