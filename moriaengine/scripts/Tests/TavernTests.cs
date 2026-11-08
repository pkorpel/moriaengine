using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1254 - the tavern's life checked on the server; Tests/Protocol/TavernTests.cs drives these and opens the
	/// innkeeper's dialog through a client. Edoras gets a tavern with its people, in the evening.</summary>
	public static class TavernTests {
		private static readonly TagKey venueTK = TagKey.Acquire("_taverntest_venue_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("TAVERN_CHECK: " + name + "=" + ok);
		}

		private static CityVenue Venue(Player player) {
			return (CityVenue) player.GetTag(venueTK);
		}

		private static Item Gem(Player player, string role) {
			foreach (Item gem in Residents.OfVenue(Venue(player).VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					return gem;
				}
			}
			return null;
		}

		//at their own spot in the tavern, where they offer what the tavern has
		private static void Inside(Player player, Item gem) {
			int[] spot = ResidentSchedule.SpotIn(Venue(player), Residents.IdOf(gem));
			Residents.BodyOf(gem).Go(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], Venue(player).M));
		}

		private static void Near(Player player, Item gem) {
			NPC npc = Residents.BodyOf(gem);
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		private static List<string> Offers(Player player, Item gem) {
			List<string> keys = new List<string>();
			foreach (VenueAction action in VenueActions.For(player, Residents.BodyOf(gem), gem)) {
				keys.Add(action.Key);
			}
			return keys;
		}

		private static int Button(string key) {
			return VenueActions.ButtonOf(VenueActions.Get(key));
		}

		[SteamFunction]
		public static void TestTavernSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				VenueClock.Override = 19; //evening: the bard and the patrons are in
				List<FoundRoom> rooms = RoomScan.Candidates("Edoras", player.MovementSettings);
				VenueDraft draft = VenueDraft.OfRoom(rooms[0], rooms[0].M, "Edoras");
				draft.SetType(VenueType.Get("tavern").IndexOf());
				string problem;
				CityVenue venue = draft.Save(out problem);
				if (venue == null) {
					throw new SEException("the tavern wasn't saved: " + problem);
				}
				Residents.FillRoles(venue);
				player.SetTag(venueTK, venue);
				foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
					NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
					Inside(player, gem);
				}
				Item innkeeper = Gem(player, "innkeeper");
				Near(player, innkeeper);
				Report("fiveActionsAreRegistered", (VenueActions.Get("tavern_rumour") != null) && (VenueActions.Get("tavern_room") != null) &&
					(VenueActions.Get("tavern_dice") != null) && (VenueActions.Get("tavern_song") != null) && (VenueActions.Get("tavern_tip") != null));
				Tavern.ForgetRumours();
				Console.WriteLine("TAVERN_SETUP: innkeeper=" + Residents.BodyOf(innkeeper).Uid + " rumour=" + Button("tavern_rumour") +
					" room=" + Button("tavern_room"));
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("TAVERN_SETUP_DONE");
			}
		}

		/// <summary>After the client clicked the innkeeper's "Co je noveho?" and then "Pokoj na noc".</summary>
		[SteamFunction]
		public static void TestTavernAfterClient(Character self) {
			Player player = (Player) self;
			try {
				Report("theInnkeeperToldARumour", Tavern.LastRumourOf(player, Residents.IdOf(Gem(player, "innkeeper"))) != null);
				Report("theRoomIsTaken", player.GetTag(Tavern.RestedTK) != null);
				player.RemoveTag(Tavern.RestedTK);
				player.RemoveTag(Tavern.RoomPaidTK); //the rules' room starts the day afresh
			} finally {
				Console.WriteLine("TAVERN_CLIENT_DONE");
			}
		}

		[SteamFunction]
		public static void TestTavernRules(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			int gamesPerHour = TavernSettings.instance.gamesPerHour;
			try {
				Rules(player);
			} finally {
				player.Account.PLevel = plevel;
				TavernSettings.instance.gamesPerHour = gamesPerHour;
				Errands.NowOverride = null;
				VenueClock.Override = 19;
				Tavern.RollOverride.Clear();
				Console.WriteLine("TAVERN_RULES_DONE");
			}
		}

		private static void Rules(Player player) {
			Item innkeeper = Gem(player, "innkeeper"), patron = Gem(player, "patron"), bard = Gem(player, "bard");
			NPC innBody = Residents.BodyOf(innkeeper), patronBody = Residents.BodyOf(patron), bardBody = Residents.BodyOf(bard);

			//who offers what, and only in the tavern
			List<string> inn = Offers(player, innkeeper);
			Report("theInnkeeperOffersRumoursARoomAndDice", inn.Contains("tavern_rumour") && inn.Contains("tavern_room") && inn.Contains("tavern_dice") &&
				!inn.Contains("tavern_song"));
			List<string> guest = Offers(player, patron);
			Report("aPatronHasNoRooms", guest.Contains("tavern_rumour") && guest.Contains("tavern_dice") && !guest.Contains("tavern_room"));
			List<string> evening = Offers(player, bard);
			VenueClock.Override = 12;
			List<string> noon = Offers(player, bard);
			VenueClock.Override = 19;
			Report("theBardSingsInTheEvening", evening.Contains("tavern_song") && evening.Contains("tavern_tip") && !noon.Contains("tavern_song") && noon.Contains("tavern_tip"));
			patronBody.Go(new Point4D((ushort) (patronBody.X + 40), patronBody.Y, patronBody.Z, patronBody.M));
			Report("outsideTheTavernNothing", Offers(player, patron).Count == 0);
			Inside(player, patron);

			//rumours: about the city's people or its news, one a minute from each
			bool about = true, plain = true;
			for (int i = 0; i < 20; i++) {
				string text = Tavern.AboutSomeone(innkeeper, "Edoras");
				about &= (text != null) && !text.StartsWith(Residents.NameOf(innkeeper) + " ", StringComparison.Ordinal);
				plain &= IsPlain(text) && IsPlain(Tavern.Lore("Edoras")) && IsPlain(Tavern.Lore("Barad-dur"));
			}
			Report("rumoursTellOfOthers", about);
			Report("rumoursArePlainAscii", plain);
			bool ownNews = true;
			for (int i = 0; i < 30; i++) {
				ownNews &= (Array.IndexOf(Tavern.CommonLore, Tavern.Lore("Barad-dur")) < 0) && (Array.IndexOf(Tavern.CommonLore, Tavern.Lore("Nurn")) < 0)
					&& (Array.IndexOf(Tavern.CommonLore, Tavern.Lore("Rhun")) < 0);
			}
			Report("eachCityHasItsNewsAndMordorOnlyItsOwn", (Tavern.LocalLore("Minas_Tirith") != null) && (Tavern.LocalLore("Edoras") != null) &&
				(Tavern.LocalLore("Rhun") != null) && ownNews); //#1304 - Rhun has its own news now, still on Mordor's side
			Tavern.ForgetRumours();
			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			VenueActions.Get("tavern_rumour").Do(player, patronBody, patron);
			DateTime? first = Tavern.LastRumourOf(player, Residents.IdOf(patron));
			Errands.NowOverride = start.AddSeconds(30);
			VenueActions.Get("tavern_rumour").Do(player, patronBody, patron);
			bool heldBack = Tavern.LastRumourOf(player, Residents.IdOf(patron)) == first;
			Errands.NowOverride = start.AddSeconds(61);
			VenueActions.Get("tavern_rumour").Do(player, patronBody, patron);
			Report("aRumourAMinute", (first == start) && heldBack && (Tavern.LastRumourOf(player, Residents.IdOf(patron)) == start.AddSeconds(61)));

			//the room: paid once a real day, a booster at the next login
			player.Account.PLevel = 1;
			GoldUtils.Grant(player, 5000);
			Errands.NowOverride = start;
			long before = GoldUtils.CountGoldOn(player);
			VenueActions.Get("tavern_room").Do(player, innBody, innkeeper);
			long afterRoom = GoldUtils.CountGoldOn(player);
			Report("aRoomCostsItsPrice", (before - afterRoom == TavernSettings.instance.roomPrice) && (player.GetTag(Tavern.RestedTK) != null));
			VenueActions.Get("tavern_room").Do(player, innBody, innkeeper);
			Report("notTwiceBeforeSleeping", GoldUtils.CountGoldOn(player) == afterRoom);
			int storedBefore = StoredOf(player, TavernSettings.instance.roomBoosterKind);
			player.On_LogIn();
			Report("theNextLoginGivesABooster", (StoredOf(player, TavernSettings.instance.roomBoosterKind) == storedBefore + 1) && (player.GetTag(Tavern.RestedTK) == null));
			player.On_LogIn();
			Report("onlyOnce", StoredOf(player, TavernSettings.instance.roomBoosterKind) == storedBefore + 1);
			Errands.NowOverride = start.AddHours(23);
			VenueActions.Get("tavern_room").Do(player, innBody, innkeeper);
			bool refusedSameDay = (GoldUtils.CountGoldOn(player) == afterRoom) && (player.GetTag(Tavern.RestedTK) == null);
			Errands.NowOverride = start.AddHours(25);
			VenueActions.Get("tavern_room").Do(player, innBody, innkeeper);
			Report("aRoomOnceARealDay", refusedSameDay && (GoldUtils.CountGoldOn(player) == afterRoom - TavernSettings.instance.roomPrice));
			player.RemoveTag(Tavern.RestedTK);

			//dice: win double, lose the stake, a tie gives it back, a cap an hour, no GM
			Errands.NowOverride = start.AddHours(30);
			int bet = TavernSettings.instance.betMedium;
			long[] gained = new long[3];
			int[][] rolls = { new[] { 6, 6, 1, 1 }, new[] { 1, 2, 5, 6 }, new[] { 3, 4, 5, 2 } };
			bool played = true;
			for (int i = 0; i < 3; i++) {
				foreach (int r in rolls[i]) {
					Tavern.RollOverride.Enqueue(r);
				}
				long g = GoldUtils.CountGoldOn(player);
				played &= Tavern.Play(player, patronBody, bet);
				gained[i] = GoldUtils.CountGoldOn(player) - g;
			}
			Report("diceWinLoseTie", played && (gained[0] == bet) && (gained[1] == -bet) && (gained[2] == 0));
			TavernSettings.instance.gamesPerHour = 4;
			bool fourth = Tavern.Play(player, patronBody, bet);
			long capped = GoldUtils.CountGoldOn(player);
			bool fifth = Tavern.Play(player, patronBody, bet);
			bool untouched = GoldUtils.CountGoldOn(player) == capped;
			Errands.NowOverride = start.AddHours(31.1);
			bool broke = Tavern.Play(player, patronBody, 100000000); //a new hour whose first bet can't be paid still starts the count afresh
			bool nextHour = Tavern.Play(player, patronBody, bet);
			Report("gamesAreCappedAnHour", fourth && !fifth && untouched && !broke && nextHour);
			player.Account.PLevel = 7;
			Report("aGmDoesNotGamble", !Tavern.Play(player, patronBody, bet));
			player.Account.PLevel = 1;

			//the bard: a song a line at a time, a tip makes a friend once a day
			Near(player, bard);
			VenueActions.Get("tavern_song").Do(player, bardBody, bard);
			BardSongTimer song = bardBody.GetTimer(BardSongTimer.Key) as BardSongTimer;
			Report("theBardSings", (song != null) && (song.line == 1) && !Offers(player, bard).Contains("tavern_song"));
			Errands.NowOverride = start.AddHours(40);
			string bardId = Residents.IdOf(bard);
			int friendBefore = Residents.Friendship(player, bardId);
			long goldBefore = GoldUtils.CountGoldOn(player);
			VenueActions.Get("tavern_tip").Do(player, bardBody, bard);
			VenueActions.Get("tavern_tip").Do(player, bardBody, bard);
			bool twoPaid = GoldUtils.CountGoldOn(player) == goldBefore - (2 * TavernSettings.instance.tip);
			int afterTwo = Residents.Friendship(player, bardId);
			Errands.NowOverride = start.AddHours(65);
			VenueActions.Get("tavern_tip").Do(player, bardBody, bard);
			Report("aTipMakesAFriendOnceADay", twoPaid && (afterTwo == Math.Min(Residents.MaxFriendship, friendBefore + 1)) &&
				(Residents.Friendship(player, bardId) == Math.Min(Residents.MaxFriendship, friendBefore + 2)));
			Console.WriteLine("TAVERN_SONG: bard=" + bardBody.Uid);
		}

		private static int StoredOf(Player player, int kind) {
			foreach (KeyValuePair<int, int> row in Boosters.Stored(player)) {
				if (row.Key == kind) {
					return row.Value;
				}
			}
			return 0;
		}

		private static bool IsPlain(string text) {
			foreach (char c in text ?? "") {
				if (c > 127) {
					return false;
				}
			}
			return true;
		}

		/// <summary>Some seconds after the song began: how far the bard got.</summary>
		[SteamFunction]
		public static void TestTavernSongLater(Character self) {
			Player player = (Player) self;
			try {
				NPC bardBody = Residents.BodyOf(Gem(player, "bard"));
				BardSongTimer song = bardBody.GetTimer(BardSongTimer.Key) as BardSongTimer;
				Report("theSongGoesOnLineByLine", (song != null) && (song.line >= 2));
				Console.WriteLine("TAVERN_SONG_LINE: line=" + ((song == null) ? -1 : song.line));
			} finally {
				Console.WriteLine("TAVERN_SONG_DONE");
			}
		}
	}
}
