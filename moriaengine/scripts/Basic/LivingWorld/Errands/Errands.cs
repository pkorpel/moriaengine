using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

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
		/// <summary>How near a resident a player must be to talk to them, and see them.</summary>
		public const int Reach = 3;

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
			NPC companion = Body(step.companionId);
			if (companion != null) { //#1252 - walks with the player; the schedule leaves a follower alone while the errand holds
				companion.PetGoalPoint = null;
				companion.FollowTarget = player;
				companion.SetTag(FollowingTK, 1);
				NPCAiTimer.GetFor(companion)?.WakeUp();
			}
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
			}
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
			errand.index++;
			if (errand.IsDone) {
				Finish(player, errand);
			} else {
				StartStep(player, errand);
				player.SysMessage(errand.name + ": " + errand.Current.task);
			}
			return true;
		}

		/// <summary>The pay of a done errand (design 5.6): gold and experience by tier, level and length, the giver's friendship,
		/// the city's reputation; a medium one may add a magic pouch, a large one a magic bag and maybe a booster.</summary>
		private static void Finish(Player player, Errand errand) {
			ErrandLog log = Log(player);
			log.active.Remove(errand);
			Release(player, errand);
			ErrandTemplate template = ErrandTemplates.Get(errand.key);
			int share = errand.payPercent;
			if (template != null) {
				share = (share * template.PayPercent(errand)) / 100;
				template.OnDone(player, errand);
			}
			share = WarCamp.PayShare(player, errand.city, share); //#1280 - the rank's better share
			log.streak = (log.lastKey == errand.key) ? log.streak + 1 : 1;
			log.lastKey = errand.key;
			log.done++;
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
			Residents.AddFriendship(player, errand.giverId, friend + Bathhouse.FriendshipBonus(player)); //#1256 - fresh from the bath house
			Reputation.Change(player, errand.city, rep);
			if ((errand.Tier == ErrandTier.Medium) && (Globals.dice.Next(100) < s.pouchChance)) {
				Give(player, "i_mi_pouch");
			} else if (errand.Tier == ErrandTier.Large) {
				Give(player, "i_mi_bag");
				if (Globals.dice.Next(100) < s.boosterChance) {
					Boosters.Add(player, 0);
				}
			}
			Item giver = Residents.ById(errand.giverId);
			NPC giverBody = (giver == null) ? null : Residents.BodyOf(giver);
			if ((giverBody != null) && !string.IsNullOrEmpty(errand.thanks)) {
				giverBody.Say(errand.thanks);
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
				if ((item.Def == def) && !IsErrandItem(item)) {
					count += item.Amount;
				} else if (item is Container inside) {
					count += CountIn(inside, def);
				}
			}
			return count;
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
				if ((item.Def == def) && !IsErrandItem(item)) {
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
			Item gem = (step == null) ? null : Residents.ById(step.residentId);
			NPC body = (gem == null) ? null : Residents.BodyOf(gem);
			if ((body == null) || (body.M != player.M)) {
				player.SysMessage("Nevis, kde ho hledat.");
				return false;
			}
			player.QuestArrow(true, body);
			return true;
		}

		// ---- builders for templates ----

		public static ErrandStep Talk(Item resident, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Talk, residentId = Residents.IdOf(resident), task = task, say = say, reply = reply };
		}

		public static ErrandStep Deliver(Item resident, string itemDef, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Deliver, residentId = Residents.IdOf(resident), itemDef = itemDef ?? "", task = task, say = say, reply = reply };
		}

		public static ErrandStep Fetch(Item resident, string itemDef, int count, string task, string say, string reply) {
			return new ErrandStep { kind = (int) ErrandStepKind.Fetch, residentId = Residents.IdOf(resident), itemDef = itemDef, count = count, task = task, say = say, reply = reply };
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
	}
}
