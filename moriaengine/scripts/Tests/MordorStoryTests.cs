using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1302 - Mordor's stories on the server: the pits of Nurn, the Eye's spies, the Mouth's dispatches.
	/// Tests/Protocol/MordorStoryTests.cs takes a dispatch through a client and puts it in the tower's box with a double click first.</summary>
	public static class MordorStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("MORDORSTORIES_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_mordorstorytest_" + key + "_");
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

		private static Item Gem(Player player, string key, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(key));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if ((role == null) || (Residents.RoleKeyOf(gem) == role)) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>An overseer's post in Nurn; an overseer's post, a mess hall and a forge in the war camp; only the dispatches
		/// offered; the hero beside the camp's overseer.</summary>
		[SteamFunction]
		public static void TestMordorStoriesSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D nurn = WorldEvents.CentreOf("Nurn"), camp = WorldEvents.CentreOf("Barad-dur");
				List<int[]> n = Spots(nurn, 2);
				player.SetTag(VenueKey("nurn"), OpenAir("Nurn", "overseer", n[0], nurn.M));
				player.SetTag(VenueKey("nurnmess"), OpenAir("Nurn", "messhall", n[1], nurn.M)); //a Nurn giver who isn't an overseer
				List<int[]> c = Spots(camp, 3);
				player.SetTag(VenueKey("campoverseer"), OpenAir("Barad-dur", "overseer", c[0], camp.M));
				player.SetTag(VenueKey("messhall"), OpenAir("Barad-dur", "messhall", c[1], camp.M));
				player.SetTag(VenueKey("forge"), OpenAir("Barad-dur", "forge", c[2], camp.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new DispatchErrand());
				player.RemoveTag(Errands.LogTK);
				Reputation.Set(player, "Barad-dur", 0); //Vojak: medium orders
				player.SetTag(WarCamp.TopRankTK, 6); //no promotion pay in the way of the pay checks
				NPC overseer = Residents.BodyOf(Gem(player, "campoverseer", "overseer"));
				player.Go(new Point4D(overseer.X, overseer.Y, overseer.Z, overseer.M));
				Console.WriteLine("MORDORSTORIES_SETUP: overseer=" + overseer.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The tower's box of the dispatch the client took: at Minas Morgul, and the hero beside it.</summary>
		[SteamFunction]
		public static void TestMordorStoriesBesideBox(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "dispatches");
			Thing box = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => t is Item);
			Point4D tower = WorldEvents.CentreOf("Minas_Morgul");
			Report("theBoxIsAtTheTower", (box != null) && (box.M == tower.M) && (Point2D.GetSimpleDistance(box, tower) <= Errands.ThingSpread)
				&& Errands.Where(errand.Current).StartsWith("Minas Morgul, ", StringComparison.Ordinal));
			if (box != null) {
				player.Go(new Point4D(box.X, box.Y, box.Z, box.M));
			}
			Console.WriteLine("MORDORSTORIES_BOX: uid=" + ((box == null) ? 0 : box.Uid));
		}

		[SteamFunction]
		public static void TestMordorStoriesState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theDispatchArrived", !Errands.Active(player).Exists(e => e.key == "dispatches") && (player.Gold > gold));
			Console.WriteLine("MORDORSTORIES_STATE_DONE");
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city) {
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
			player.Level = 10; //the same level each errand: the pay scales by it
			player.Experience = 0;
		}

		private static bool Do(Player player, Errand errand, int choice = -1) {
			NPC body = Residents.BodyOf(Residents.ById(errand.Current.residentId));
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			return Errands.Do(player, errand, body, choice);
		}

		private static int Pouches(Player player) {
			int count = 0;
			ThingDef pouch = ThingDef.GetByDefname("i_mi_pouch");
			foreach (AbstractItem item in player.Backpack) {
				if (item.Def == pouch) {
					count++;
				}
			}
			return count;
		}

		[SteamFunction]
		public static void TestMordorStoriesChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			ErrandSettings es = Errands.Settings;
			int pouchChance = es.pouchChance, deedGold = DeedSettings.instance.gold, deedExp = DeedSettings.instance.exp;
			try {
				account.PLevel = 1;
				DeedSettings.instance.gold = 0; //the courier's deed would pay a tier along the way
				DeedSettings.instance.exp = 0;
				ErrandTemplates.Register(new NurnQuotaErrand());
				ErrandTemplates.Register(new EyeSpyErrand());
				Chain(player);
			} finally {
				es.pouchChance = pouchChance;
				DeedSettings.instance.gold = deedGold;
				DeedSettings.instance.exp = deedExp;
				account.PLevel = plevel;
				MordorStories.RollOverride.Clear();
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("MORDORSTORIES_CHAIN_DONE");
			}
		}

		//a Nurn quota run to its end with the choice given: the gold it paid, or -1
		private static long Quota(Player player, Item overseer, int choice) {
			Fresh(player);
			Errand quota = Build(player, new NurnQuotaErrand(), overseer, "Nurn");
			if ((quota == null) || !Errands.Accept(player, quota)) {
				return -1;
			}
			foreach (Thing slave in Errands.ThingsOf(player, quota)) {
				slave.RemoveTimer(ErrandThingFleeTimer.Key);
				NPCAiTimer.Freeze((NPC) slave, 60);
				player.Go(new Point4D(slave.X, slave.Y, slave.Z, slave.M));
				Errands.UseThing(slave, player);
			}
			long gold = player.Gold;
			return (quota.index == 1) && Do(player, quota, choice) && !Errands.Active(player).Contains(quota) ? player.Gold - gold : -1;
		}

		private static void Chain(Player player) {
			ErrandSettings es = Errands.Settings;
			es.pouchChance = 0;

			//the pits of Nurn: slaves that flee and can't be hurt; all handed back, or one let go for half the pay, karma, a risk and a gift
			Item overseer = Gem(player, "nurn", "overseer");
			Fresh(player);
			Errand look = Build(player, new NurnQuotaErrand(), overseer, "Nurn");
			bool shape = false;
			if ((look != null) && Errands.Accept(player, look)) {
				List<Thing> slaves = Errands.ThingsOf(player, look);
				Point4D nurn = WorldEvents.CentreOf("Nurn");
				shape = (slaves.Count == NurnQuotaErrand.Slaves) && slaves.TrueForAll(t => (t is NPC n) && n.Flag_Invulnerable && (n.GetTimer(ErrandThingFleeTimer.Key) != null)
					&& (Point2D.GetSimpleDistance(n, nurn) <= Errands.ThingSpread));
				Errands.GiveUp(player, look);
			}
			bool onlyOverseers = (Build(player, new NurnQuotaErrand(), Gem(player, "nurnmess", "cook"), "Nurn") == null)
				&& (Build(player, new NurnQuotaErrand(), Gem(player, "campoverseer", "overseer"), "Barad-dur") == null);
			Report("nurnSlavesFleeInNurnFromItsOverseersOnly", shape && onlyOverseers);

			player.RemoveTag(MordorStories.FreedTK);
			short karma = player.Karma;
			long full = Quota(player, overseer, 0);
			bool handedBack = (full > 0) && (player.Karma == karma) && (MordorStories.Freed(player) == 0);
			int rep = Reputation.Of(player, "Nurn");
			MordorStories.RollOverride.Enqueue(99); //not found out
			long half = Quota(player, overseer, NurnQuotaErrand.FreeChoice);
			bool freed = (half > 0) && (Math.Abs((2 * half) - full) <= 1) && (player.Karma == karma + NurnQuotaErrand.KarmaGain)
				&& (MordorStories.Freed(player) == 1) && (Reputation.Of(player, "Nurn") == rep + es.repMedium);
			Report("handedBackOrOneFreedForHalfAndKarma", handedBack && freed);
			MordorStories.RollOverride.Enqueue(0); //found out
			rep = Reputation.Of(player, "Nurn");
			Quota(player, overseer, NurnQuotaErrand.FreeChoice);
			bool foundOut = Reputation.Of(player, "Nurn") == rep + es.repMedium - NurnQuotaErrand.FoundOutLoss;
			int pouches = Pouches(player);
			MordorStories.RollOverride.Enqueue(99);
			Quota(player, overseer, NurnQuotaErrand.FreeChoice); //the third freed
			Report("foundOutSometimesAndAGiftEveryThird", foundOut && (MordorStories.Freed(player) == 3) && (Pouches(player) == pouches + 1));

			//the Eye's spies: from the camp's overseers; two clues single out the spy among three suspects
			Item campOverseer = Gem(player, "campoverseer", "overseer");
			bool campOnly = (Build(player, new EyeSpyErrand(), overseer, "Nurn") == null) && (Build(player, new EyeSpyErrand(), Gem(player, "messhall", "cook"), "Barad-dur") == null);
			bool clues = true;
			for (int i = 0; i < 10; i++) {
				Errand probe = Build(player, new EyeSpyErrand(), campOverseer, "Barad-dur");
				if (probe == null) {
					clues = false;
					break;
				}
				ErrandStep accuse = probe.steps[2];
				int fitting = 0;
				for (int k = 0; k < accuse.choices.Count; k++) {
					Item suspect = SuspectOf(accuse.choices[k], probe.city);
					if ((suspect != null) && probe.steps[0].reply.Contains(MordorStories.WorkLabel(suspect)) && probe.steps[1].reply.EndsWith("na " + MordorStories.FirstLetter(suspect) + ".")) {
						fitting++;
						clues &= k == accuse.count;
					}
				}
				clues &= (accuse.choices.Count == EyeSpyErrand.Suspects) && (fitting == 1);
			}
			Report("theCluesSingleOutTheSpyFromTheCampsOverseers", campOnly && clues);

			Fresh(player);
			Errand right = Build(player, new EyeSpyErrand(), campOverseer, "Barad-dur");
			long gold = player.Gold;
			rep = Reputation.Of(player, "Barad-dur");
			bool paid = (right != null) && Errands.Accept(player, right) && Do(player, right) && Do(player, right) && Do(player, right, right.steps[2].count)
				&& (player.Gold > gold) && (Reputation.Of(player, "Barad-dur") > rep);
			es.pouchChance = 100; //a pouch would come with any paid medium errand: a wrong accusation gets none
			Fresh(player);
			Errand wrong = Build(player, new EyeSpyErrand(), campOverseer, "Barad-dur");
			gold = player.Gold;
			rep = Reputation.Of(player, "Barad-dur");
			int pouchesBefore = Pouches(player);
			bool unpaid = (wrong != null) && Errands.Accept(player, wrong) && Do(player, wrong) && Do(player, wrong) && Do(player, wrong, (wrong.steps[2].count + 1) % EyeSpyErrand.Suspects)
				&& (player.Gold == gold) && (Reputation.Of(player, "Barad-dur") == rep - WarCampSettings.instance.orderFailLoss) && (Pouches(player) == pouchesBefore);
			es.pouchChance = 0;
			Report("theRightSpyPaysAWrongOneCosts", paid && unpaid);

			//the Mouth's dispatches: the camp's only
			Report("dispatchesOnlyFromTheCamp", Build(player, new DispatchErrand(), overseer, "Nurn") == null);
		}

		//the resident a "Je to <name>." choice names, in the city
		private static Item SuspectOf(string choice, string city) {
			foreach (Item gem in Residents.AllGems) {
				CityVenue work = Residents.WorkplaceOf(gem);
				if ((work != null) && string.Equals(work.City, city, StringComparison.OrdinalIgnoreCase) && (choice == "Je to " + BasicErrands.Name(gem) + ".")) {
					return gem;
				}
			}
			return null;
		}
	}
}
