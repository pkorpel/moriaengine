using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1300 - the regional stories on the server: the hobbit post and its ranks, the horse thieves, the Citadel guard's
	/// helm, the elves' letters to the Grey Havens. Tests/Protocol/RegionalStoryTests.cs takes the letters through a client and puts
	/// them in the elves' chest with a double click first.</summary>
	public static class RegionalStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("STORIES_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_storytest_" + key + "_");
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

		/// <summary>A tavern in Hobitin; a stable and a tavern in Edoras; a barracks and a tavern in Minas Tirith; only the elves'
		/// letters offered; the hero beside the Hobitin innkeeper.</summary>
		[SteamFunction]
		public static void TestStoriesSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D hobitin = WorldEvents.CentreOf("Hobitin"), edoras = WorldEvents.CentreOf("Edoras"), tirith = WorldEvents.CentreOf("Minas_Tirith");
				player.SetTag(VenueKey("hobitin"), OpenAir("Hobitin", "tavern", Spots(hobitin, 1)[0], hobitin.M));
				List<int[]> e = Spots(edoras, 2), t = Spots(tirith, 2);
				player.SetTag(VenueKey("stable"), OpenAir("Edoras", "stable", e[0], edoras.M));
				player.SetTag(VenueKey("edorastavern"), OpenAir("Edoras", "tavern", e[1], edoras.M));
				player.SetTag(VenueKey("barracks"), OpenAir("Minas_Tirith", "barracks", t[0], tirith.M));
				player.SetTag(VenueKey("tirithtavern"), OpenAir("Minas_Tirith", "tavern", t[1], tirith.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new GreyHavensErrand());
				player.RemoveTag(Errands.LogTK);
				NPC innkeeper = Residents.BodyOf(Gem(player, "hobitin", "innkeeper"));
				player.Go(new Point4D(innkeeper.X, innkeeper.Y, innkeeper.Z, innkeeper.M));
				Console.WriteLine("STORIES_SETUP: innkeeper=" + innkeeper.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The elves' chest of the errand the client took: at the Grey Havens, and the hero beside it.</summary>
		[SteamFunction]
		public static void TestStoriesBesideChest(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "greyhavens");
			Thing chest = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => t is Item);
			Point4D havens = WorldEvents.CentreOf("Sede_pristavy");
			Report("theChestIsAtTheHavens", (chest != null) && (chest.M == havens.M) && (Point2D.GetSimpleDistance(chest, havens) <= Errands.ThingSpread)
				&& (errand.Expires - Errands.Now).TotalMinutes > 58);
			Report("theJournalSaysWhere", (errand != null) && Errands.Where(errand.Current).StartsWith("Sede pristavy, ", StringComparison.Ordinal));
			if (chest != null) {
				player.Go(new Point4D(chest.X, chest.Y, chest.Z, chest.M));
			}
			Console.WriteLine("STORIES_CHEST: uid=" + ((chest == null) ? 0 : chest.Uid));
		}

		[SteamFunction]
		public static void TestStoriesState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theLettersSailed", !Errands.Active(player).Exists(e => e.key == "greyhavens") && (player.Gold > gold));
			Console.WriteLine("STORIES_STATE_DONE");
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
		}

		private static void Beside(Player player, Thing thing) {
			if (thing is NPC npc) {
				NPCAiTimer.Freeze(npc, 60);
			}
			player.Go(new Point4D(thing.X, thing.Y, thing.Z, thing.M));
		}

		private static bool Talk(Player player, Errand errand) {
			NPC body = Residents.BodyOf(Residents.ById(errand.Current.residentId));
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			return Errands.Do(player, errand, body);
		}

		[SteamFunction]
		public static void TestStoriesChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				Chain(player);
			} finally {
				account.PLevel = plevel;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("STORIES_CHAIN_DONE");
			}
		}

		//a hobbit post round taken and done: the gold it paid, or -1
		private static long PostRound(Player player, Item giver) {
			Fresh(player);
			Errand post = Build(player, new HobbitPostErrand(), giver, "Hobitin");
			if ((post == null) || !Errands.Accept(player, post)) {
				return -1;
			}
			player.Level = 10; //the same level each round: the pay scales by it
			player.Experience = 0;
			long gold = player.Gold;
			for (int i = 0; i < HobbitPostErrand.Letters; i++) {
				if (!Talk(player, post)) {
					return -1;
				}
			}
			return Errands.Active(player).Contains(post) ? -1 : player.Gold - gold;
		}

		private static void Chain(Player player) {
			//the stories' templates known to the engine (Accept reads the deadline, Finish and things their hooks), the old ones back in the finally
			ErrandTemplates.Register(new HobbitPostErrand());
			ErrandTemplates.Register(new HorseThievesErrand());
			ErrandTemplates.Register(new CitadelHelmErrand());
			//the hobbit post: three letters to three people, timed; a rank's better share
			Item hobbitGiver = Gem(player, "hobitin", "cook");
			Fresh(player);
			Errand post = Build(player, new HobbitPostErrand(), hobbitGiver, "Hobitin");
			HashSet<string> to = new HashSet<string>();
			bool shape = (post != null) && (post.steps.Count == HobbitPostErrand.Letters);
			for (int i = 0; shape && (i < post.steps.Count); i++) {
				shape &= (post.steps[i].Kind == ErrandStepKind.Deliver) && to.Add(post.steps[i].residentId) && (post.steps[i].residentId != Residents.IdOf(hobbitGiver));
			}
			bool timed = false;
			if (shape && Errands.Accept(player, post)) {
				timed = Math.Abs((post.Expires - Errands.Now).TotalMinutes - 20) < 1;
				Errands.GiveUp(player, post);
			}
			bool onlyHobitin = Build(player, new HobbitPostErrand(), Gem(player, "edorastavern", "cook"), "Edoras") == null;
			Report("thePostIsThreeLettersTimedInHobitinOnly", shape && timed && onlyHobitin);
			player.RemoveTag(RegionalStories.PostRoundsTK);
			bool plainShare = new HobbitPostErrand().PayPercent(player, null) == 100;
			long first = PostRound(player, hobbitGiver);
			bool counted = RegionalStories.PostRounds(player) == 1;
			player.SetTag(RegionalStories.PostRoundsTK, RegionalStories.PostRankFrom[1] - 1);
			PostRound(player, hobbitGiver); //the round that reaches the rank, still paid as Postak
			bool promoted = (RegionalStories.PostRank(player) == 1) && (new HobbitPostErrand().PayPercent(player, null) == 100 + HobbitPostErrand.SharePerRank);
			long ranked = PostRound(player, hobbitGiver);
			Report("roundsRaiseTheRankAndItsShare", plainShare && (first > 0) && counted && promoted && (ranked > first));

			//the horse thieves: only from Edoras's stable, barracks, town hall or market; thieves killed, horses caught, the report
			Item groom = Gem(player, "stable", "groom");
			bool whoGives = (Build(player, new HorseThievesErrand(), Gem(player, "edorastavern", "cook"), "Edoras") == null)
				&& (Build(player, new HorseThievesErrand(), hobbitGiver, "Hobitin") == null);
			Fresh(player);
			Errand horses = Build(player, new HorseThievesErrand(), groom, "Edoras");
			bool thieves = false, caught = false, reported = false;
			if ((horses != null) && Errands.Accept(player, horses)) {
				List<Thing> men = Errands.ThingsOf(player, horses);
				thieves = (men.Count == HorseThievesErrand.Thieves) && men.TrueForAll(t => (t is NPC n) && !n.Flag_Invulnerable);
				foreach (Thing man in men) {
					Beside(player, man);
					((NPC) man).CauseDeath(player);
				}
				thieves &= horses.index == 1;
				List<Thing> mounts = Errands.ThingsOf(player, horses);
				caught = (mounts.Count == HorseThievesErrand.Horses) && mounts.TrueForAll(t => (t is NPC n) && n.Flag_Invulnerable);
				foreach (Thing horse in mounts) {
					Beside(player, horse);
					Errands.UseThing(horse, player);
				}
				caught &= (horses.index == 2) && mounts.TrueForAll(t => t.IsDeleted);
				reported = Talk(player, horses) && !Errands.Active(player).Contains(horses);
			}
			Report("horseThievesFromTheRightPlacesKilledCaughtReported", whoGives && thieves && caught && reported);

			//a Kill step's NPC killed by someone else alone is replaced, but only twice its count; then anyone's kill counts
			Fresh(player);
			Errand farmed = Build(player, new HorseThievesErrand(), groom, "Edoras");
			bool capped = false;
			if ((farmed != null) && Errands.Accept(player, farmed)) {
				int replaced = 0;
				for (int i = 0; i < 2 * HorseThievesErrand.Thieves; i++) {
					List<Thing> left = Errands.ThingsOf(player, farmed);
					if (left.Count > 0) {
						((NPC) left[0]).CauseDeath(null);
						replaced++;
					}
				}
				capped = (replaced == 2 * HorseThievesErrand.Thieves) && (farmed.index == 0) && (farmed.Current.progress == 0)
					&& (Errands.ThingsOf(player, farmed).Count == HorseThievesErrand.Thieves);
				((NPC) Errands.ThingsOf(player, farmed)[0]).CauseDeath(null); //past the cap: counts
				capped &= (farmed.Current.progress == 1) && (Errands.ThingsOf(player, farmed).Count == HorseThievesErrand.Thieves - 1);
				Errands.GiveUp(player, farmed);
			}
			Report("othersKillsReplacedOnlyTwiceTheCount", capped);

			//the Citadel helm: only from Minas Tirith's guards; in the tavern; 30 minutes
			Item guard = Gem(player, "barracks", "guard");
			CityVenue tavern = (CityVenue) player.GetTag(VenueKey("tirithtavern"));
			bool guardsOnly = Build(player, new CitadelHelmErrand(), Gem(player, "barracks", "captain"), "Minas_Tirith") == null;
			Fresh(player);
			Errand helm = Build(player, new CitadelHelmErrand(), guard, "Minas_Tirith");
			bool inTavern = false, found = false, back = false;
			if ((helm != null) && Errands.Accept(player, helm)) {
				List<Thing> things = Errands.ThingsOf(player, helm);
				inTavern = (things.Count == 1) && (VenueRegistry.At((IPoint4D) things[0]) == tavern) && (Math.Abs((helm.Expires - Errands.Now).TotalMinutes - 30) < 1);
				if (things.Count == 1) {
					Beside(player, things[0]);
					found = Errands.UseThing(things[0], player) && things[0].IsDeleted && (helm.index == 1);
				}
				back = Talk(player, helm) && !Errands.Active(player).Contains(helm);
			}
			Report("theHelmIsInTheTavernFoundAndReturned", guardsOnly && inTavern && found && back);

			//the elves' letters: only from Hobitin
			Report("theLettersOnlyFromHobitin", Build(player, new GreyHavensErrand(), Gem(player, "edorastavern", "cook"), "Edoras") == null);
		}
	}
}
