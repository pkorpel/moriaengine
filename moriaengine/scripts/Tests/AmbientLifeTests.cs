using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1262 - barks, conversations and the crier checked on the server; Tests/Protocol/AmbientLifeTests.cs hears a bark
	/// through a client and sends the GM's .vyvolat. Edoras gets a market and a tavern, by day.</summary>
	public static class AmbientLifeTests {
		private static readonly TagKey marketTK = TagKey.Acquire("_ambient_market_"), tavernTK = TagKey.Acquire("_ambient_tavern_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("AMBIENT_CHECK: " + name + "=" + ok);
		}

		private static CityVenue Market(Player player) {
			return (CityVenue) player.GetTag(marketTK);
		}

		private static List<Item> Of(CityVenue venue, string role) {
			List<Item> gems = new List<Item>();
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					gems.Add(gem);
				}
			}
			return gems;
		}

		private static void Inside(CityVenue venue, Item gem) {
			int[] spot = ResidentSchedule.SpotIn(venue, Residents.IdOf(gem));
			Residents.BodyOf(gem).Go(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], venue.M));
		}

		private static void Near(Player player, NPC npc) {
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		private static int Talking(CityVenue venue) {
			int n = 0;
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.BodyOf(gem).GetTimer(ConversationTimer.Key) != null) {
					n++;
				}
			}
			return n;
		}

		private static bool IsPlain(string text) {
			foreach (char c in text ?? "") {
				if (c > 127) {
					return false;
				}
			}
			return true;
		}

		[SteamFunction]
		public static void TestAmbientSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				VenueClock.Override = 12;
				VenueLifeSettings s = VenueLifeSettings.instance;
				s.barkChance = 0; //the scheduler's own ticks stay quiet: the checks call each part themselves
				s.talkChance = 0;
				s.crierMinutes = 100000;
				List<FoundRoom> rooms = RoomScan.Candidates("Edoras", player.MovementSettings);
				string[] types = { "market", "tavern" };
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
					player.SetTag((i == 0) ? marketTK : tavernTK, venue);
				}
				AmbientLife.Forget();
				CityNews.Clear();
				Near(player, Residents.BodyOf(Of(Market(player), "merchant")[0]));
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("AMBIENT_SETUP_DONE");
			}
		}

		/// <summary>A bark now, wherever the player stands; its words for the client to hear.</summary>
		[SteamFunction]
		public static void TestAmbientBark(Character self) {
			try {
				AmbientLife.Forget();
				int barked = AmbientLife.Barks(100);
				Report("aPassingPlayerHearsABark", (barked > 0) && (AmbientLife.LastBarkText != null));
				Console.WriteLine("AMBIENT_BARK: text=" + AmbientLife.LastBarkText);
			} finally {
				Console.WriteLine("AMBIENT_BARK_DONE");
			}
		}

		[SteamFunction]
		public static void TestAmbientRules(Character self) {
			Player player = (Player) self;
			VenueLifeSettings s = VenueLifeSettings.instance;
			try {
				Barks(player);
				Talks(player);
				Crier(player);
				Hero(player);
			} finally {
				Errands.NowOverride = null;
				s.crierMinutes = 100000;
				Console.WriteLine("AMBIENT_RULES_DONE");
			}
		}

		private static void Barks(Player player) {
			CityVenue market = Market(player);
			NPC merchant = Residents.BodyOf(Of(market, "merchant")[0]);
			Near(player, merchant);

			//only near a player
			player.Go(new Point4D((ushort) (merchant.X + 60), merchant.Y, merchant.Z, merchant.M));
			AmbientLife.Forget();
			bool quietAlone = AmbientLife.Barks(100) == 0;
			Near(player, merchant);

			//one bark per venue a tick: as many as venues with someone in reach
			HashSet<CityVenue> inReach = new HashSet<CityVenue>();
			foreach (Item gem in Residents.AllGems) {
				NPC npc = Residents.BodyOf(gem);
				CityVenue at = (npc == null) ? null : VenueRegistry.At(npc);
				if ((at != null) && (npc.M == player.M) && (Point2D.GetSimpleDistance(npc, player) <= VenueLifeSettings.instance.barkRange)) {
					inReach.Add(at);
				}
			}
			AmbientLife.Forget();
			int first = AmbientLife.Barks(100);
			string firstBy = AmbientLife.LastBarkBy;
			Report("oneBarkPerVenueOnlyNearAPlayer", quietAlone && (inReach.Count >= 1) && (first == inReach.Count));

			//the same resident doesn't bark again within barkSeconds
			bool again = true;
			for (int i = 0; i < 5; i++) {
				AmbientLife.Barks(100);
				again &= AmbientLife.LastBarkBy != firstBy || (AmbientLife.LastBarkText == null);
			}
			Report("aResidentBarksOnceInAWhile", again);

			//every bark is plain Czech for its role
			bool plain = true;
			foreach (Item gem in Residents.OfVenue(market.VenueId)) {
				for (int i = 0; i < 20; i++) {
					string line = AmbientLife.BarkFor(gem, market);
					plain &= (line != null) && IsPlain(line);
				}
			}
			Report("barksArePlainAscii", plain);
		}

		private static void Talks(Player player) {
			CityVenue market = Market(player);
			List<Item> merchants = Of(market, "merchant");
			NPC a = Residents.BodyOf(merchants[0]), b = Residents.BodyOf(merchants[1]);
			b.Go(new Point4D((ushort) (a.X + 1), a.Y, a.Z, a.M));
			Near(player, a);
			AmbientLife.Forget();
			ConversationTimer talk = AmbientLife.Start(merchants[0], merchants[1]);
			bool four = (talk != null) && (talk.line == 1) && talk.Step() && talk.Step() && !talk.Step() && (talk.line == 4);
			bool plain = (talk != null) && IsPlain(talk.lines) && (talk.lines.Split('\n').Length == 4);
			a.DeleteTimer(ConversationTimer.Key);
			bool pairRests = AmbientLife.Start(merchants[0], merchants[1]) == null;
			Report("aConversationIsFourLinesAndAPairRests", four && plain && pairRests);

			//a pass starts one talk in the venue, and no second one while it runs
			AmbientLife.Forget();
			int started = AmbientLife.Conversations(100);
			int runningAfterFirst = Talking(market);
			AmbientLife.Forget(); //pairs could talk again: only the running talk holds the venue back
			AmbientLife.Conversations(100);
			Report("oneTalkAtATimeInAVenue", (started >= 1) && (runningAfterFirst == 1) && (Talking(market) == 1));
			foreach (Item gem in Residents.OfVenue(market.VenueId)) {
				Residents.BodyOf(gem).DeleteTimer(ConversationTimer.Key);
			}
		}

		/// <summary>After the client's .vyvolat: the crier calls it, in turn with nothing else; a call every crierMinutes; news ages out.</summary>
		private static void Crier(Player player) {
			CityVenue market = Market(player);
			Item crierGem = Of(market, "crier")[0];
			NPC crier = Residents.BodyOf(crierGem);
			Near(player, crier);
			List<string> news = CityNews.Of("Edoras");
			bool gmNews = news.Contains("Na trhu se prodava zlato");
			string call = AmbientLife.CallFor(Residents.IdOf(crierGem), "Edoras");
			Report("theGmsNewsIsCalled", gmNews && (call == "Slyste, slyste! Na trhu se prodava zlato"));

			VenueLifeSettings.instance.crierMinutes = 5;
			AmbientLife.Forget();
			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			int firstCall = AmbientLife.Criers();
			int soon = AmbientLife.Criers();
			Errands.NowOverride = start.AddMinutes(6);
			int later = AmbientLife.Criers();
			Report("theCrierCallsEveryFewMinutes", (firstCall == 1) && (soon == 0) && (later == 1));

			Errands.NowOverride = start.AddHours(VenueLifeSettings.instance.newsHours + 1);
			string stale = AmbientLife.CallFor(Residents.IdOf(crierGem), "Edoras");
			Report("oldNewsGivesWayToMarketCries", (CityNews.Of("Edoras").Count == 0) && stale.StartsWith("Slyste, slyste!", StringComparison.Ordinal) && !stale.Contains("zlato"));
			Errands.NowOverride = null;
		}

		/// <summary>The real thing: barks on, the residents' scheduler (a 20 s tick) makes one by itself.</summary>
		[SteamFunction]
		public static void TestAmbientLive(Character self) {
			Player player = (Player) self;
			try {
				Near(player, Residents.BodyOf(Of(Market(player), "merchant")[0]));
				AmbientLife.Forget();
				VenueLifeSettings.instance.barkChance = 100;
			} finally {
				Console.WriteLine("AMBIENT_LIVE_DONE");
			}
		}

		[SteamFunction]
		public static void TestAmbientLiveState(Character self) {
			try {
				bool barked = AmbientLife.LastBarkText != null;
				Console.WriteLine("AMBIENT_LIVE: barked=" + (barked ? 1 : 0));
				if (barked) {
					Report("theSchedulerBarksByItself", true);
					VenueLifeSettings.instance.barkChance = 0;
				}
			} finally {
				Console.WriteLine("AMBIENT_LIVE_STATE");
			}
		}

		private static void Hero(Player player) {
			CityNews.Clear();
			int was = Reputation.Of(player, "Edoras");
			Reputation.Set(player, "Edoras", Reputation.Max - 5);
			player.RemoveTag(Reputation.CityDeedKey("Edoras")); //no deed of late: not yet a hero
			bool notYet = Reputation.Level(player, "Edoras") != ReputationLevel.Hrdina;
			Reputation.Change(player, "Edoras", 1);
			List<string> news = CityNews.Of("Edoras");
			bool told = (Reputation.Level(player, "Edoras") == ReputationLevel.Hrdina) && news.Exists(n => n.Contains(player.Name));
			Reputation.Change(player, "Edoras", 1);
			bool once = CityNews.Of("Edoras").Count == news.Count;
			Report("aNewHeroIsNewsOnce", notYet && told && once);
			Reputation.Set(player, "Edoras", was);
			CityNews.Clear();
		}
	}
}
