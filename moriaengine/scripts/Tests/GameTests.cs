using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1298 - the games on the server: arm wrestling, the drinking contest, the archery contest and riddles, with their
	/// stakes, pauses and who offers them. Tests/Protocol/GameTests.cs has a client wrestle a patron and answer a scholar's
	/// riddle first. Edoras gets an open-air tavern, library and barracks, in the evening.</summary>
	public static class GameTests {
		private static readonly string[] types = { "tavern", "library", "barracks" };

		private static void Report(string name, bool ok) {
			Console.WriteLine("GAMES_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string type) {
			return TagKey.Acquire("_gametest_" + type + "_");
		}

		private static Item Gem(Player player, string type, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(type));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					return gem;
				}
			}
			return null;
		}

		private static NPC Body(Player player, string type, string role) {
			return Residents.BodyOf(Gem(player, type, role));
		}

		private static void Near(Player player, NPC npc) {
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		private static bool Offers(Player player, string type, string role, string key) {
			Item gem = Gem(player, type, role);
			return VenueActions.For(player, Residents.BodyOf(gem), gem).Contains(VenueActions.Get(key));
		}

		/// <summary>The three venues with their people at their own spots (frozen), the hero beside the tavern's patron, gold in the pack.</summary>
		[SteamFunction]
		public static void TestGamesSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				VenueClock.Override = 19; //evening: the patrons are in
				Point4D p = WorldEvents.CentreOf("Edoras");
				Map map = Map.GetMap(p.M);
				List<int[]> spots = new List<int[]>();
				foreach (int[] tile in WorldEvents.Reachable(map, new[] { p.X, p.Y, p.Z }, 25)) {
					if ((WorldSpots.Distance(tile, p.X, p.Y) >= 3) && spots.TrueForAll(o => WorldSpots.Distance(tile, o[0], o[1]) >= 6)) {
						spots.Add(tile);
					}
				}
				if (spots.Count < types.Length) {
					throw new SEException("Only " + spots.Count + " spots for the test venues near Edoras's centre.");
				}
				for (int i = 0; i < types.Length; i++) {
					VenueDraft draft = new VenueDraft();
					draft.SetType(VenueType.Get(types[i]).IndexOf());
					draft.M = p.M;
					draft.City = "Edoras";
					draft.Rects.Add(new ImmutableRectangle(spots[i][0] - 2, spots[i][1] - 2, spots[i][0] + 2, spots[i][1] + 2));
					CityVenue venue = draft.Save(out string problem);
					if (venue == null) {
						throw new SEException(types[i] + " wasn't saved: " + problem);
					}
					Residents.FillRoles(venue);
					foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
						NPC body = Residents.BodyOf(gem);
						NPCAiTimer.Freeze(body, 900);
						int[] spot = ResidentSchedule.SpotIn(venue, Residents.IdOf(gem));
						body.Go(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], venue.M));
					}
					player.SetTag(VenueKey(types[i]), venue);
				}
				GoldUtils.Grant(player, 2000);
				player.RemoveTag(Games.RiddleTK);
				Games.RollOverride.Clear();
				NPC patron = Body(player, "tavern", "patron"), scholar = Body(player, "library", "scholar");
				Near(player, patron);
				Console.WriteLine("GAMES_SETUP: patron=" + patron.Uid + " scholar=" + scholar.Uid + " arm=" + VenueActions.ButtonOf(VenueActions.Get("game_arm"))
					+ " riddle=" + VenueActions.ButtonOf(VenueActions.Get("game_riddle")) + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = Math.Min(plevel, (byte) 1); //a plain player: a GM doesn't play for money
				Console.WriteLine("GAMES_SETUP_DONE");
			}
		}

		/// <summary>The client's bout is won: three pulls the player's way.</summary>
		[SteamFunction]
		public static void TestGamesArmWins(Character self) {
			Games.RollOverride.Clear();
			for (int i = 0; i < Games.Pulls; i++) {
				Games.RollOverride.Enqueue(1000); //mine: Roll returns it as is
				Games.RollOverride.Enqueue(0);
			}
			Console.WriteLine("GAMES_ROLLS: arm");
		}

		/// <summary>The client's riddle: the first one (a comb); the hero beside the scholar.</summary>
		[SteamFunction]
		public static void TestGamesRiddleNext(Character self) {
			Player player = (Player) self;
			Games.RollOverride.Clear();
			Games.RollOverride.Enqueue(0);
			Near(player, Body(player, "library", "scholar"));
			Console.WriteLine("GAMES_ROLLS: riddle exp=" + player.Experience + " level=" + player.Level);
		}

		[SteamFunction]
		public static void TestGamesRules(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			GameSettings s = GameSettings.instance;
			int armRounds = s.armRounds, armPerHour = s.armPerHour, riddleMinutes = s.riddleMinutes;
			try {
				account.PLevel = 1;
				Rules(player);
			} finally {
				s.armRounds = armRounds;
				s.armPerHour = armPerHour;
				s.riddleMinutes = riddleMinutes;
				account.PLevel = plevel;
				Errands.NowOverride = null;
				VenueClock.Override = null;
				Games.RollOverride.Clear();
				Console.WriteLine("GAMES_RULES_DONE");
			}
		}

		private static void Queue(params int[] rolls) {
			Games.RollOverride.Clear();
			foreach (int roll in rolls) {
				Games.RollOverride.Enqueue(roll);
			}
		}

		private static void Rules(Player player) {
			GameSettings s = GameSettings.instance;
			NPC patron = Body(player, "tavern", "patron"), innkeeper = Body(player, "tavern", "innkeeper"), captain = Body(player, "barracks", "captain");
			NPC scholar = Body(player, "library", "scholar");
			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;

			//who offers what
			Report("whoOffersWhat", Offers(player, "tavern", "patron", "game_arm") && Offers(player, "tavern", "patron", "game_riddle")
				&& Offers(player, "tavern", "innkeeper", "game_drink") && !Offers(player, "tavern", "cook", "game_drink") && !Offers(player, "tavern", "innkeeper", "game_arm")
				&& Offers(player, "library", "scholar", "game_riddle") && Offers(player, "barracks", "captain", "game_archery")
				&& !Offers(player, "barracks", "guard", "game_archery"));

			//arm wrestling: the client's bout rests this patron's arm; later a loss and a draw
			int bet = TavernSettings.instance.betSmall;
			Near(player, patron);
			long gold = player.Gold;
			Queue(1000, 0, 1000, 0, 1000, 0);
			bool rests = !Games.Wrestle(player, patron, bet) && (player.Gold == gold);
			Errands.NowOverride = start.AddMinutes(s.armMinutes + 1);
			Queue(0, 1000, 0, 1000, 0, 1000);
			bool lost = Games.Wrestle(player, patron, bet) && (player.Gold == gold - bet);
			Errands.NowOverride = start.AddMinutes((2 * s.armMinutes) + 2);
			s.armRounds = 2;
			Queue(5, 5, 5, 5);
			gold = player.Gold;
			bool draw = Games.Wrestle(player, patron, bet) && (player.Gold == gold);
			Report("armWinLoseDrawAndRest", rests && lost && draw);
			s.armRounds = 15;
			s.armPerHour = 0;
			Errands.NowOverride = start.AddMinutes((3 * s.armMinutes) + 3);
			Report("armCappedAnHour", !Games.Wrestle(player, patron, bet) && (player.Gold == gold));
			s.armPerHour = 10;

			//the drinking contest: win, then (an hour on) a loss under the table, then a draw
			Near(player, innkeeper);
			Errands.NowOverride = start;
			player.Stam = player.MaxStam;
			gold = player.Gold;
			Queue(0, 99);
			Games.Drink(player, innkeeper, Gem(player, "tavern", "innkeeper"));
			bool won = player.Gold == gold + s.drinkStake;
			gold = player.Gold;
			Games.Drink(player, innkeeper, Gem(player, "tavern", "innkeeper"));
			bool once = player.Gold == gold;
			Errands.NowOverride = start.AddMinutes(s.drinkMinutes + 1);
			Queue(99, 0);
			Games.Drink(player, innkeeper, Gem(player, "tavern", "innkeeper"));
			bool lostDrink = (player.Gold == gold - s.drinkStake) && (player.Stam == 0);
			Errands.NowOverride = start.AddMinutes((2 * s.drinkMinutes) + 2);
			gold = player.Gold;
			Queue(99, 99);
			Games.Drink(player, innkeeper, Gem(player, "tavern", "innkeeper"));
			bool drawDrink = player.Gold == gold;
			Report("drinkWinLoseDrawOnceAnHour", won && once && lostDrink && drawDrink);
			string rivalName;
			Games.Rival(Residents.WorkplaceOf(Gem(player, "tavern", "innkeeper")), out rivalName);
			Report("aPatronDrinksAgainstYou", rivalName == Residents.NameOf(Gem(player, "tavern", "patron")));

			//the archery contest: no skill no contest; win, rest, lose, tie
			Near(player, captain);
			Item captainGem = Gem(player, "barracks", "captain");
			Errands.NowOverride = start;
			player.SetRealSkillValue((int) SkillName.Archery, 0);
			gold = player.Gold;
			Games.ArcheryContest(player, captain, captainGem);
			bool untrained = player.Gold == gold;
			player.SetRealSkillValue((int) SkillName.Archery, 900); //a roll of 999 misses, 0 hits
			Queue(0, 999, 0, 999, 0, 999, 0, 999, 0, 999);
			Games.ArcheryContest(player, captain, captainGem);
			bool wonArchery = player.Gold == gold - s.archeryFee + s.archeryPrize;
			gold = player.Gold;
			Games.ArcheryContest(player, captain, captainGem);
			bool archeryRests = player.Gold == gold;
			Errands.NowOverride = start.AddMinutes(s.archeryContestMinutes + 1);
			Queue(999, 0, 999, 0, 999, 0, 999, 0, 999, 0);
			Games.ArcheryContest(player, captain, captainGem);
			bool lostArchery = player.Gold == gold - s.archeryFee;
			Errands.NowOverride = start.AddMinutes((2 * s.archeryContestMinutes) + 2);
			gold = player.Gold;
			Queue(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			Games.ArcheryContest(player, captain, captainGem);
			Report("archeryWinLoseTieAndRest", untrained && wonArchery && archeryRests && lostArchery && (player.Gold == gold));

			//riddles (a minute apart here): the riddle asked stays until answered, only it is answered, a wrong answer is told and
			//pays nothing, the next waits; a right one pays, and the first of a real day makes a friend
			Near(player, scholar);
			Item scholarGem = Gem(player, "library", "scholar");
			s.riddleMinutes = 1;
			player.RemoveTag(Games.RiddleTK);
			player.RemoveTag(Games.RiddleAtTK);
			player.RemoveTag(Games.RiddleDayKey(scholarGem));
			Errands.NowOverride = start;
			Queue(4);
			Games.AskRiddle(player, scholar, scholarGem);
			bool same = (player.GetTag(Games.RiddleTK) is int first) && (first == 4);
			Queue(7);
			Games.AskRiddle(player, scholar, scholarGem);
			same &= (player.GetTag(Games.RiddleTK) is int again) && (again == 4);
			bool onlyAsked = !Games.Answer(player, scholar, 5, "tma");
			int exp = player.Experience, level = player.Level;
			bool wrong = Games.Answer(player, scholar, 4, "oko") && (player.Experience == exp) && (player.Level == level) && (player.GetTag(Games.RiddleTK) == null);
			Games.AskRiddle(player, scholar, scholarGem);
			bool waits = player.GetTag(Games.RiddleTK) == null;
			Report("riddleStaysOnlyItIsAnsweredWrongIsToldNextWaits", same && onlyAsked && wrong && waits);
			Errands.NowOverride = start.AddMinutes(2);
			int friendship = Residents.Friendship(player, Residents.IdOf(scholarGem));
			Queue(5);
			Games.AskRiddle(player, scholar, scholarGem);
			bool right = Games.Answer(player, scholar, 5, " Tma! ") && ((player.Experience > exp) || (player.Level > level));
			bool friend = Residents.Friendship(player, Residents.IdOf(scholarGem)) == friendship + 1;
			Errands.NowOverride = start.AddMinutes(4);
			Queue(0);
			Games.AskRiddle(player, scholar, scholarGem);
			bool rightAgain = Games.Answer(player, scholar, 0, "HREBEN");
			bool friendOnce = Residents.Friendship(player, Residents.IdOf(scholarGem)) == friendship + 1;
			Report("riddleRightPaysAndAFriendOnceADay", right && friend && rightAgain && friendOnce);

			bool ascii = true;
			foreach (string[] riddle in Games.Riddles) {
				foreach (string text in riddle) {
					foreach (char c in text) {
						ascii &= c < 128;
					}
				}
				for (int i = 1; i < riddle.Length; i++) {
					ascii &= Games.Plain(riddle[i]) == riddle[i];
				}
			}
			Report("riddlesArePlainAscii", ascii);

			//a GM plays nothing for money
			player.Account.PLevel = 4;
			Errands.NowOverride = start.AddDays(1);
			gold = player.Gold;
			bool gmArm = !Games.Wrestle(player, patron, bet);
			Games.Drink(player, innkeeper, Gem(player, "tavern", "innkeeper"));
			Games.ArcheryContest(player, captain, captainGem);
			player.Account.PLevel = 1;
			Report("aGmDoesNotPlayForMoney", gmArm && (player.Gold == gold));
		}

		/// <summary>After the client's bout and answer.</summary>
		[SteamFunction]
		public static void TestGamesAfterClient(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] was = sa.Args.Trim().Trim('"').Split(' ');
			long gold = long.Parse(was[0]);
			int exp = int.Parse(was[1]), level = int.Parse(was[2]);
			Report("theClientWonABout", player.Gold == gold + TavernSettings.instance.betSmall);
			Report("theClientSolvedARiddle", (player.Experience > exp) || (player.Level > level));
			Console.WriteLine("GAMES_CLIENT_DONE");
		}
	}
}
