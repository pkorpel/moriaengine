using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1250 - .settings "Ukoly": the errands' limits and pay (design 5.5, 5.6).</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class ErrandSettings : SettingsMetaCategory {
		[SavedMember]
		public static ErrandSettings instance = new ErrandSettings();

		[LoadingInitializer]
		public ErrandSettings() {
		}

		/// <summary>Errands a player can have taken at once.</summary>
		[SaveableData]
		public int maxActive = 3;

		/// <summary>Errands taken in one city in one game day (24 real minutes), and from one resident.</summary>
		[SaveableData]
		public int perCityPerDay = 6, perGiverPerDay = 1;

		/// <summary>Each time the same template is done again in a row, the pay drops by this percent, down to payFloor.</summary>
		[SaveableData]
		public int repeatPenalty = 25, payFloor = 25;

		/// <summary>Base pay by tier (small, medium, large), raised with the player's level and the errand's length.</summary>
		[SaveableData]
		public int goldSmall = 60, goldMedium = 150, goldLarge = 400;

		[SaveableData]
		public int expSmall = 40, expMedium = 120, expLarge = 350;

		/// <summary>The giver's friendship and the city's reputation for a done errand, by tier.</summary>
		[SaveableData]
		public int friendSmall = 1, friendMedium = 2, friendLarge = 2;

		[SaveableData]
		public int repSmall = 5, repMedium = 10, repLarge = 20;

		/// <summary>In percent: a medium errand's chance of a magic pouch; a large one's of a booster (it always gives a magic bag).</summary>
		[SaveableData]
		public int pouchChance = 33, boosterChance = 25;
	}

	/// <summary>#1250 - the errand engine: offering, taking, doing a step at a resident, finishing, giving up, the limits and
	/// the pay. Steps are done in the resident's dialog (D_ResidentTalk), where the player picks what to say; nothing completes
	/// by a typed word (design 5.5).</summary>
	public static class Errands {
		internal static readonly TagKey LogTK = TagKey.Acquire("_errands_");
		internal static readonly TagKey ItemTK = TagKey.Acquire("_errand_item_");
		public const string EnemyRealmRefusal = "Pro nepritele nasi rise tu zadnou praci nemam."; //#1312

		/// <summary>How near a resident a player must be to talk to them, and see them.</summary>
		public const int Reach = 4; //#1312 - the user's call: residents wander while talked to, 3 left players out of reach

		public static DateTime? NowOverride;

		internal static DateTime Now {
			get { return NowOverride ?? DateTime.UtcNow; }
		}

		/// <summary>A game day: 24 real minutes (LightAndWeather's day).</summary>
		public static int GameDay {
			get { return (int) (Globals.TimeInSeconds / (24 * 60)); }
		}

		internal static ErrandSettings Settings {
			get { return ErrandSettings.instance; }
		}

		/// <summary>The player's errands, the run-out ones dropped (and told), the day's counts reset on a new game day.</summary>
		public static ErrandLog Log(Player player) {
			ErrandLog log = player.GetTag(LogTK) as ErrandLog;
			if (log == null) {
				log = new ErrandLog();
				player.SetTag(LogTK, log);
			}
			log.NewDayIfNeeded(GameDay);
			for (int i = log.active.Count - 1; i >= 0; i--) {
				Errand errand = log.active[i];
				if (errand.Expires <= Now) {
					log.active.RemoveAt(i);
					TakeBackItems(player, errand);
					player.SysMessage("Ukol \"" + errand.name + "\" vyprsel.");
					WarCamp.OnFailed(player, errand); //#1280 - an order run out costs standing
				}
			}
			return log;
		}

		public static List<Errand> Active(Player player) {
			return Log(player).active;
		}

		/// <summary>Why this giver offers nothing now (null when they can): the player's limits and the giver's.</summary>
		public static string WhyNot(Player player, Item giverGem, string city) {
			if (Reputation.IsEnemyCity(player, city)) {
				return EnemyRealmRefusal; //#1312 - the enemy realm's towns have no work for you
			}
			ErrandLog log = Log(player);
			ErrandSettings s = Settings;
			if (log.active.Count >= s.maxActive) {
				return "Uz toho mas dost na praci. Prijd, az neco dodelas.";
			}
			foreach (Errand errand in log.active) {
				if (errand.giverId == Residents.IdOf(giverGem)) {
					return "Nejdriv dodelej, co jsem ti dal.";
				}
			}
			if (log.Count(Residents.IdOf(giverGem)) >= s.perGiverPerDay) {
				return "Dnes uz nic nepotrebuju. Zastav se jindy.";
			}
			if ((city != null) && (log.Count(city) >= s.perCityPerDay)) {
				return "Pro dnesek jsi pomohl dost. Mesto si poradi i bez tebe.";
			}
			if ((city != null) && (Reputation.Level(player, city) == ReputationLevel.Nepritel)) {
				return "S nepritelem mesta nemluvim."; //an enemy earns no standing by errands; the shops refuse them too
			}
			return null;
		}

		/// <summary>Whether the player can talk to the resident now: alive, near, in sight. Told why not.</summary>
		public static bool CanTalk(Player player, NPC resident) {
			if ((resident == null) || resident.IsDeleted || resident.Flag_Dead || player.Flag_Dead) {
				return false;
			}
			if ((resident.M != player.M) || (Point2D.GetSimpleDistance(resident, player) > Reach) || !player.CanSeeLOS(resident).Allow) {
				player.SysMessage("Jsi od " + resident.Name + " prilis daleko.");
				return false;
			}
			return true;
		}

		/// <summary>The player's errand of this number, or null (done, given up, run out).</summary>
		public static Errand ById(Player player, int id) {
			foreach (Errand errand in Active(player)) {
				if (errand.id == id) {
					return errand;
				}
			}
			return null;
		}

		/// <summary>An errand this giver would offer now, made but not taken (or null): a template drawn by weight among the
		/// ones that can be built here.</summary>
		public static Errand Make(Player player, Item giverGem) {
			CityVenue work = Residents.WorkplaceOf(giverGem);
			string city = (work == null) ? null : work.City;
			if ((city == null) || (WhyNot(player, giverGem, city) != null)) {
				return null;
			}
			List<ErrandTemplate> pool = new List<ErrandTemplate>(ErrandTemplates.All);
			while (pool.Count > 0) {
				ErrandTemplate template = DrawByWeight(pool);
				pool.Remove(template);
				if (WarCamp.AboveRank(player, city, template.Tier)) {
					continue; //#1280 - the camp gives no order above the rank
				}
				Errand errand = new Errand { key = template.Key, name = template.Name, giverId = Residents.IdOf(giverGem), city = city, tier = (int) template.Tier };
				ErrandContext context = new ErrandContext(player, giverGem, city);
				if (template.TryBuild(context, errand) && (errand.steps.Count > 0)) {
					return errand; //its time runs from when it's taken
				}
			}
			return null;
		}

		private static ErrandTemplate DrawByWeight(List<ErrandTemplate> pool) {
			int total = 0;
			foreach (ErrandTemplate template in pool) {
				total += Math.Max(1, template.Weight);
			}
			int roll = Globals.dice.Next(total);
			foreach (ErrandTemplate template in pool) {
				roll -= Math.Max(1, template.Weight);
				if (roll < 0) {
					return template;
				}
			}
			return pool[pool.Count - 1];
		}

		/// <summary>The player takes the errand: it joins their log and counts for the day's limits.</summary>
		public static bool Accept(Player player, Errand errand) {
			Item giver = Residents.ById(errand.giverId);
			if ((giver == null) || (WhyNot(player, giver, errand.city) != null)) {
				return false;
			}
			if (WarCamp.AboveRank(player, errand.city, errand.Tier)) {
				return false; //#1280 - offered before the rank fell
			}
			if (Log(player).active.Contains(errand)) {
				return false;
			}
			ErrandLog log = Log(player);
			//the same template done in a row, and the same held now: holding three at once is no way round the penalty
			int repeats = ((log.lastKey == errand.key) ? log.streak : 0) + log.active.FindAll(a => a.key == errand.key).Count;
			errand.payPercent = Math.Max(Settings.payFloor, 100 - (repeats * Settings.repeatPenalty));
			ErrandTemplate template = ErrandTemplates.Get(errand.key);
			errand.expiresTicks = Now.AddMinutes(WarCamp.Minutes(errand.city, (template == null) ? 60 : template.Minutes)).Ticks; //#1280 - an order's is shorter
			errand.id = log.nextId++;
			log.active.Add(errand);
			log.Add(errand.giverId);
			log.Add(errand.city);
			StartStep(player, errand);
			player.SysMessage("Novy ukol: " + errand.name + ". " + errand.Current.task);
			return true;
		}

		//a Deliver step with an item puts it in the pack as it begins
		private static void StartStep(Player player, Errand errand) {
			ErrandStep step = errand.Current;
			if (step == null) {
				return;
			}
			if ((step.Kind == ErrandStepKind.Deliver) && (step.itemDef.Length > 0)) {
				ItemDef def = ThingDef.GetByDefname(step.itemDef) as ItemDef;
				CityVenue venue = (step.venueId.Length > 0) ? VenueRegistry.ById(step.venueId) : null;
				int[] spot = (venue == null) ? null : ResidentSchedule.SpotIn(venue, "errand/" + ItemMark(player, errand));
				if (def == null) {
					step.itemDef = ""; //no such item: a message to carry instead, or the step could never be done
				} else if (spot != null) { //#1252 - lost somewhere in the venue: lying there to be found
					Item item = (Item) def.Create(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], venue.M));
					item.SetTag(ItemTK, ItemMark(player, errand));
					ItemDecaySystem.Cancel(item); //it waits for whoever looks for it, until the errand ends
					step.placed = item;
				} else {
					Item item = (Item) def.Create(player.Backpack);
					item.SetTag(ItemTK, ItemMark(player, errand));
					step.placed = item; //#1258 - Release clears it wherever it went: worn, dropped, in a chest (a sword to carry was a free sword)
				}
			}
			if (step.Kind == ErrandStepKind.Things) {
				PlaceThings(player, errand, step);
			}
			NPC companion = Body(step.companionId);
			if (companion != null) { //#1252 - walks with the player; the schedule leaves a follower alone while the errand holds
				companion.PetGoalPoint = null;
				companion.FollowTarget = player;
				companion.SetTag(FollowingTK, 1);
				NPCAiTimer.GetFor(companion)?.WakeUp();
			}
			ErrandTemplates.Get(errand.key)?.OnStepStart(player, errand); //#1317
		}

		private static NPC Body(string residentId) {
			if (string.IsNullOrEmpty(residentId)) {
				return null;
			}
			Item gem = Residents.ById(residentId);
			return (gem == null) ? null : Residents.BodyOf(gem);
		}

		/// <summary>On a resident walking with a player for an errand (not a GM's "come").</summary>
		internal static readonly TagKey FollowingTK = TagKey.Acquire("_errand_follow_");

		//#1252 - whoever followed the player for this errand goes back to their day, and an item the errand made (laid in a venue,
		//or put in the pack since #1258) is cleared wherever it went: worn, in the bank, in a chest, in another's pack
		private static void Release(Player player, Errand errand) {
			foreach (ErrandStep step in errand.steps) {
				NPC companion = Body(step.companionId);
				if ((companion != null) && (companion.FollowTarget == player)) {
					LetGo(companion);
				}
				if ((step.placed != null) && !step.placed.IsDeleted) {
					step.placed.Delete();
				}
				foreach (Thing thing in ThingsOf(step, ItemMark(player, errand))) {
					thing.Delete(); //#1293 - the step's things go with the errand, done, given up or run out
				}
			}
			ErrandTemplates.Get(errand.key)?.OnEnd(player, errand); //#1317
		}

		private static void LetGo(NPC companion) {
			companion.FollowTarget = null;
			companion.RemoveTag(FollowingTK);
			Item gem = Residents.GemOf(companion);
			if (gem != null) {
				ResidentSchedule.Apply(gem, companion, false);
			}
		}

		/// <summary>#1252 - from the resident schedule: true while an errand still holds this resident walking with its player.
		/// One whose player is gone (offline, deleted) or whose errand ended or ran out is let go here, as nothing else
		/// would notice.</summary>
		public static bool HoldsFollower(NPC npc) {
			if (npc.GetTag(FollowingTK) == null) {
				return false;
			}
			Player player = npc.FollowTarget as Player;
			if ((player != null) && !player.IsDeleted && !player.Flag_Disconnected) {
				string id = Residents.IdOf(Residents.GemOf(npc));
				foreach (Errand errand in Active(player)) { //Active drops run-out errands first
					for (int i = errand.index; i < errand.steps.Count; i++) {
						if (errand.steps[i].companionId == id) {
							return true;
						}
					}
				}
			}
			LetGoQuietly(npc);
			return false;
		}

		private static void LetGoQuietly(NPC npc) {
			npc.FollowTarget = null;
			npc.RemoveTag(FollowingTK);
		}

		/// <summary>#1252 - an errand's item can be picked up only by its player (a GM can always): a lost ring lies in a venue for
		/// whoever was asked to find it.</summary>
		public static bool MayPickUp(Item item, Character picker) {
			string mark = item.GetTag(ItemTK) as string;
			if ((mark == null) || ((picker != null) && picker.IsGM)) {
				return true;
			}
			int slash = mark.IndexOf('/');
			return (picker != null) && (slash > 0) && (mark.Substring(0, slash) == picker.Uid.ToString(CultureInfo.InvariantCulture));
		}

		private static string ItemMark(Player player, Errand errand) {
			return player.Uid.ToString(CultureInfo.InvariantCulture) + "/" + errand.giverId + "/" + errand.expiresTicks.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>The errands whose current step is at this resident.</summary>
		public static List<Errand> At(Player player, Item residentGem) {
			List<Errand> here = new List<Errand>();
			string id = Residents.IdOf(residentGem);
			foreach (Errand errand in Active(player)) {
				ErrandStep step = errand.Current;
				if ((step != null) && (step.residentId == id)) {
					here.Add(errand);
				}
			}
			return here;
		}

		/// <summary>The player does the errand's current step at its resident (`choice` for a Choose step). False, with the
		/// reason told, when the step's terms aren't met.</summary>
		public static bool Do(Player player, Errand errand, NPC resident, int choice = -1) {
			ErrandStep step = errand.Current;
			Item gem = (resident == null) ? null : Residents.GemOf(resident);
			if ((step == null) || (gem == null) || !Active(player).Contains(errand) || (Residents.IdOf(gem) != step.residentId) || !CanTalk(player, resident)) {
				return false;
			}
			NPC companion = Body(step.companionId); //checked before anything is taken or spent
			if ((step.companionId.Length > 0) && ((companion == null) || (companion.M != player.M) || (Point2D.GetSimpleDistance(companion, player) > Reach + 3))) {
				player.RedMessage((companion == null) ? "Ten, koho jsi mel doprovodit, tu neni." : companion.Name + " neni s tebou.");
				return false;
			}
			switch (step.Kind) {
				case ErrandStepKind.Deliver:
					if ((step.itemDef.Length > 0) && !TakeMarked(player, errand)) {
						player.RedMessage("Nemas u sebe, co jsi mel prinest.");
						return false;
					}
					break;
				case ErrandStepKind.Fetch:
					ItemDef def = ThingDef.GetByDefname(step.itemDef) as ItemDef;
					if ((def == null) || (CountIn(player.Backpack, def) < step.count)) {
						player.RedMessage("Jeste nemas vse, co je potreba (" + step.count.ToString(CultureInfo.InvariantCulture) + ").");
						return false;
					}
					Consume(player.Backpack, def, step.count);
					break;
				case ErrandStepKind.Collect:
					Collector bag = FullBag(player.Backpack, ThingDef.GetByDefname(step.itemDef), step.count);
					if (bag == null) {
						player.RedMessage("Jeste toho nemas dost (" + step.count.ToString(CultureInfo.InvariantCulture) + ").");
						return false;
					}
					bag.Delete();
					break;
				case ErrandStepKind.Wait:
					if (!VenueClock.IsOpen(step.fromHour, step.toHour, VenueClock.Hour)) {
						player.RedMessage("Ted ne. Prijd mezi " + step.fromHour.ToString(CultureInfo.InvariantCulture) + ". a " + step.toHour.ToString(CultureInfo.InvariantCulture) + ". hodinou.");
						return false;
					}
					break;
				case ErrandStepKind.Choose:
					if ((choice < 0) || (choice >= step.choices.Count)) {
						return false;
					}
					step.chosen = choice;
					break;
			}
			string reply = (step.Kind == ErrandStepKind.Choose) && (choice < step.choiceReplies.Count) ? step.choiceReplies[choice] : step.reply;
			if (!string.IsNullOrEmpty(reply)) {
				resident.Say(reply);
			}
			Advance(player, errand);
			return true;
		}

		//the step done: the next begins, or the errand is finished
		private static void Advance(Player player, Errand errand) {
			errand.index++;
			if (errand.IsDone) {
				Finish(player, errand);
			} else {
				StartStep(player, errand);
				player.SysMessage(errand.name + ": " + errand.Current.task);
			}
		}

		#region things (#1293)
		/// <summary>On a thing a Things step made: its errand's mark (the player, the giver, the errand's end).</summary>
		internal static readonly TagKey ThingTK = TagKey.Acquire("_errand_thing_");
		internal const int ThingSpread = 15, ThingMinFromCentre = 4, OutsideSteps = 30;

		/// <summary>#1329 - Outside things (OutsideSpots): the road out is walked until it has been this many steps past the town's edge;
		/// a town not left within OutsideMaxSteps gets the old walk of OutsideSteps.</summary>
		internal const int OutsideBeyond = 10, OutsideMaxSteps = 150;

		/// <summary>A Things step's things still in the world: only those that still carry the errand's mark, as a uid freed by a
		/// caught or killed thing is soon another's (an item, a resident, a player), and must never be taken for one of these.</summary>
		internal static List<Thing> ThingsOf(ErrandStep step, string mark) {
			List<Thing> things = new List<Thing>();
			foreach (string part in (step.thingUids ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid) && (Thing.UidGetThing(uid) is Thing thing) && !thing.IsDeleted
						&& ((thing.GetTag(ThingTK) as string) == mark)) {
					things.Add(thing);
				}
			}
			return things;
		}

		/// <summary>The current step's things of one of the player's errands.</summary>
		public static List<Thing> ThingsOf(Player player, Errand errand) {
			ErrandStep step = errand.Current;
			return ((step == null) || (step.Kind != ErrandStepKind.Things)) ? new List<Thing>() : ThingsOf(step, ItemMark(player, errand));
		}

		//a thing done with (caught, killed) leaves the step's list and loses the mark, so its uid, freed later, means nothing here;
		//the uid is read before the thing goes (a deleted thing's Uid isn't its own any more)
		private static void Forget(ErrandStep step, Thing thing, int uid) {
			List<string> uids = new List<string>((step.thingUids ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			uids.Remove(uid.ToString(CultureInfo.InvariantCulture));
			step.thingUids = string.Join(",", uids);
			if (!thing.IsDeleted) {
				thing.RemoveTag(ThingTK);
			}
		}

		/// <summary>Spots for a step's things: in its venue, in the streets near the centre, or on a road out of the city.</summary>
		internal static List<Point4D> ThingSpots(Errand errand, ErrandStep step, int count, string seed) {
			List<Point4D> spots = new List<Point4D>();
			CityVenue venue = (step.venueId.Length > 0) ? VenueRegistry.ById(step.venueId) : null;
			Point4D centre = string.IsNullOrEmpty(step.thingCity) ? (GiverTownCentre(errand) ?? WorldEvents.CentreOf(errand.city)) : WorldEvents.CentreOf(step.thingCity);
			switch ((ErrandThingPlace) step.thingPlace) {
				case ErrandThingPlace.Venue:
					for (int i = 0; (venue != null) && (i < count); i++) {
						int[] spot = ResidentSchedule.SpotIn(venue, seed + "/" + i);
						if (spot != null) {
							spots.Add(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], venue.M));
						}
					}
					break;
				case ErrandThingPlace.Streets:
					if (centre != null) {
						List<int[]> tiles = WorldEvents.Reachable(Map.GetMap(centre.M), new[] { centre.X, centre.Y, centre.Z }, ThingSpread);
						tiles.RemoveAll(t => WorldSpots.Distance(t, centre.X, centre.Y) < ThingMinFromCentre);
						for (int i = 0; (i < count) && (tiles.Count > 0); i++) {
							int pick = Globals.dice.Next(tiles.Count);
							spots.Add(new Point4D((ushort) tiles[pick][0], (ushort) tiles[pick][1], (sbyte) tiles[pick][2], centre.M));
							tiles.RemoveAt(pick);
						}
					}
					break;
				default:
					if (centre != null) {
						spots.AddRange(OutsideSpots(centre, TownAround(errand, step, centre), count));
					}
					break;
			}
			return spots;
		}

		/// <summary>#1317 - a city can be several towns (Dol Amroth with Pelargir and Linhir, the Shire, Dale with Esgaroth): the centre of
		/// the one the giver works in (the smallest of the city's areas holding the workplace), or null to use the city's.</summary>
		private static Point4D GiverTownCentre(Errand errand) {
			Item gem = Residents.ById(errand.giverId);
			FlaggedRegion town = TownOf(gem, errand.city);
			if (town == null) {
				return null;
			}
			//one area can be two places: Erebor's gate hall and its halls deep in the mountain, 1600 tiles apart, joined by a teleport;
			//when the workplace's part isn't the centre's, the workplace is the centre
			Point4D centre = WorldEvents.CentreOfArea(town);
			CityVenue work = Residents.WorkplaceOf(gem);
			foreach (ImmutableRectangle rect in town.Rectangles) {
				if (rect.Contains(work.X, work.Y)) {
					return rect.Contains(centre.X, centre.Y) ? centre : new Point4D(work.X, work.Y, work.Z, work.M);
				}
			}
			return centre;
		}

		/// <summary>#1329 - `count` spots on a road out of the town, on its stretch past the edge, two tiles apart (outside the town for
		/// real: a fixed walk of OutsideSteps stayed inside nearly every town, Edoras's hill about half the time). A short stretch (an
		/// island's shore) takes the ends of the other roads out too, so the things don't stand on one tile. A town not left (or none)
		/// gets the old walk.</summary>
		internal static List<Point4D> OutsideSpots(Point4D centre, FlaggedRegion town, int count) {
			List<Point4D> spots = new List<Point4D>();
			List<List<int[]>> roads = (town == null) ? null : WorldEvents.RoutesOutOf(centre, town, OutsideBeyond, OutsideMaxSteps);
			if ((roads == null) || (roads.Count == 0)) {
				List<int[]> road = WorldEvents.RouteIn(centre, OutsideSteps);
				for (int i = 0; (road != null) && (i < count); i++) {
					int[] tile = road[Math.Min(i * 2, road.Count - 1)]; //along the road, not all on one tile
					spots.Add(new Point4D((ushort) tile[0], (ushort) tile[1], (sbyte) tile[2], centre.M));
				}
				return spots;
			}
			//each road begins with its stretch past the edge: one of the longest first, then the rest by their stretch
			List<int> stretches = roads.ConvertAll(road => {
				int past = 0;
				while ((past < road.Count) && !town.ContainsPoint(road[past][0], road[past][1])) {
					past++;
				}
				return past;
			});
			List<int> order = new List<int>();
			for (int r = 0; r < roads.Count; r++) {
				order.Add(r);
			}
			order.Sort((a, b) => stretches[b].CompareTo(stretches[a]));
			int longest = order.FindAll(r => stretches[r] == stretches[order[0]]).Count;
			int first = Globals.dice.Next(longest);
			int head = order[first];
			order[first] = order[0];
			order[0] = head;
			foreach (int r in order) {
				List<int[]> road = roads[r];
				for (int i = 0; (i < stretches[r]) && (spots.Count < count); i += 2) {
					int[] tile = road[i];
					if (!spots.Exists(o => (o.X == tile[0]) && (o.Y == tile[1]))) {
						spots.Add(new Point4D((ushort) tile[0], (ushort) tile[1], (sbyte) tile[2], centre.M));
					}
				}
			}
			for (int i = 0; (spots.Count > 0) && (spots.Count < count); i++) {
				spots.Add(spots[i]); //too few tiles outside for them all: some share
			}
			return spots;
		}

		/// <summary>#1329 - the town an Outside step's things must leave: the giver's (TownOf), or for a step in another city (or a
		/// giver gone) the city's area holding the centre the things are placed from.</summary>
		private static FlaggedRegion TownAround(Errand errand, ErrandStep step, Point4D centre) {
			if (string.IsNullOrEmpty(step.thingCity)) {
				return TownOf(Residents.ById(errand.giverId), errand.city) ?? TownAt(errand.city, centre.X, centre.Y, centre.M);
			}
			return TownAt(step.thingCity, centre.X, centre.Y, centre.M);
		}

		/// <summary>#1317 - the town of a city a resident works in: the smallest of the city's areas holding the workplace, or null.</summary>
		public static FlaggedRegion TownOf(Item gem, string city) {
			CityVenue work = (gem == null) ? null : Residents.WorkplaceOf(gem);
			return (work == null) ? null : TownAt(city, work.X, work.Y, work.M);
		}

		/// <summary>#1329 - the smallest of the city's areas holding the spot, or null.</summary>
		public static FlaggedRegion TownAt(string city, int x, int y, byte m) {
			if (string.IsNullOrEmpty(city)) {
				return null;
			}
			Point2D at = new Point2D(x, y);
			FlaggedRegion town = null;
			long smallest = long.MaxValue;
			foreach (FlaggedRegion area in VenueRegistry.AreasOf(city)) {
				if ((area.Mapplane == m) && area.ContainsPoint(at)) {
					long tiles = 0;
					foreach (ImmutableRectangle rect in area.Rectangles) {
						tiles += (long) (rect.Width + 1) * (rect.Height + 1);
					}
					if (tiles < smallest) {
						smallest = tiles;
						town = area;
					}
				}
			}
			return town;
		}

		/// <summary>#1300 - whether a Things step would find a spot for its things (else they'd stand beside the player): a template
		/// placing them far away checks before it offers the errand.</summary>
		internal static bool CanPlace(Errand errand, ErrandStep step) {
			return ThingSpots(errand, step, 1, "errand/check").Count > 0;
		}

		//a Things step's things into the world, marked as the errand's (beside the player when no spot is found)
		private static void PlaceThings(Player player, Errand errand, ErrandStep step) {
			ThingDef def = ThingDef.GetByDefname(step.itemDef);
			if (def == null) {
				Logger.WriteWarning("Errand " + errand.key + ": no def " + step.itemDef + " for its things.");
			}
			List<Point4D> spots = (def == null) ? new List<Point4D>() : ThingSpots(errand, step, step.count, "errand/" + ItemMark(player, errand));
			for (int i = spots.Count; (def != null) && (i < step.count); i++) { //nowhere found (a venue with no spots): beside the player, or the step could never be done
				int[] near = WorldSpots.NearestStandable(player.GetMap(), player.X + 2 + i, player.Y, 3);
				spots.Add((near == null) ? new Point4D(player.X, player.Y, player.Z, player.M) : new Point4D((ushort) near[0], (ushort) near[1], (sbyte) near[2], player.M));
			}
			List<string> uids = new List<string>();
			foreach (Point4D at in spots) {
				uids.Add(PlaceOne(player, errand, step, def, at).Uid.ToString(CultureInfo.InvariantCulture));
			}
			step.thingUids = string.Join(",", uids);
			step.count = uids.Count;
			step.progress = 0;
		}

		private static Thing PlaceOne(Player player, Errand errand, ErrandStep step, ThingDef def, Point4D at) {
			Thing thing = def.Create(at);
			thing.SetTag(ThingTK, ItemMark(player, errand));
			if (thing is Item item) {
				item.SetAttribute(ItemAttributes.attr_move_never, true);
				ItemDecaySystem.Cancel(item);
			} else if (thing is NPC npc) {
				NPCAiTimer ai = NPCAiTimer.GetFor(npc);
				if (ai != null) {
					ai.NeverSleeps = true;
				}
				if ((ErrandThingAction) step.thingAction == ErrandThingAction.Catch) {
					npc.Flag_Invulnerable = true; //caught, not killed: nobody can strike it down and leave the step undoable
				}
				if (step.flees) {
					ErrandThingFleeTimer flee = new ErrandThingFleeTimer();
					npc.AddTimer(ErrandThingFleeTimer.Key, flee);
					flee.DueInSeconds = ErrandThingFleeTimer.Seconds;
					flee.PeriodInSeconds = ErrandThingFleeTimer.Seconds;
				}
			}
			ErrandThingCheckTimer check = new ErrandThingCheckTimer(); //goes when its errand is over, its player away or gone
			thing.AddTimer(ErrandThingCheckTimer.Key, check);
			check.DueInSeconds = ErrandThingCheckTimer.Seconds;
			check.PeriodInSeconds = ErrandThingCheckTimer.Seconds;
			return thing;
		}

		/// <summary>From a thing's check timer: whether it still belongs to a live errand of its player (its step the current one);
		/// read without settling run-out errands, so nothing is released from inside another thing's timer twice.</summary>
		internal static bool IsLiveThing(Thing thing) {
			return ErrandOfThing(thing, out Player owner, false) != null;
		}

		/// <summary>The errand (and its player) a thing belongs to, its step the current one, or null.</summary>
		internal static Errand ErrandOfThing(Thing thing, out Player player) {
			return ErrandOfThing(thing, out player, true);
		}

		//settle: read through Active (run-out errands released); without, the log as it is, a run-out errand not counting
		internal static Errand ErrandOfThing(Thing thing, out Player player, bool settle) {
			player = null;
			string mark = thing.GetTag(ThingTK) as string;
			int slash = (mark == null) ? -1 : mark.IndexOf('/');
			if ((slash <= 0) || !int.TryParse(mark.Substring(0, slash), NumberStyles.Integer, CultureInfo.InvariantCulture, out int uid)) {
				return null;
			}
			player = Thing.UidGetThing(uid) as Player;
			if ((player == null) || player.IsDeleted) {
				return null;
			}
			string uidText = thing.Uid.ToString(CultureInfo.InvariantCulture);
			List<Errand> errands = settle ? Active(player) : (((player.GetTag(LogTK) as ErrandLog) == null) ? new List<Errand>() : ((ErrandLog) player.GetTag(LogTK)).active);
			foreach (Errand errand in errands) {
				ErrandStep step = errand.Current;
				if (!settle && (errand.Expires <= Now)) {
					continue;
				}
				if ((step != null) && (step.Kind == ErrandStepKind.Things) && (ItemMark(player, errand) == mark)
						&& Array.IndexOf((step.thingUids ?? "").Split(','), uidText) >= 0) {
					return errand;
				}
			}
			return null;
		}

		/// <summary>From a thing's double click: the errand's player catches or uses it, if near enough and in sight.</summary>
		public static bool UseThing(Thing thing, Character clicker) {
			Errand errand = ErrandOfThing(thing, out Player owner);
			if ((errand == null) || (clicker != owner)) {
				if (clicker is Player other) {
					other.SysMessage("To neni tvoje starost.");
				}
				return false;
			}
			ErrandStep step = errand.Current;
			ErrandThingAction action = (ErrandThingAction) step.thingAction;
			int reach = (action == ErrandThingAction.Catch) ? 1 : 2;
			if ((action == ErrandThingAction.Kill) || owner.Flag_Dead) {
				return false;
			}
			if ((owner.M != thing.M) || (Point2D.GetSimpleDistance(owner, thing) > reach) || (Math.Abs(owner.Z - thing.Z) > 15) || !owner.CanSeeLOS(thing).Allow) {
				owner.SysMessage((action == ErrandThingAction.Catch) ? "Musis byt hned vedle." : "Na to nedosahnes.");
				return false;
			}
			ErrandTemplate template = ErrandTemplates.Get(errand.key);
			int uid = thing.Uid;
			if ((template == null) || template.OnThing(owner, errand, thing)) {
				if (thing.IsDeleted) {
					Forget(step, thing, uid);
				}
				CountThing(owner, errand);
			}
			return true;
		}

		/// <summary>From NPC.On_Death: a Kill step's NPC killed by its player (or with them among its fighters) counts; one killed
		/// by anyone else alone doesn't, and another takes its place, so the step can still be done. Read without settling run-out
		/// errands: a release mid-death would delete the dying NPC under its own death.</summary>
		public static void OnThingDeath(NPC npc, Character killedBy) {
			Errand errand = ErrandOfThing(npc, out Player owner, false);
			if (errand == null) {
				return;
			}
			ErrandStep step = errand.Current;
			Forget(step, npc, npc.Uid); //its uid and mark gone first: nothing releases it while it dies
			if ((ErrandThingAction) step.thingAction != ErrandThingAction.Kill) {
				return;
			}
			if ((killedBy == owner) || LevelingSystem.FindFighters(npc, killedBy, false).Contains(owner) || (step.replaced >= 2 * step.count)) {
				CountThing(owner, errand);
			} else if (ThingDef.GetByDefname(step.itemDef) is ThingDef def) {
				step.replaced++;
				List<Point4D> spots = ThingSpots(errand, step, 1, "errand/again/" + npc.Uid);
				Point4D at = (spots.Count > 0) ? spots[0] : new Point4D(npc.X, npc.Y, npc.Z, npc.M);
				Thing again = PlaceOne(owner, errand, step, def, at);
				step.thingUids = string.IsNullOrEmpty(step.thingUids) ? again.Uid.ToString(CultureInfo.InvariantCulture) : step.thingUids + "," + again.Uid.ToString(CultureInfo.InvariantCulture);
			}
		}

		private static void CountThing(Player player, Errand errand) {
			ErrandStep step = errand.Current;
			step.progress++;
			if (step.progress >= step.count) {
				if (!string.IsNullOrEmpty(step.reply)) {
					player.SysMessage(step.reply);
				}
				Advance(player, errand);
			} else {
				player.SysMessage(errand.name + ": " + step.progress + " z " + step.count + ".");
			}
		}


		#endregion things

		/// <summary>The pay of a done errand (design 5.6): gold and experience by tier, level and length, the giver's friendship,
		/// the city's reputation; a medium one may add a magic pouch, a large one a magic bag and maybe a booster.</summary>
		private static void Finish(Player player, Errand errand) {
			ErrandLog log = Log(player);
			log.active.Remove(errand);
			Release(player, errand);
			ErrandTemplate template = ErrandTemplates.Get(errand.key);
			int share = errand.payPercent;
			if (template != null) {
				share = (share * template.PayPercent(player, errand)) / 100;
				template.OnDone(player, errand);
			}
			share = WarCamp.PayShare(player, errand.city, share); //#1280 - the rank's better share
			log.streak = (log.lastKey == errand.key) ? log.streak + 1 : 1;
			log.lastKey = errand.key;
			log.done++;
			if (share > 0) {
				Deeds.Add(player, Deeds.Courier); //#1296 - (#1302) an errand that earned nothing, as a wrong accusation, is no deed
			}
			ErrandSettings s = Settings;
			int gold, exp, friend, rep;
			switch (errand.Tier) {
				case ErrandTier.Large: gold = s.goldLarge; exp = s.expLarge; friend = s.friendLarge; rep = s.repLarge; break;
				case ErrandTier.Medium: gold = s.goldMedium; exp = s.expMedium; friend = s.friendMedium; rep = s.repMedium; break;
				default: gold = s.goldSmall; exp = s.expSmall; friend = s.friendSmall; rep = s.repSmall; break;
			}
			double scale = (1.0 + (player.Level / 50.0)) * (0.5 + (0.25 * errand.steps.Count)) * (share / 100.0);
			gold = (int) Math.Round(gold * scale);
			exp = (int) Math.Round(exp * scale);
			if (gold > 0) {
				GoldUtils.Grant(player, (uint) gold);
			}
			if (exp > 0) {
				LevelingSystem.AddExperience(player, exp);
			}
			if (template != null) {
				friend = template.GiverFriendship(errand, friend); //#1291 - the outcome's change with the tier's, in one
			}
			Residents.AddFriendship(player, errand.giverId, friend + Bathhouse.FriendshipBonus(player)); //#1256 - fresh from the bath house
			Item giver = Residents.ById(errand.giverId); //null when the giver is gone since
			if (share > 0) { //#1302 - an errand that earned nothing raises no standing (nor marks a hero's deed)
				Reputation.Change(player, errand.city, rep, CityFlavour.FactionOf(giver)); //#1282 - and the giver's faction
			}
			if (share <= 0) {
				//#1302 - an errand that earned nothing (a wrong accusation) gives no pouch or bag either
			} else if ((errand.Tier == ErrandTier.Medium) && (Globals.dice.Next(100) < s.pouchChance)) {
				Give(player, "i_mi_pouch");
			} else if (errand.Tier == ErrandTier.Large) {
				Give(player, "i_mi_bag");
				if (Globals.dice.Next(100) < s.boosterChance) {
					Boosters.Add(player, 0);
				}
			}
			NPC giverBody = (giver == null) ? null : Residents.BodyOf(giver);
			if ((giverBody != null) && !string.IsNullOrEmpty(errand.thanks)) {
				giverBody.Say(errand.thanks);
			}
			if (share <= 0) {
				player.SysMessage("Ukol skoncil: " + errand.name + ". Bez odmeny.", ClassAbilities.ClassMessageHue);
				return;
			}
			player.SysMessage("Ukol splnen: " + errand.name + ". Odmena " + gold.ToString(CultureInfo.InvariantCulture) + " zlatych a " +
				exp.ToString(CultureInfo.InvariantCulture) + " zkusenosti.", ClassAbilities.ClassMessageHue);
		}

		private static void Give(Player player, string defname) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			if (def != null) {
				def.Create(player.Backpack);
			}
		}

		/// <summary>The player drops the errand; an item it gave goes back to its owner.</summary>
		public static void GiveUp(Player player, Errand errand) {
			if (Log(player).active.Remove(errand)) {
				TakeBackItems(player, errand);
				player.SysMessage("Vzdal jsi ukol \"" + errand.name + "\".");
				WarCamp.OnFailed(player, errand); //#1280 - an order given up costs standing
			}
		}

		private static void TakeBackItems(Player player, Errand errand) {
			while (TakeMarked(player, errand)) {
			}
			Release(player, errand);
		}

		//the errand's item in the pack, taken away; false if there's none
		private static bool TakeMarked(Player player, Errand errand) {
			Item marked = FindMarked(player.Backpack, ItemMark(player, errand)) ?? FindMarked(player.Bank, ItemMark(player, errand));
			if (marked == null) {
				return false;
			}
			marked.Delete();
			return true;
		}

		private static Item FindMarked(Container container, string mark) {
			if (container == null) {
				return null;
			}
			foreach (AbstractItem abstractItem in container) {
				if (abstractItem is Item item) {
					if (string.Equals(item.GetTag(ItemTK) as string, mark, StringComparison.Ordinal)) {
						return item;
					}
					if (item is Container inside) {
						Item found = FindMarked(inside, mark);
						if (found != null) {
							return found;
						}
					}
				}
			}
			return null;
		}

		//a bag of the def in the pack holding at least `count`: not merely the first one found
		private static Collector FullBag(Container container, ThingDef def, int count) {
			if ((container == null) || (def == null)) {
				return null;
			}
			foreach (AbstractItem item in container) {
				if ((item is Collector bag) && (bag.Def == def) && (bag.Collected >= count)) {
					return bag;
				}
				if (item is Container inside) {
					Collector found = FullBag(inside, def, count);
					if (found != null) {
						return found;
					}
				}
			}
			return null;
		}

		//an errand's own item (a letter to deliver) never counts as something fetched
		internal static bool IsErrandItem(AbstractItem item) {
			return (item is Item i) && (i.GetTag(ItemTK) != null);
		}

		internal static int CountIn(Container container, ItemDef def) {
			int count = 0;
			if (container == null) {
				return 0;
			}
			foreach (AbstractItem item in container) {
				if ((item.Def == def) && !IsErrandItem(item) && !IsMagic(item)) {
					count += item.Amount;
				} else if (item is Container inside) {
					count += CountIn(inside, def);
				}
			}
			return count;
		}

		/// <summary>#1293 - a magic item is never an errand's goods: a commission for a dagger mustn't take the player's magic one.</summary>
		private static bool IsMagic(AbstractItem item) {
			return (item is Item it) && MagicItems.IsMagic(it);
		}

		internal static int Consume(Container container, ItemDef def, int count) {
			List<AbstractItem> items = new List<AbstractItem>();
			foreach (AbstractItem item in container) {
				items.Add(item);
			}
			foreach (AbstractItem item in items) {
				if (count <= 0) {
					break;
				}
				if ((item.Def == def) && !IsErrandItem(item) && !IsMagic(item)) {
					int amount = item.Amount;
					if (amount <= count) {
						count -= amount;
						item.Delete();
					} else {
						item.Amount = amount - count;
						count = 0;
					}
				} else if (item is Container inside) {
					count = Consume(inside, def, count);
				}
			}
			return count;
		}

		/// <summary>"Merimas Brandor, Pekarna": whom the step is at and where they are at this hour, for the journal.</summary>
		public static string Where(ErrandStep step) {
			if (step.Kind == ErrandStepKind.Things) { //#1293 - no resident: the place of its things
				CityVenue venue = (step.venueId.Length > 0) ? VenueRegistry.ById(step.venueId) : null;
				string elsewhere = string.IsNullOrEmpty(step.thingCity) ? "" : step.thingCity.Replace('_', ' ') + ", "; //#1300 - another city's
				switch ((ErrandThingPlace) step.thingPlace) {
					case ErrandThingPlace.Venue: return (venue == null) ? "ve meste" : venue.Label;
					case ErrandThingPlace.Streets: return elsewhere + "v ulicich kolem namesti";
					default: return elsewhere + "na ceste za mestem";
				}
			}
			Item gem = Residents.ById(step.residentId);
			if (gem == null) {
				return "?";
			}
			CityVenue place = ResidentSchedule.PlaceFor(gem, VenueClock.Shift) ?? Residents.WorkplaceOf(gem);
			return (place == null) ? Residents.NameOf(gem) : Residents.NameOf(gem) + ", " + place.Label;
		}

		/// <summary>The quest arrow to the resident of the errand's current step.</summary>
		public static bool PointTo(Player player, Errand errand) {
			ErrandStep step = errand.Current;
			if ((step != null) && (step.Kind == ErrandStepKind.Things)) { //#1293 - the nearest of its things
				Thing nearest = null;
				foreach (Thing thing in ThingsOf(step, ItemMark(player, errand))) {
					if ((thing.M == player.M) && ((nearest == null) || (Point2D.GetSimpleDistance(thing, player) < Point2D.GetSimpleDistance(nearest, player)))) {
						nearest = thing;
					}
				}
				if (nearest == null) {
					player.SysMessage("Nevis, kde to hledat.");
					return false;
				}
				player.QuestArrow(true, nearest);
				return true;
			}
			Item gem = (step == null) ? null : Residents.ById(step.residentId);
			NPC body = (gem == null) ? null : Residents.BodyOf(gem);
			if ((body == null) || (body.M != player.M)) {
				player.SysMessage("Nevis, kde ho hledat.");
				return false;
			}
			player.QuestArrow(true, body);
			return true;
		}

		/// <summary>#1312 - Erebor's .arrowend (arrowquest 0): the quest arrow off.</summary>
		[SteamFunction]
		public static void ArrowEnd(Player self) {
			self.QuestArrow(false, self);
		}

		// ---- builders for templates ----

		public static ErrandStep Talk(Item resident, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Talk, residentId = Residents.IdOf(resident), task = task, say = say, reply = reply };
		}

		public static ErrandStep Deliver(Item resident, string itemDef, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Deliver, residentId = Residents.IdOf(resident), itemDef = itemDef ?? "", task = task, say = say, reply = reply };
		}

		/// <summary>#1312 - the task names the item in brackets, as the player sees it: only that def counts ("polena" was any of
		/// a dozen woods).</summary>
		public static ErrandStep Fetch(Item resident, string itemDef, int count, string task, string say, string reply) {
			string name = ItemName(itemDef);
			if ((name.Length > 0) && !task.EndsWith(name + ")", StringComparison.Ordinal)) {
				task = task.EndsWith(")", StringComparison.Ordinal) ? task.Substring(0, task.Length - 1) + ", " + name + ")" : task + " (" + name + ")"; //"(chce vino, wine)", one bracket
			}
			return new ErrandStep { kind = (int) ErrandStepKind.Fetch, residentId = Residents.IdOf(resident), itemDef = itemDef, count = count, task = task, say = say, reply = reply };
		}

		/// <summary>An item def's name as a single item shows it, "" if there's no such def.</summary>
		public static string ItemName(string itemDef) {
			return (ThingDef.GetByDefname(itemDef) is ItemDef def) ? (def.Name ?? "") : "";
		}

		/// <summary>#1312 - the things an errand wants brought, for its offer: "20x log".</summary>
		public static string Wanted(Errand errand) {
			List<string> wanted = new List<string>();
			foreach (ErrandStep step in errand.steps) {
				string name = (step.Kind == ErrandStepKind.Fetch) ? ItemName(step.itemDef) : "";
				if (name.Length > 0) {
					wanted.Add(step.count.ToString(CultureInfo.InvariantCulture) + "x " + name);
				}
			}
			return string.Join(", ", wanted);
		}

		public static ErrandStep Collect(Item resident, string bagDef, int count, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Collect, residentId = Residents.IdOf(resident), itemDef = bagDef, count = count, task = task, say = say, reply = reply };
		}

		public static ErrandStep Wait(Item resident, int fromHour, int toHour, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Wait, residentId = Residents.IdOf(resident), fromHour = fromHour, toHour = toHour, task = task, say = say, reply = reply };
		}

		public static ErrandStep Choose(Item resident, string task, string[] choices, string[] replies) {
			if ((choices.Length < 1) || (choices.Length > 9)) {
				throw new SEException("an errand's choice has 1 to 9 answers"); //a dialog button holds the answer as one digit
			}
			ErrandStep step = new ErrandStep { kind = (int) ErrandStepKind.Choose, residentId = Residents.IdOf(resident), task = task };
			step.choices.AddRange(choices);
			step.choiceReplies.AddRange(replies);
			return step;
		}

		/// <summary>#1293 - a step of things in the world: `count` of `def` put at `place` (in `venue` for a venue) as the step begins,
		/// to catch, kill or use; `flees` makes its NPCs run from the player. `reply` is said (as a message) when the last is done.
		/// `inCity` (#1300) puts streets or road things in another city than the errand's.</summary>
		public static ErrandStep Things(string def, int count, ErrandThingAction action, ErrandThingPlace place, CityVenue venue, bool flees, string task, string reply,
				string inCity = null) {
			return new ErrandStep {
				kind = (int) ErrandStepKind.Things, itemDef = def, count = count, thingAction = (int) action, thingPlace = (int) place,
				venueId = (venue == null) ? "" : venue.VenueId, flees = flees, task = task, reply = reply, thingCity = inCity ?? ""
			};
		}
	}

	/// <summary>#1293 - a fleeing errand thing's clock: each second it runs from its player when they come close.</summary>
	[SaveableClass, DeepCopyableClass]
	public class ErrandThingFleeTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_errandThingFlees_");
		internal const double Seconds = 1;
		internal const int Within = 3, Steps = 4, Runs = 3;
		internal static readonly TagKey RunsTK = TagKey.Acquire("_errand_runs_");

		[LoadingInitializer, DeepCopyImplementation]
		public ErrandThingFleeTimer() {
		}

		/// <summary>It runs from its player a few times, then tires and can be caught.</summary>
		protected override void OnTimeout(TagHolder cont) {
			if ((cont is NPC npc) && !npc.IsDeleted && !npc.Flag_Dead && (Errands.ErrandOfThing(npc, out Player owner, false) != null) && !owner.Flag_Dead
					&& (owner.M == npc.M) && (Point2D.GetSimpleDistance(owner, npc) <= Within)) {
				int runs = (npc.GetTag(RunsTK) is int done) ? done : 0;
				if (runs >= Runs) {
					return;
				}
				npc.SetTag(RunsTK, runs + 1);
				NPCAiTimer.GetFor(npc)?.FleeFrom(owner, Steps);
			}
		}
	}

	/// <summary>#1293 - an errand thing's slow check: it goes when its errand is over (done, given up, run out), its step passed,
	/// or its player gone, whether or not anyone is near. A restart keeps it and its timer, so leftovers go after a boot too.</summary>
	[SaveableClass, DeepCopyableClass]
	public class ErrandThingCheckTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_errandThingCheck_");
		internal const double Seconds = 60;

		[LoadingInitializer, DeepCopyImplementation]
		public ErrandThingCheckTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Thing thing) && !thing.IsDeleted && !Errands.IsLiveThing(thing)) {
				thing.Delete();
			}
		}
	}

	/// <summary>#1293 - an errand item's double click (a lamp to light, a cart to search).</summary>
	public class E_ErrandThingItem : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (self.GetTag(Errands.ThingTK) != null) {
				Errands.UseThing(self, clicker);
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
	}

	/// <summary>#1293 - an errand NPC's double click (a runaway cat, a thief, a drunk orc to catch).</summary>
	public class E_ErrandThingNpc : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (self.GetTag(Errands.ThingTK) != null) {
				Errands.UseThing(self, clicker);
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
	}
}
