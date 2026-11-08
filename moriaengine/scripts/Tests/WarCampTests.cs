using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1280 - Barad-dur's war camp on the server: ranks and promotion, orders (shorter, capped by rank, refusing, giving up and
	/// running out cost standing, the rank's better share), and the rival captains (serving, their three orders, giving up, running
	/// out, switching sides, the weekly settlement, the cutthroats, the nemesis). Tests/Protocol/WarCampTests.cs asks a resident for an
	/// order and refuses it through a client first.</summary>
	public static class WarCampTests {
		private static readonly TagKey giverTK = TagKey.Acquire("_warcamptest_giver_");
		private static readonly TagKey otherTK = TagKey.Acquire("_warcamptest_other_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("WARCAMP_CHECK: " + name + "=" + ok);
		}

		internal sealed class SmallOrder : ErrandTemplate {
			public override string Key {
				get { return "test_order_small"; }
			}

			public override string Name {
				get { return "Maly rozkaz"; }
			}

			public override ErrandTier Tier {
				get { return ErrandTier.Small; }
			}

			public override bool TryBuild(ErrandContext context, Errand errand) {
				errand.offer = "Hlas se u me.";
				errand.steps.Add(Errands.Talk(context.GiverGem, "Hlas se u " + context.GiverName, "Hlasim se.", "Dobre."));
				return true;
			}
		}

		internal sealed class LargeOrder : ErrandTemplate {
			public override string Key {
				get { return "test_order_large"; }
			}

			public override string Name {
				get { return "Velky rozkaz"; }
			}

			public override ErrandTier Tier {
				get { return ErrandTier.Large; }
			}

			public override bool TryBuild(ErrandContext context, Errand errand) {
				errand.offer = "Velka vec.";
				errand.steps.Add(Errands.Talk(context.GiverGem, "Hlas se u " + context.GiverName, "Hlasim se.", "Dobre."));
				return true;
			}
		}

		/// <summary>A mess hall in Barad-dur with its cook and grunts, only the two test orders; a Mordor soldier beside the cook.</summary>
		[SteamFunction]
		public static void TestWarCampSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				StaticRegion area = StaticRegion.GetByDefname("a_Barad_dur");
				ImmutableRectangle rect = area.Rectangles[0];
				Map map = Map.GetMap(area.Mapplane);
				int[] spot = WorldSpots.NearestStandable(map, rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2), 20);
				VenueDraft draft = new VenueDraft();
				draft.SetType(VenueType.Get("messhall").IndexOf());
				draft.M = (byte) area.Mapplane;
				draft.City = WarCamp.City;
				draft.Rects.Add(new ImmutableRectangle(spot[0] - 3, spot[1] - 3, spot[0] + 3, spot[1] + 3));
				string problem;
				CityVenue messhall = draft.Save(out problem);
				if (messhall == null) {
					throw new SEException("the mess hall wasn't saved: " + problem);
				}
				Residents.FillRoles(messhall);
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new SmallOrder());
				List<Item> people = Residents.OfVenue(messhall.VenueId);
				foreach (Item gem in people) {
					NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
				}
				Item giver = people[0];
				player.SetTag(giverTK, giver);
				player.SetTag(otherTK, people[1]);
				NPC body = Residents.BodyOf(giver);
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				player.Realm = Realm.Mordor;
				player.RemoveTag(Errands.LogTK);
				Reputation.Set(player, WarCamp.City, 0);
				Console.WriteLine("WARCAMP_SETUP: giver=" + body.Uid);
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("WARCAMP_SETUP_DONE");
			}
		}

		[SteamFunction]
		public static void TestWarCampState(Character self) {
			Console.WriteLine("WARCAMP_STATE: rep=" + Reputation.Of(self, WarCamp.City));
		}

		[SteamFunction]
		public static void TestWarCampChain(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //GMs pay nothing
			Point4D start = new Point4D(player.X, player.Y, player.Z, player.M);
			try {
				Orders(player);
				Captains(player);
			} finally {
				player.Account.PLevel = plevel;
				RivalCaptains.clock = null;
				Reputation.NowOverride = null;
				player.Go(start);
			}
			Console.WriteLine("WARCAMP_CHAIN_DONE");
		}

		private static void Orders(Player player) {
			Item giver = (Item) player.GetTag(giverTK);
			Report("theClientRefusedAnOrder", Reputation.Of(player, WarCamp.City) == -WarCampSettings.instance.orderRefuseLoss);

			//the rank is the camp's reading of Barad-dur's reputation
			int[] values = { -700, -200, -50, 0, 300, 500 };
			CampRank[] ranks = { CampRank.Zradce, CampRank.Cerv, CampRank.Rekrut, CampRank.Vojak, CampRank.Desatnik, CampRank.Setnik };
			bool read = true;
			for (int i = 0; i < values.Length; i++) {
				Reputation.Set(player, WarCamp.City, values[i]);
				read &= WarCamp.RankOf(player) == ranks[i];
			}
			Report("ranks", read && (Reputation.CityLevelName(player, WarCamp.City) == "Setnik") && (Reputation.CityLevelName(player, "Edoras") == Reputation.LevelName(Reputation.Level(player, "Edoras"))));

			//a promotion pays once per rank (#1296: the friend deed's pay off, Desatnik is respect enough for its first tier)
			int deedGold = DeedSettings.instance.gold;
			DeedSettings.instance.gold = 0;
			player.RemoveTag(WarCamp.TopRankTK);
			Reputation.Set(player, WarCamp.City, 0);
			Reputation.Change(player, WarCamp.City, 1); //the first change remembers Vojak as the best so far
			long before = GoldUtils.CountGoldOn(player);
			Reputation.Change(player, WarCamp.City, 250); //Desatnik
			long paid = GoldUtils.CountGoldOn(player) - before;
			Reputation.Change(player, WarCamp.City, -150);
			before = GoldUtils.CountGoldOn(player);
			Reputation.Change(player, WarCamp.City, 150); //Desatnik again: no second pay
			DeedSettings.instance.gold = deedGold;
			Report("promotionPaysOnce", (paid == 4 * WarCampSettings.instance.promotionGold) && (GoldUtils.CountGoldOn(player) == before));

			//the rank caps the order's tier
			ErrandTemplates.Register(new LargeOrder());
			Reputation.Set(player, WarCamp.City, 0);
			bool noLarge = true;
			for (int i = 0; i < 30; i++) {
				Errand made = Errands.Make(player, giver);
				noLarge &= (made != null) && (made.Tier != ErrandTier.Large);
			}
			Reputation.Set(player, WarCamp.City, 500);
			bool someLarge = false;
			for (int i = 0; i < 30; i++) {
				Errand made = Errands.Make(player, giver);
				someLarge |= (made != null) && (made.Tier == ErrandTier.Large);
			}
			ErrandTemplates.Unregister("test_order_large");
			Report("rankCapsTheTier", noLarge && someLarge && !WarCamp.AboveRank(player, "Edoras", ErrandTier.Large));

			//shorter deadlines, the rank's better share; Edoras unchanged
			Errand order = Errands.Make(player, giver);
			bool taken = Errands.Accept(player, order);
			double minutes = (order.Expires - Errands.Now).TotalMinutes;
			Report("shorterAndBetterPaid", taken && (minutes > 28) && (minutes < 31) && (WarCamp.PayShare(player, WarCamp.City, 100) == 120)
				&& (WarCamp.PayShare(player, "Edoras", 100) == 100) && (WarCamp.Minutes("Edoras", 60) == 60));

			//giving it up costs standing; so does running out
			int rep = Reputation.Of(player, WarCamp.City);
			Errands.GiveUp(player, order);
			bool gaveUp = Reputation.Of(player, WarCamp.City) == rep - WarCampSettings.instance.orderFailLoss;
			Errand second = Errands.Make(player, (Item) player.GetTag(otherTK)); //one a day from a giver: the second from another
			bool takenAgain = (second != null) && Errands.Accept(player, second);
			rep = Reputation.Of(player, WarCamp.City);
			if (takenAgain) {
				second.expiresTicks = Errands.Now.AddMinutes(-1).Ticks;
			}
			Errands.Log(player);
			Report("failingCostsStanding", gaveUp && takenAgain && (Reputation.Of(player, WarCamp.City) == rep - WarCampSettings.instance.orderFailLoss));
		}

		private static NPC Captain(Player player, Captain captain, int dx) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname(captain.Def)).Create(player.X + dx, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(npc, 900);
			return npc;
		}

		private static void Captains(Player player) {
			Captain gorbag = RivalCaptains.All[0], shagrat = RivalCaptains.All[1];
			NPC gorbagNpc = Captain(player, gorbag, 1), shagratNpc = Captain(player, shagrat, 2);
			D_WarCaptain dialog = SingletonScript<D_WarCaptain>.Instance;
			player.RemoveTag(RivalCaptains.SideTK);
			player.RemoveTag(RivalCaptains.BetrayedTK);
			player.RemoveTag(RivalCaptains.OrderTK);
			player.RemoveTag(RivalCaptains.NemesisTK);
			RivalCaptains.State(gorbag).power = 2;
			RivalCaptains.State(shagrat).power = 2;
			RivalCaptains.State(gorbag).points = 0;
			RivalCaptains.State(shagrat).points = 0;
			Reputation.Set(player, WarCamp.City, 0);

			//only Mordor's own
			player.Realm = Realm.Gondor;
			bool gondorTurned = RivalCaptains.Refusal(player) != null;
			player.Realm = Realm.Mordor;
			Report("mordorOnly", gondorTurned && (RivalCaptains.Refusal(player) == null) && RivalCaptains.Of(gorbagNpc) == gorbag);

			//serving Gorbag raises his band and lowers Shagrat's by half
			int gBefore = Reputation.OfFaction(player, gorbag.Faction), sBefore = Reputation.OfFaction(player, shagrat.Faction);
			bool notOfferedYet = !dialog.BuildFor(gorbagNpc, player).HasResponse("captain_order");
			dialog.Press("captain_order", gorbagNpc, player); //not his man yet: nothing
			bool noOrder = RivalCaptains.PeekOrder(player) == null;
			dialog.Press("captain_serve", gorbagNpc, player);
			int favour = WarCampSettings.instance.serveFavour;
			Report("serving", notOfferedYet && noOrder && (RivalCaptains.SideOf(player) == gorbag) && (Reputation.OfFaction(player, gorbag.Faction) == gBefore + favour)
				&& (Reputation.OfFaction(player, shagrat.Faction) == sBefore - (favour / 2)) && RivalCaptains.ServingLine(player, WarCamp.City).Contains("Gorbag")
				&& dialog.BuildFor(gorbagNpc, player).HasResponse("captain_order"));

			//the three orders
			int saved = WarCampSettings.instance.cutthroatChance;
			WarCampSettings.instance.cutthroatChance = 0;
			try {
				Report("ironOrder", IronOrder(player, gorbag, gorbagNpc, dialog));
				Report("tributeOrder", TributeOrder(player, gorbag));
				Report("headsOrder", HeadsOrder(player, gorbag));

				//giving up and running out cost his favour
				int g = Reputation.OfFaction(player, gorbag.Faction);
				RivalCaptains.Give(player, gorbag, 70);
				dialog.Press("captain_giveup", gorbagNpc, player);
				bool gaveUp = (RivalCaptains.PeekOrder(player) == null) && (Reputation.OfFaction(player, gorbag.Faction) == g - WarCampSettings.instance.captainOrderFavour);
				RivalCaptains.Give(player, gorbag, 70);
				RivalCaptains.clock = DateTime.UtcNow.AddMinutes(WarCampSettings.instance.captainOrderMinutes + 1);
				g = Reputation.OfFaction(player, gorbag.Faction);
				bool ranOut = (RivalCaptains.CurrentOrder(player) == null) && (Reputation.OfFaction(player, gorbag.Faction) == g - WarCampSettings.instance.captainOrderFavour);
				RivalCaptains.clock = null;
				//a pause after each order: none offered, none given
				bool cooling = (RivalCaptains.CooldownLeft(player) > 0) && !dialog.BuildFor(gorbagNpc, player).HasResponse("captain_order");
				dialog.Press("captain_order", gorbagNpc, player);
				cooling &= RivalCaptains.PeekOrder(player) == null;
				player.RemoveTag(RivalCaptains.LastOrderEndTK);
				Report("failingHisOrders", gaveUp && ranOut && cooling);
			} finally {
				WarCampSettings.instance.cutthroatChance = saved;
			}

			//switching to Shagrat with Gorbag's order held: the order fails, the switch costs, Gorbag remembers
			WarCampSettings.instance.cutthroatChance = 0;
			RivalCaptains.Give(player, gorbag, 70);
			WarCampSettings.instance.cutthroatChance = saved;
			int city = Reputation.Of(player, WarCamp.City);
			dialog.Press("captain_serve", shagratNpc, player);
			int cost = WarCampSettings.instance.serveFavour + WarCampSettings.instance.captainOrderFavour;
			bool switched = (RivalCaptains.SideOf(player) == shagrat) && (RivalCaptains.PeekOrder(player) == null) && (Reputation.Of(player, WarCamp.City) == city - cost)
				&& RivalCaptains.Greeting(gorbag, player).StartsWith("Zradce") && RivalCaptains.Greeting(shagrat, player).StartsWith("Muj vojak");
			//back and forth only ever loses standing
			city = Reputation.Of(player, WarCamp.City);
			dialog.Press("captain_serve", gorbagNpc, player);
			dialog.Press("captain_serve", shagratNpc, player);
			Report("switching", switched && (Reputation.Of(player, WarCamp.City) == city - (2 * WarCampSettings.instance.serveFavour)));

			//the weekly settlement: more done orders wins
			CaptainState gs = RivalCaptains.State(gorbag), ss = RivalCaptains.State(shagrat);
			gs.points = 3;
			ss.points = 1;
			int gWins = gs.wins, sLosses = ss.losses;
			RivalCaptains.SetWeekStart(DateTime.UtcNow.AddDays(-1));
			RivalCaptains.Settle();
			bool notYet = (gs.power == 2) && (gs.points == 3);
			RivalCaptains.SetWeekStart(DateTime.UtcNow.AddDays(-(WarCampSettings.instance.challengeDays + 1)));
			RivalCaptains.Settle();
			Report("weeklySettlement", notYet && (gs.power == 3) && (ss.power == 1) && (gs.points == 0) && (ss.points == 0) && (gs.wins == gWins + 1) && (ss.losses == sLosses + 1));

			//Gorbag's cutthroats come for Shagrat's man; not for a Gondor hero
			player.Realm = Realm.Gondor;
			player.SetTag(RivalCaptains.CutthroatsFromTK, gorbag.Key);
			bool noneForGondor = RivalCaptains.Ambush(player).Count == 0;
			player.Realm = Realm.Mordor;
			player.Flag_Invulnerable = true;
			player.SetTag(RivalCaptains.CutthroatsFromTK, gorbag.Key);
			noneForGondor &= RivalCaptains.Ambush(player).Count == 0; //nor for an invulnerable hero
			player.Flag_Invulnerable = false;
			player.SetTag(RivalCaptains.CutthroatsFromTK, shagrat.Key);
			noneForGondor &= RivalCaptains.Ambush(player).Count == 0; //nor from the captain the hero serves now
			player.SetTag(RivalCaptains.CutthroatsFromTK, gorbag.Key);
			List<NPC> came = RivalCaptains.Ambush(player);
			bool hunting = came.Count == WarCampSettings.instance.cutthroatCount;
			foreach (NPC cutthroat in came) {
				NPCAiTimer ai = NPCAiTimer.GetFor(cutthroat);
				hunting &= cutthroat.Name.Contains("Gorbag") && ((cutthroat.GetTag(RivalCaptains.CutthroatOfTK) as string) == gorbag.Key)
					&& (ai != null) && (ai.Target == player) && (cutthroat.GetTimer(CutthroatLeaveTimer.Key) != null)
					&& (cutthroat.AggroMode == AggroMode.Calm) && (cutthroat.GetTag(RivalCaptains.CutthroatTargetTK) == player) && !RivalCaptains.ShouldLeave(cutthroat);
			}
			bool leaves = false;
			if (came.Count > 1) {
				came[1].SetTag(RivalCaptains.CutthroatUntilTK, Globals.TimeInSeconds - 1);
				leaves = RivalCaptains.ShouldLeave(came[1]) && !RivalCaptains.ShouldLeave(came[0]);
				came[1].SetTag(RivalCaptains.CutthroatUntilTK, Globals.TimeInSeconds + 300);
			}
			Report("cutthroats", noneForGondor && hunting && leaves);

			//killed by them: Gorbag rises and taunts, once an ambush, they leave; a bystander's death counts nothing
			int power = gs.power, wins = gs.wins;
			Player bystander = (Player) ThingDef.FindCharDef(0x190).Create(player.X + 1, player.Y + 1, player.Z, player.M);
			RivalCaptains.OnPlayerDeath(bystander, came.Count > 0 ? came[0] : null);
			bool notBystanders = gs.power == power;
			bystander.Delete();
			RivalCaptains.OnPlayerDeath(player, came.Count > 0 ? came[0] : null);
			bool gone = came.TrueForAll(c => c.IsDeleted);
			RivalCaptains.OnPlayerDeath(player, came.Count > 1 ? came[1] : null); //a second death by the same ambush
			Report("nemesis", notBystanders && gone && (came.Count > 0) && (gs.power == power + 1) && (gs.wins == wins)
				&& ((player.GetTag(RivalCaptains.NemesisTK) as string) == gorbag.Key) && RivalCaptains.Greeting(gorbag, player).StartsWith("Zase ty"));
			foreach (NPC cutthroat in came) {
				if (!cutthroat.IsDeleted) {
					cutthroat.Delete();
				}
			}
			gorbagNpc.Delete();
			shagratNpc.Delete();
		}

		private static bool IronOrder(Player player, Captain gorbag, NPC gorbagNpc, D_WarCaptain dialog) {
			RivalCaptains.Give(player, gorbag, 70);
			CaptainOrder order = RivalCaptains.PeekOrder(player);
			bool sized = (order != null) && (order.Kind == CaptainOrderKind.Iron) && (order.Count == WarCampSettings.instance.ironBase + (2 * WarCampSettings.instance.ironPerPower));
			bool notReady = !dialog.BuildFor(gorbagNpc, player).HasResponse("captain_done");
			dialog.Press("captain_done", gorbagNpc, player);
			notReady &= RivalCaptains.PeekOrder(player) != null;
			Item iron = (Item) ((ItemDef) ThingDef.GetByDefname(RivalCaptains.IronDef)).Create(player.Backpack);
			iron.Amount = order.Count;
			int points = RivalCaptains.State(gorbag).points, favour = Reputation.OfFaction(player, gorbag.Faction);
			long gold = GoldUtils.CountGoldOn(player);
			bool ready = dialog.BuildFor(gorbagNpc, player).HasResponse("captain_done");
			dialog.Press("captain_done", gorbagNpc, player);
			return sized && notReady && ready && (RivalCaptains.PeekOrder(player) == null) && iron.IsDeleted && (RivalCaptains.State(gorbag).points == points + 1)
				&& (Reputation.OfFaction(player, gorbag.Faction) == favour + WarCampSettings.instance.captainOrderFavour)
				&& (GoldUtils.CountGoldOn(player) == gold + (2 * WarCampSettings.instance.captainOrderGold));
		}

		private static bool TributeOrder(Player player, Captain gorbag) {
			long own = GoldUtils.CountGoldOn(player);
			if (own > 0) {
				GoldUtils.Pay(player, own, PaySource.Usual, "");
			}
			RivalCaptains.Give(player, gorbag, 30);
			CaptainOrder order = RivalCaptains.PeekOrder(player);
			bool sized = (order != null) && (order.Kind == CaptainOrderKind.Tribute) && (order.Count == 2 * WarCampSettings.instance.tributePerPower);
			bool notWithout = !RivalCaptains.Finish(player, order);
			GoldUtils.Grant(player, (uint) order.Count);
			bool done = RivalCaptains.Finish(player, order);
			return sized && notWithout && done && (GoldUtils.CountGoldOn(player) == 0); //a tribute isn't paid back in gold
		}

		private static bool HeadsOrder(Player player, Captain gorbag) {
			RivalCaptains.Give(player, gorbag, 0);
			CaptainOrder order = RivalCaptains.PeekOrder(player);
			Player enemy = (Player) ThingDef.FindCharDef(0x190).Create(player.X + 1, player.Y + 1, player.Z, player.M);
			Item own = (Item) ((ItemDef) ThingDef.GetByDefname("i_flesh_head")).Create(player.Backpack);
			own.SetTag(CorpseCarving.HeadOfTK, player); //the hero's own head doesn't count
			Item head = (Item) ((ItemDef) ThingDef.GetByDefname("i_flesh_head")).Create(player.Backpack);
			head.SetTag(CorpseCarving.HeadOfTK, enemy);
			enemy.Realm = Realm.Mordor;
			bool notAMordorHead = !RivalCaptains.CanFinish(player, order);
			enemy.Realm = Realm.Gondor;
			bool done = RivalCaptains.Finish(player, order);
			bool result = (order.Kind == CaptainOrderKind.Heads) && (order.Count == 1) && notAMordorHead && done && head.IsDeleted && !own.IsDeleted;
			own.Delete();
			enemy.Delete();
			return result;
		}

		/// <summary>The cutthroats' timer: arms with a delay, the protocol test waits for the ambush.</summary>
		[SteamFunction]
		public static void TestWarCampCutthroatTimer(Character self) {
			Player player = (Player) self;
			player.Realm = Realm.Mordor;
			RivalCaptains.SendCutthroats(player, RivalCaptains.All[0], 1); //Gorbag's: the chain left the hero Shagrat's man
			Console.WriteLine("WARCAMP_TIMER_ARMED");
		}

		[SteamFunction]
		public static void TestWarCampCutthroatReport(Character self) {
			int count = 0;
			foreach (Character ch in self.GetMap().GetCharsInRange(self.X, self.Y, 8)) {
				if ((ch.GetTag(RivalCaptains.CutthroatOfTK) as string) == RivalCaptains.All[0].Key) {
					count++;
					ch.Delete();
				}
			}
			Console.WriteLine("WARCAMP_CUTTHROATS: count=" + count);
		}
	}
}
