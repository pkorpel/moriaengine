using System;
using System.Collections.Generic;
using SteamEngine.Common;

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
			Console.WriteLine("WORLDEVENTS_STATE: running=" + running.Count + ((running.Count > 0) ? " kind=" + running[0].Kind.Key + " city=" + running[0].City : ""));
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

				//the rules: a raid only on Gondor's cities and at dusk, its size by the players there
				VenueClock.Override = 18;
				bool dusk = raidKind.IsTime();
				VenueClock.Override = 12;
				bool noon = raidKind.IsTime();
				VenueClock.Override = null;
				Report("raidRules", raidKind.IsFor("Edoras") && raidKind.IsFor("Hobitin") && !raidKind.IsFor("Barad-dur") && !raidKind.IsFor("Harad")
					&& dusk && !noon && (OrcRaid.SizeFor(0) == s.raidBase) && (OrcRaid.SizeFor(2) == s.raidBase + (2 * s.raidPerPlayer))
					&& (OrcRaid.SizeFor(100) == s.raidMax) && caravanKind.IsFor("Umbar") && caravanKind.IsFor("Barad-dur")
					&& (Caravan.BanditsFor(1) == s.banditBase + s.banditPerPlayer));

				//the raid the client's .udalost started: a GM isn't counted, so the smallest warband, a chief first, making for the centre
				Point4D edoras = WorldEvents.CentreOf("Edoras");
				OrcRaid raid = (WorldEvents.Running.Count == 1) ? WorldEvents.Running[0] as OrcRaid : null;
				bool started = (raid != null) && (raid.City == "Edoras") && (raid.Npcs.Count == OrcRaid.SizeFor(0))
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
					&& (raid.Heroes[player] == OrcRaid.SizeFor(0));
				Report("raidWon", won && (player.Gold - gold == s.raidGold + (s.raidKillGold * OrcRaid.SizeFor(0)))
					&& (Reputation.Of(player, "Edoras") > standing) && (WorldEvents.PricePercent("Edoras") == s.raidWonPercent)
					&& CityNews.Of("Edoras").Exists(t => t.Contains("odrazili")) && WorldEvents.RecentOutcomes().Exists(t => t.Contains("odrazili")));

				//lost: time runs out, the raiders leave and Minas Tirith's prices rise (through Reputation.PriceFor)
				WorldEvents.ResetForTests();
				GoTo(player, WorldEvents.CentreOf("Minas_Tirith"));
				long before = Reputation.PriceFor(player, 1000);
				WorldEvent lost = WorldEvents.Start(raidKind, "Minas_Tirith");
				bool lostStarted = (lost != null) && (lost.Npcs.Count == OrcRaid.SizeFor(1));
				if (lost != null) {
					lost.EndsAt = Globals.TimeInSeconds;
				}
				WorldEvents.Tick();
				Report("raidLost", lostStarted && lost.Ended && !lost.Won && lost.Npcs.TrueForAll(n => n.IsDeleted)
					&& (WorldEvents.PricePercent("Minas_Tirith") == s.raidLostPercent)
					&& (Reputation.PriceFor(player, 1000) == (before * (100 + s.raidLostPercent) + 50) / 100)
					&& CityNews.Of("Minas_Tirith").Exists(t => t.Contains("vyplenili")));

				//a GM's start refuses a city the kind isn't for, and a second event in a busy city
				Report("startRefused", (WorldEvents.Start(raidKind, "Harad") == null) && (WorldEvents.Start(caravanKind, "Minas_Tirith") != null)
					&& (WorldEvents.Start(raidKind, "Minas_Tirith") == null));
				WorldEvents.ResetForTests();

				//the caravan: merchant and pack animals on the road, bandits halfway, the merchant waits out the fight, arrives, escorts paid
				GoTo(player, edoras);
				Caravan caravan = WorldEvents.Start(caravanKind, "Edoras") as Caravan;
				bool onRoad = (caravan != null) && (caravan.Merchant != null) && (caravan.Merchant.PetGoalPoint != null)
					&& (caravan.Npcs.FindAll(n => n.Def == ThingDef.GetByDefname(CaravanKind.PackDef)).TrueForAll(n => n.FollowTarget == caravan.Merchant))
					&& (caravan.Npcs.Count == 1 + s.caravanPack) && (Point2D.GetSimpleDistance(caravan.Merchant, edoras) > Caravan.ArrivedWithin);
				bool ambushed = false, waited = false;
				if (caravan != null) {
					caravan.Ambush();
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
					KillAll(caravan, player, n => n.Def == ThingDef.GetByDefname(CaravanKind.BanditDef));
				}
				Report("caravanAmbush", onRoad && ambushed && waited);
				gold = player.Gold;
				if (caravan != null) {
					GoTo(player, edoras);
					caravan.Merchant.Go(edoras);
				}
				WorldEvents.Tick();
				Report("caravanArrived", (caravan != null) && caravan.Ended && caravan.Won && (player.Gold - gold == s.caravanGold)
					&& (WorldEvents.PricePercent("Edoras") == s.caravanPercent) && CityNews.Of("Edoras").Exists(t => t.Contains("dorazila")));

				//lost: the merchant dies, the caravan goes, nothing gets cheaper
				WorldEvents.ResetForTests();
				Caravan doomed = WorldEvents.Start(caravanKind, "Hobitin") as Caravan;
				if (doomed != null) {
					doomed.Merchant.CauseDeath(null);
				}
				WorldEvents.Tick();
				Report("caravanLost", (doomed != null) && doomed.Ended && !doomed.Won && doomed.Npcs.TrueForAll(n => n.IsDeleted)
					&& (WorldEvents.PricePercent("Hobitin") == 0) && CityNews.Of("Hobitin").Exists(t => t.Contains("ztratila")));

				//after a restart: an event's NPC with no event under way goes
				WorldEvents.ResetForTests();
				NPC stray = (NPC) ((NPCDef) ThingDef.GetByDefname(OrcRaidKind.RaiderDef)).Create(edoras.X + 2, edoras.Y, edoras.Z, edoras.M);
				stray.SetTag(WorldEvent.EventTK, "najezd/Edoras/999999");
				Report("leftoversCleared", (WorldEvents.ClearLeftovers() >= 1) && stray.IsDeleted);

				//the scheduler: off starts nothing; on, with the hour not dusk, only a caravan can start where a player is
				GoTo(player, edoras);
				s.enabled = false;
				s.startChance = 100;
				WorldEvents.Tick();
				bool offQuiet = WorldEvents.Running.Count == 0;
				s.enabled = true;
				VenueClock.Override = 12;
				WorldEvents.Tick();
				VenueClock.Override = null;
				bool onStarts = (WorldEvents.Running.Count == 1) && (WorldEvents.Running[0].Kind == caravanKind);
				WorldEvents.Tick();
				bool oneAtATime = WorldEvents.Running.Count == 1;
				Report("scheduler", offQuiet && onStarts && oneAtATime);
				WorldEvents.ResetForTests();
			} finally {
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
