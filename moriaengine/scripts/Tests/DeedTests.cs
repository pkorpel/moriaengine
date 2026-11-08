using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1296 - deeds and titles on the server: real kills, an errand, cities walked into, a troll beaten, a festival, the
	/// city's respect; tiers paid once each. Tests/Protocol/DeedTests.cs reads the journal's Skutky tab through a client.</summary>
	public static class DeedTests {
		private const string MonsterDef = "c_orc";
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>(); //put back when the chain ends

		private static void Report(string name, bool ok) {
			Console.WriteLine("DEEDS_CHECK: " + name + "=" + ok);
		}

		/// <summary>A one-step errand: say a word to the giver.</summary>
		private sealed class WordErrand : ErrandTemplate {
			public override string Key {
				get { return "test_deed_word"; }
			}

			public override string Name {
				get { return "Slovo"; }
			}

			public override ErrandTier Tier {
				get { return ErrandTier.Small; }
			}

			public override bool TryBuild(ErrandContext context, Errand errand) {
				errand.offer = "Rekni mi slovo.";
				errand.steps.Add(Errands.Talk(context.GiverGem, "Rekni slovo", "Slovo.", "Dekuji."));
				return true;
			}
		}

		private static NPC Monster(Player player) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname(MonsterDef)).Create(player.X, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(npc, 60);
			npc.ExperienceReward = 50;
			return npc;
		}

		private static int Count(Player player, string key) {
			return Deeds.Count(player, Deeds.Get(key));
		}

		private static int Tier(Player player, string key) {
			return Deeds.Tier(player, Deeds.Get(key));
		}

		/// <summary>Clean deeds, a plain player; the chain's checks, then the journal is the client's.</summary>
		[SteamFunction]
		public static void TestDeedsChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			DeedSettings s = DeedSettings.instance;
			int sGold = s.gold, sExp = s.exp, sGrowth = s.growth;
			Errands.NowOverride = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc); //no festival's day: the calendar mustn't start one
			try {
				s.gold = 300;
				s.exp = 500;
				s.growth = 3;
				WorldEvents.ResetForTests();
				Deeds.ForgetForTests(player);
				account.PLevel = 1;
				Chain(player);
			} finally {
				s.gold = sGold;
				s.exp = sExp;
				s.growth = sGrowth;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				ErrandTemplates.Unregister("test_deed_word");
				removed.Clear();
				VenueClock.Override = null;
				Errands.NowOverride = null;
				WorldEvents.ResetForTests();
				account.PLevel = plevel;
				Console.WriteLine("DEEDS_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			Point4D edoras = WorldEvents.CentreOf("Edoras");
			player.Go(edoras);

			//the slayer: nine kills pay nothing, the tenth reaches the first tier and its title
			long gold = player.Gold;
			for (int i = 0; i < 9; i++) {
				Monster(player).CauseDeath(player);
			}
			bool nine = (Count(player, Deeds.Slayer) == 9) && (Tier(player, Deeds.Slayer) == 0) && (player.Gold == gold);
			int exp = player.Experience, level = player.Level;
			Monster(player).CauseDeath(player);
			Report("slayerCounts", nine && (Count(player, Deeds.Slayer) == 10));
			Report("aTierPaysAndNames", (Tier(player, Deeds.Slayer) == 1) && (player.Gold - gold == 300) && ((player.Experience > exp) || (player.Level > level))
				&& (Deeds.Title(player, Deeds.Get(Deeds.Slayer)) == "Lovec zverat"));

			//not counted: someone's pet (one's own turns on its owner when hit, and is wild again), a monster worth nothing, a death
			//nobody caused
			NPC owner = Monster(player);
			NPC pet = Monster(player);
			pet.ControlMaster = owner;
			pet.CauseDeath(player);
			owner.Delete();
			NPC worthless = Monster(player);
			worthless.ExperienceReward = 0;
			worthless.CauseDeath(player);
			Monster(player).CauseDeath(null);
			Report("petsAndWorthlessDontCount", Count(player, Deeds.Slayer) == 10);

			//a GM's kill
			player.Account.PLevel = 4;
			Monster(player).CauseDeath(player);
			player.Account.PLevel = 1;
			Report("aGmDoesNoDeeds", Count(player, Deeds.Slayer) == 10);

			//a tier is paid once; a jump past two tiers pays both, each growth times the one before
			gold = player.Gold;
			Deeds.Check(player, Deeds.Get(Deeds.Slayer));
			bool once = player.Gold == gold;
			Deeds.Add(player, Deeds.Slayer, 990);
			Report("eachTierOnce", once && (Tier(player, Deeds.Slayer) == 3) && (player.Gold - gold == (300 * 3) + (300 * 9))
				&& (Deeds.Title(player, Deeds.Get(Deeds.Slayer)) == "Postrach divociny"));

			//the courier: a done errand
			Courier(player, edoras);

			//the explorer: three cities, each once however often walked into
			player.Go(WorldEvents.CentreOf("Minas_Tirith"));
			player.Go(edoras);
			player.Go(WorldEvents.CentreOf("Hobitin"));
			player.Go(WorldEvents.CentreOf("Minas_Tirith"));
			player.Go(edoras);
			Report("citiesCountOnce", (Count(player, Deeds.Explorer) == 3) && (Tier(player, Deeds.Explorer) == 1));

			//the defender: a troll beaten at night; the festival's deed doesn't move
			VenueClock.Override = 23;
			Troll troll = WorldEvents.Start(WorldEvents.KindOf("trol"), "Edoras") as Troll;
			if (troll != null) {
				player.Go(new Point4D(troll.Beast.X, troll.Beast.Y, troll.Beast.Z, troll.Beast.M));
				troll.Beast.CauseDeath(player);
				WorldEvents.Tick();
			}
			VenueClock.Override = null;
			Report("anEventWonCounts", (troll != null) && troll.Won && (Count(player, Deeds.Defender) == 1) && (Tier(player, Deeds.Defender) == 1)
				&& (Count(player, Deeds.Festival) == 0));

			//an enemy of the city does no deed by it
			Realm realm = player.Realm;
			player.Realm = Realm.Mordor;
			WorldEvents.Reward(player, "Edoras", 0, 0, 0);
			player.Realm = realm;
			Report("anEnemyDoesNone", Count(player, Deeds.Defender) == 1);

			//the festival: a guest counts at its end, once; the animal caught is no defender's deed
			WorldEvents.ResetForTests();
			Festival feast = WorldEvents.Start(WorldEvents.KindOf("slunovrat"), "Minas_Tirith") as Festival;
			if ((feast != null) && (feast.Animal != null)) {
				player.Go(new Point4D(feast.Animal.X, feast.Animal.Y, feast.Animal.Z, feast.Animal.M));
				feast.Catch(player);
				feast.EndsAt = Globals.TimeInSeconds;
				WorldEvents.Tick();
			}
			Report("aFestivalCounts", (feast != null) && feast.Ended && (Count(player, Deeds.Festival) == 1) && (Count(player, Deeds.Defender) == 1));

			//friend of the cities: worked out from the city's respect, paid when it's reached
			ReputationSettings rs = ReputationSettings.instance;
			gold = player.Gold;
			Reputation.Set(player, "Edoras", rs.respectedFrom - 1);
			bool notYet = Count(player, Deeds.Friend) == 0;
			Reputation.Change(player, "Edoras", 5);
			Report("respectMakesAFriend", notYet && (Count(player, Deeds.Friend) == 1) && (Tier(player, Deeds.Friend) == 1) && (player.Gold - gold == 300));
		}

		private static void Courier(Player player, Point4D edoras) {
			Map map = Map.GetMap(edoras.M);
			int[] spot = null;
			foreach (int[] tile in WorldEvents.Reachable(map, new[] { edoras.X, edoras.Y, edoras.Z }, 20)) {
				if (WorldSpots.Distance(tile, edoras.X, edoras.Y) >= 4) {
					spot = tile;
					break;
				}
			}
			bool done = false;
			if (spot != null) {
				byte plevel = player.Account.PLevel;
				player.Account.PLevel = 7;
				VenueDraft draft = new VenueDraft();
				draft.SetType(VenueType.Get("tavern").IndexOf());
				draft.M = edoras.M;
				draft.City = "Edoras";
				draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
				CityVenue venue = draft.Save(out string problem);
				player.Account.PLevel = plevel;
				if (venue == null) {
					throw new SEException("The test tavern wasn't saved: " + problem);
				}
				Residents.FillRoles(venue);
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new WordErrand());
				player.RemoveTag(Errands.LogTK);
				Item giver = Residents.OfVenue(venue.VenueId)[0];
				NPC body = Residents.BodyOf(giver);
				NPCAiTimer.Freeze(body, 120);
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				Errand errand = Errands.Make(player, giver);
				done = (errand != null) && Errands.Accept(player, errand) && Errands.Do(player, errand, body);
			}
			Report("anErrandCounts", done && (Count(player, Deeds.Courier) == 1) && (Tier(player, Deeds.Courier) == 0));
		}
	}
}
