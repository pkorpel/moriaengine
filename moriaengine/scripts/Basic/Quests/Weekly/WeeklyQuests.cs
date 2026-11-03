using System;
using System.Collections.Generic;
using System.Linq;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1090 - Erebor's weekly quests (questy/weekly.scp). A player of level 10 or more, not a craftsman, gets a set of
	/// 3 to 8 small tasks from three pools, renewed 7 days after it came (per player, rolling). Each task pays experience, gold
	/// to the bank and, from the medium pool on, magic bags when done; all of them done pays a set's reward. No NPC gives
	/// them: the set is checked on login (once had), each level-up, the journal and each step of a task. The numbers, texts and
	/// the draw are Erebor's; docs/current/04-gameplay-systems/quests/weekly.md.</summary>
	public static class WeeklyQuests {
		public const int MinLevel = 10, MaxTasks = 8, LowSlots = 4, MediumSlots = 6;
		public const int Low = 0, Medium = 1, High = 2;
		internal const string EatFood = "eatFood", DrinkPotion = "drinkPotion", Loot = "loot", MeetMaster = "meetMaster", HealPlayer = "healPlayer",
			DmgNpc = "dmgNPC", ReceiveDmg = "receiveDmg", StepOnWeb = "stepOnWeb", KillInDung = "killInDung", KillStreak = "KillStreak";
		private static readonly TimeSpan Week = TimeSpan.FromDays(7), StreakGap = TimeSpan.FromSeconds(15);
		private static readonly TagKey setTK = TagKey.Acquire("_weekly_");
		private static readonly TagKey corpseAmountTK = TagKey.Acquire("_weeklyCorpseAmount_");
		private static readonly TagKey spawnedItemTK = TagKey.Acquire("_weeklySpawnedItem_");
		private static readonly TagKey steppedByTK = TagKey.Acquire("_weeklySteppedBy_");

		#region definitions
		private sealed class TaskDef {
			internal readonly int RewardSize, MaxMI, Icon, MoveX, MoveY;
			internal readonly bool Blocked;
			internal readonly Action<Player, WeeklySet, WeeklyTask, int> Setup;

			internal TaskDef(int rewardSize, int maxMI, Action<Player, WeeklySet, WeeklyTask, int> setup, int icon = -1, int moveX = 0, int moveY = 0, bool blocked = false) {
				this.RewardSize = rewardSize;
				this.MaxMI = maxMI;
				this.Setup = setup;
				this.Icon = icon;
				this.MoveX = moveX;
				this.MoveY = moveY;
				this.Blocked = blocked;
			}
		}

		/// <summary>W:18-222. Blocked ones are never drawn: Erebor's own f_weekly_hasQuest kept its player-kill tasks out; the
		/// conquest palantir, the hunting sign and "finish a dungeon" (whose hook Erebor never called) have no system here.</summary>
		private static readonly TaskDef[][] pools = {
			new[] {
				new TaskDef(60, 0, (p, s, t, l) => SetupEatFood(p, s, t, true)),
				new TaskDef(65, 0, (p, s, t, l) => SetupEatFood(p, s, t, false)),
				new TaskDef(53, 0, SetupMeetMaster),
				new TaskDef(55, 0, (p, s, t, l) => SetupDrinkPotion(p, s, t, "i_potion_nightSight", 6)),
				new TaskDef(65, 0, (p, s, t, l) => SetupDrinkPotion(p, s, t, "LESS", 2)),
				new TaskDef(65, 0, SetupStepOnWeb),
				new TaskDef(53, 0, (p, s, t, l) => SetupLoot(s, t, "i_gold", 350000, 130), 0x0EEF),
				new TaskDef(60, 0, (p, s, t, l) => SetupLoot(s, t, "t_fruit", 100, 50, "Lootni predmet:<br>Jakekoliv ovoce"), 0x0993, 8, 0),
				new TaskDef(65, 0, (p, s, t, l) => SetupLoot(s, t, "t_reagent", 3000, 50, "Lootni predmet:<br>Jakekoli reagenty"), 0x18DD, 2, 0),
				new TaskDef(80, 0, (p, s, t, l) => SetupKillInDung(s, t, 550, 20), 0x20E9, -3, 9),
				new TaskDef(75, 0, (p, s, t, l) => SetupHealPlayer(p, s, t, 15, 100), 0x0E21, 4, 0),
				new TaskDef(75, 0, (p, s, t, l) => SetupLoot(s, t, "t_gem", 150, 100, "Lootni predmet:<br>Jakekoli drahokamy"), 0x2C80, 2, 8)
			},
			new[] {
				new TaskDef(80, 0, (p, s, t, l) => SetupDrinkPotion(p, s, t, "", 4)),
				new TaskDef(100, 1, (p, s, t, l) => SetupDmgNpc(p, s, t, 35, 15), 0x36B3, 21, 12),
				new TaskDef(100, 1, (p, s, t, l) => SetupReceiveDmg(p, s, t, "prime", 50, 30), 0x122D, 0, 5),
				new TaskDef(110, 1, (p, s, t, l) => SetupReceiveDmg(p, s, t, "neprime", 10, 30), 0x122D, 0, 5),
				new TaskDef(115, 1, (p, s, t, l) => SetupKillInDung(s, t, 160, 100), 0x212E, 8, 9),
				new TaskDef(115, 2, null, 0x27FA, 2, 4, true), //killPlayer(0)
				new TaskDef(100, 1, null, 3629, 0, 0, true), //conqueror: the palantir, no conquest here
				new TaskDef(93, 1, null, 0x340E, -6, 12, true), //HuntingSign: i_sign_gquest isn't ported
				new TaskDef(120, 2, null, 0x2203, -8, 0, true), //finishDungeon: f_vg_finishDng is called nowhere
				new TaskDef(100, 1, (p, s, t, l) => SetupKillStreak(s, t, 2), 0x21FC, 15, 9)
			},
			new[] {
				new TaskDef(100, 0, (p, s, t, l) => SetupDrinkPotion(p, s, t, "GREAT", 5)),
				new TaskDef(147, 2, null, 0x27FA, 0, 0, true), //killPlayer(1)
				new TaskDef(140, 2, null, 0x27FA, 0, 0, true), //killPlayer(2)
				new TaskDef(160, 2, (p, s, t, l) => SetupKillInDung(s, t, 85, 200), 0x2132, 8, 9),
				new TaskDef(175, 2, null, 0x2203, -8, 0, true) //finishDungeon
			}
		};

		private static readonly string[] meatFoods = { "i_chicken_leg", "i_lamb_leg", "i_bird_cooked", "i_ham", "i_ribs_cooked", "i_bacon_slice", "i_sausages", "i_ribs_cooked", "i_fish_cut_cooked" };
		private static readonly string[] breadFoods = { "i_bread_loaf", "i_bread_french" };
		private static readonly string[] veggieFoods = { "i_fruit_cabbage", "i_fruit_carrot", "i_fruit_corn", "i_fruit_gourd1", "i_fruit_gourd2", "i_fruit_lettuce", "i_fruit_onion",
			"i_fruit_pumpkin", "i_fruit_squash", "i_fruit_turnip" };
		private static readonly string[] fruitFoods = { "i_fruit_dates", "i_fruit_peach", "i_fruit_pear", "i_fruit_lemon", "i_fruit_lime", "i_fruit_grapes", "i_fruit_apple",
			"i_fruit_banana", "i_fruit_coconut", "i_fruit_cantelope", "i_fruit_honeydewmelon", "i_fruit_watermelon" };
		private static readonly string[] otherFoods = { "i_pie_baked", "i_cake", "i_muffin", "i_jar_honey", "i_pizza", "i_cheese_wheel" };

		//W:224-231, [DEFNAMES WEEKLY_POTIONS]: "" is RANDOM_POTION_BOOST
		private static readonly Dictionary<string, KeyValuePair<string, int>[]> magicPotions = new Dictionary<string, KeyValuePair<string, int>[]> {
			{ "", Weighted("i_potion_refresh", 1, "i_potion_heal", 1, "i_potion_cure", 1, "i_potion_clever", 2) },
			{ "LESS", Weighted("i_potion_refreshLess", 1, "i_potion_healLess", 1, "i_potion_cureLess", 1, "i_potion_cleverLess", 2) },
			{ "GREAT", Weighted("i_potion_refreshGreater", 1, "i_potion_healGreat", 1, "i_potion_cureGreat", 1, "i_potion_cleverGreat", 2) }
		};
		private static readonly Dictionary<string, KeyValuePair<string, int>[]> fightPotions = new Dictionary<string, KeyValuePair<string, int>[]> {
			{ "", Weighted("i_potion_refresh", 1, "i_potion_agility", 2, "i_potion_heal", 1, "i_potion_strength", 2, "i_potion_cure", 1) },
			{ "LESS", Weighted("i_potion_refreshLess", 1, "i_potion_agilityLess", 2, "i_potion_healLess", 1, "i_potion_strengthLess", 2, "i_potion_cureLess", 1) },
			{ "GREAT", Weighted("i_potion_refreshGreater", 1, "i_potion_agilityGreat", 2, "i_potion_healGreat", 1, "i_potion_strengthGreat", 2, "i_potion_cureGreat", 1) }
		};

		/// <summary>W:1336-1375: Erebor's skill names, as the task says them; Cartography has a teacher but is never drawn.</summary>
		private static readonly string[] masterSkills = { "Anatomy", "AnimalLore", "Archery", "ArmsLore", "Begging", "Camping", "DetectingHidden", "Enticement", "Forensics",
			"Healing", "Herding", "Hiding", "ItemID", "Magery", "Meditation", "Musicianship", "Parrying", "Peacemaking", "Poisoning", "Provocation", "Snooping", "SpiritSpeak",
			"Stealing", "Stealth", "Tactics", "TasteID", "Tracking", "Veterinary", "Wrestling", "RemoveTrap", "Swordsmanship", "Resist", "Macefighting", "Fencing", "EI" };
		private static readonly Dictionary<string, SkillName> ereborSkillNames = new Dictionary<string, SkillName>(StringComparer.OrdinalIgnoreCase) {
			{ "DetectingHidden", SkillName.DetectHidden }, { "Enticement", SkillName.Discordance }, { "Parrying", SkillName.Parry }, { "Swordsmanship", SkillName.Swords },
			{ "Resist", SkillName.MagicResist }, { "Macefighting", SkillName.Macing }, { "EI", SkillName.EvalInt }
		};

		private static KeyValuePair<string, int>[] Weighted(params object[] pairs) {
			KeyValuePair<string, int>[] list = new KeyValuePair<string, int>[pairs.Length / 2];
			for (int i = 0; i < list.Length; i++) {
				list[i] = new KeyValuePair<string, int>((string) pairs[i * 2], (int) pairs[(i * 2) + 1]);
			}
			return list;
		}

		private static string Pick(KeyValuePair<string, int>[] list) {
			int sum = 0;
			foreach (KeyValuePair<string, int> entry in list) {
				sum += entry.Value;
			}
			int roll = Globals.dice.Next(sum);
			foreach (KeyValuePair<string, int> entry in list) {
				roll -= entry.Value;
				if (roll < 0) {
					return entry.Key;
				}
			}
			return list[list.Length - 1].Key;
		}

		private static string PickOf(string[] list) {
			return list[Globals.dice.Next(list.Length)];
		}

		/// <summary>{a b}: a uniform whole number from a to b.</summary>
		private static int Rand(int from, int to) {
			return Globals.dice.Next(Math.Min(from, to), Math.Max(from, to) + 1);
		}

		private static ItemDef Item(string defname) {
			return ThingDef.GetByDefname(defname) as ItemDef;
		}
		#endregion

		#region the task setups (W:529-776)
		private static void SetupEatFood(Player player, WeeklySet set, WeeklyTask task, bool anyFood) {
			ItemDef food = null;
			for (int attempt = 0; (food == null) && (attempt < 20); attempt++) {
				string defname = anyFood
					? PickOf(Pick(Weighted("meat", 5, "bread", 2, "veggie", 10, "fruit", 12, "other", 6)) switch {
						"meat" => meatFoods,
						"bread" => breadFoods,
						"veggie" => veggieFoods,
						"fruit" => fruitFoods,
						_ => otherFoods
					})
					: PickOf(fruitFoods);
				food = Item(defname);
			}
			int qCount = set.Tasks.Count;
			int weight = Math.Max(1, (int) Math.Round(food.Weight * 10)); //Sphere's <itemdef>.weight is in tenths of a stone
			int total = Math.Max(1, (qCount * Rand(4, 1 + qCount)) / weight);
			if (food.PrettyDefname == "i_ribs_cooked") {
				total *= 8;
			} else if (food.PrettyDefname == "i_fish_cut_cooked") {
				total *= 2;
			}
			task.Kind = EatFood;
			task.Total = total;
			task.Description = "Snez jidlo:<br>" + food.Name;
			task.Subject = food.PrettyDefname;
			task.Icon = food.Model;
		}

		private static void SetupDrinkPotion(Player player, WeeklySet set, WeeklyTask task, string which, int mult) {
			int qCount = set.Tasks.Count;
			string defname = which;
			if (!which.StartsWith("i_", StringComparison.Ordinal)) {
				bool magic = Professions.HasProfession(player, Professions.Necromant) || Professions.HasProfession(player, Professions.Mage) ||
					Professions.HasProfession(player, Professions.Mystic) || Professions.HasProfession(player, Professions.Shaman);
				defname = Pick((magic ? magicPotions : fightPotions)[which]);
			}
			ItemDef potion = Item(defname);
			task.Kind = DrinkPotion;
			task.Total = qCount * Rand(mult, (mult - 2) + qCount);
			task.Description = "Vypij lektvar:<br>" + ((potion == null) ? defname : potion.Name);
			task.Subject = defname;
			task.Icon = (potion == null) ? 0x0F0E : potion.Model;
		}

		private static void SetupLoot(WeeklySet set, WeeklyTask task, string subject, int mult, int delim, string typeDescription = null) {
			int level = set.PlayerLevel;
			task.Kind = Loot;
			task.Total = ((level * mult) + Rand(-500, 0)) / (delim + level);
			task.Subject = subject;
			ItemDef item = Item(subject);
			task.Description = typeDescription ?? ("Lootni predmet:<br>" + ((item == null) ? subject : item.Name));
			task.Icon = (item == null) ? 0 : item.Model;
		}

		private static void SetupMeetMaster(Player player, WeeklySet set, WeeklyTask task, int level) {
			string skill = PickOf(masterSkills);
			task.Kind = MeetMaster;
			task.Total = 1;
			task.Description = "Pozdrav Mistra<br>skillu " + skill;
			task.Subject = skill;
			task.Icon = 0x0E34;
			task.MoveY = -4;
		}

		private static void SetupHealPlayer(Player player, WeeklySet set, WeeklyTask task, int coef, int healerCoef) {
			//W:615-616: the skills without what magic items give, which here are modifiers: the real values
			int healing = player.GetRealSkillValue(SkillName.Healing), anatomy = player.GetRealSkillValue(SkillName.Anatomy);
			bool healer = Professions.HasProfession(player, Professions.Priest) || Professions.HasProfession(player, Professions.Shaman);
			long amount = ((long) set.PlayerLevel * (healer ? healerCoef : coef)) * (healing / 2) * (anatomy / 10) / 10000;
			task.Kind = HealPlayer;
			task.Total = (int) Math.Max(100, amount);
			task.Description = "Osetri bandazemi poskozeni.";
		}

		private static void SetupDmgNpc(Player player, WeeklySet set, WeeklyTask task, int coef, int weakCoef) {
			int level = set.PlayerLevel;
			bool weak = Professions.HasProfession(player, Professions.Priest) || Professions.HasProfession(player, Professions.Craftsman);
			long roll = Globals.dice.Next(level * 10000, ((level + 1) * 10000) + 1);
			task.Kind = DmgNpc;
			task.Total = (int) ((set.Tasks.Count * roll * (weak ? weakCoef : coef)) / 1000);
			task.Description = "Udel poskozeni monstrum.";
		}

		private static void SetupReceiveDmg(Player player, WeeklySet set, WeeklyTask task, string sort, int mult, int coef) {
			string type = (sort == "prime") ? PickOf(new[] { "prime", "jakekoli" }) : PickOf(new[] { "neprime", "jedem", "krvacenim" });
			long total = (long) set.Tasks.Count * mult * player.Vit; //tag.vitlev, the hits a player has
			if (!(Professions.HasProfession(player, Professions.Thief) || Professions.HasProfession(player, Professions.Warrior))) {
				total = (total * coef) / 100;
			}
			task.Kind = ReceiveDmg;
			task.Subject = type;
			if ((type == "jedem") || (type == "krvacenim")) {
				task.Description = "Utrz poskozeni<br>" + type + " v boji.";
				total /= 2;
			} else {
				task.Description = "Utrz " + type + " poskozeni<br>v boji.";
			}
			task.Total = (int) total;
		}

		private static void SetupStepOnWeb(Player player, WeeklySet set, WeeklyTask task, int level) {
			task.Kind = StepOnWeb;
			task.Total = (set.Tasks.Count * Rand(30, 50)) / 10;
			task.Description = "Stoupni na pavucinu.";
			task.Icon = 0x10D5;
			task.MoveX = 25;
			task.MoveY = 14;
		}

		private static void SetupKillInDung(WeeklySet set, WeeklyTask task, int coef, int minXP) {
			int level = set.PlayerLevel;
			task.Kind = KillInDung;
			task.Total = ((level * coef) + Rand(-500, 100)) / (200 + level);
			task.Description = "Zabij npc v dungeonu<br>s alespon " + minXP + "xp";
			task.Subject = minXP.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		private static void SetupKillStreak(WeeklySet set, WeeklyTask task, int coef) {
			int qCount = set.Tasks.Count;
			task.Kind = KillStreak;
			task.Subject = (qCount * coef).ToString(System.Globalization.CultureInfo.InvariantCulture);
			task.Description = "Zabij v kratkem sledu " + (qCount * coef) + "<br>monster, kazde min. s " + (qCount + 2) + "xp.";
			task.Total = (qCount * 2) / 3;
			set.StreakUntilTicks = DateTime.UtcNow.Ticks;
		}
		#endregion

		#region the set
		public static WeeklySet Of(Player player) {
			return player.GetTag(setTK) as WeeklySet;
		}

		private static string Sex(Player player, string male, string female) {
			return player.IsFemale ? female : male;
		}

		/// <summary>f_weekly_check: an expired set goes, and a player who may have one gets a new one. True when it made one,
		/// which drops the step that asked. A craftsman's set stays as it is, Erebor's quirk.</summary>
		public static bool Check(Player player) {
			if ((player.Level < MinLevel) || Professions.HasProfession(player, Professions.Craftsman)) {
				return false;
			}
			WeeklySet set = Of(player);
			if (set != null) {
				if (set.ExpiresTicks >= DateTime.UtcNow.Ticks) {
					return false;
				}
				player.RedMessage("Cas na plneni questu vyprsel.");
				player.RemoveTag(setTK);
			}
			Generate(player);
			return true;
		}

		/// <summary>Erebor's e_weekly_quest @login: only for a player who has had a set.</summary>
		internal static void OnLogin(Player player) {
			if (Of(player) != null) {
				Check(player);
			}
		}

		/// <summary>f_weekly_generate (W:325-452): the count by level, the slots' pools, Erebor's draw (a uniform start, then
		/// the next free one, wrapping), and each task's reward.</summary>
		internal static WeeklySet Generate(Player player) {
			WeeklySet set = new WeeklySet();
			set.PlayerLevel = Math.Min(200, player.Level);
			set.ReceivedTicks = DateTime.UtcNow.Ticks;
			set.ExpiresTicks = set.ReceivedTicks + Week.Ticks;
			int qCount = Math.Min(MaxTasks, (set.PlayerLevel / 20) + 3);
			for (int slot = 0; slot < qCount; slot++) {
				set.Tasks.Add(new WeeklyTask { Pool = (slot < LowSlots) ? Low : ((slot < MediumSlots) ? Medium : High) });
			}
			int level = set.PlayerLevel;
			long startPrize = ((level <= 100) ? (level * 100) : (((level - 100) * 50) + 10000)) + (200000 / (level + 20));
			long sumPrize = 0;
			foreach (WeeklyTask task in set.Tasks) {
				TaskDef[] pool = pools[task.Pool];
				int id = Globals.dice.Next(pool.Length);
				while (pool[id].Blocked || set.Tasks.Exists(other => (other != task) && (other.Kind != null) && (other.Pool == task.Pool) && (other.Id == id))) {
					id = (id + 1) % pool.Length;
				}
				TaskDef def = pool[id];
				task.Id = id;
				def.Setup(player, set, task, level);
				if (def.Icon >= 0) { //W:402-410 run after the setup: the table's icon wins
					task.Icon = def.Icon;
					task.MoveX = def.MoveX;
					task.MoveY = def.MoveY;
				}
				task.Total = Math.Max(1, task.Total);
				long single = startPrize * def.RewardSize;
				sumPrize += single;
				task.RewGold = (int) (single / 250);
				task.RewXP = (int) (single / 2200);
				task.RewMI = (task.Pool == Low) ? 0 : (int) Math.Min(single / 850000, def.MaxMI);
				Logger.WriteInfo(true, "Weekly: " + player.Name + " (" + player.Account?.Name + ") byl vygenerovan Weekly quest " + task.Total + "x " + task.Description);
			}
			long prize = sumPrize - (sumPrize / 2);
			set.RewGold = (int) (prize / 250);
			set.RewXP = (int) ((level * 2) + (prize / 2200));
			set.RewMI = (int) ((prize / 850000) + 1);
			player.SetTag(setTK, set);
			player.SysMessage(Sex(player, "Prijal", "Prijala") + " jsi novy seznam ukolu. Pro zobrazeni zadej prikaz .questy", ClassAbilities.ClassMessageHue);
			Logger.WriteInfo(true, "Weekly: " + player.Name + " (" + player.Account?.Name + ") prijal serii " + qCount + " Weekly questu na levelu " + level);
			return set;
		}

		/// <summary>f_weekly_increase: a step of a task; done, it pays at once.</summary>
		internal static bool Increase(Player player, WeeklyTask task, int add, bool showMessage) {
			if (Check(player)) {
				return false;
			}
			WeeklySet set = Of(player);
			if ((set == null) || !set.Tasks.Contains(task) || task.IsDone || (add <= 0)) {
				return false;
			}
			if (task.Done + (long) add >= task.Total) {
				task.Done = task.Total;
				Payout(player, set, task);
				return true;
			}
			task.Done += add;
			if (showMessage) {
				player.SysMessage(Sex(player, "Splnil", "Splnila") + " jsi cast ukolu tydenniho questu: " + task.Done + "/" + task.Total, ClassAbilities.ClassMessageHue);
			}
			return false;
		}

		private static void Pay(Player player, int xp, int gold, int bags) {
			LevelingSystem.AddExperience(player, xp);
			for (int left = gold; left > 0; left -= 60000) { //giveGold: piles of 60000
				GoldUtils.GrantToBank(player, (uint) Math.Min(left, 60000));
			}
			ItemDef bag = Item("i_mi_bag");
			for (int i = 0; (i < bags) && (bag != null); i++) {
				bag.Create(player.Bank);
			}
		}

		private static void Payout(Player player, WeeklySet set, WeeklyTask task) {
			Pay(player, task.RewXP, task.RewGold, task.RewMI);
			Logger.WriteInfo(true, "Weekly: " + player.Name + " (" + player.Account?.Name + ") splnil " + task.Total + "x " + task.Description + " ve Weekly questu a ziskal " +
				task.RewGold + "gp; " + task.RewXP + "expu a  " + task.RewMI + " itemu");
			D_WeeklyCompleted.Show(player, set.Tasks.IndexOf(task));
			if (set.AllDone) {
				Pay(player, set.RewXP, set.RewGold, set.RewMI);
				Logger.WriteInfo(true, "Weekly: " + player.Name + " (" + player.Account?.Name + ") splnil vsechny Weekly questy a ziskal za to odmenu " + set.RewGold + "gp; " +
					set.RewXP + "expu a " + set.RewMI + " itemu");
				player.SysMessage(Sex(player, "Splnil", "Splnila") + " jsi vsechny ukoly tydennich questu! Odmena byla vlozena do banky.", ClassAbilities.ClassMessageHue);
			} else {
				player.SysMessage(Sex(player, "Splnil", "Splnila") + " jsi ukol tydenniho questu. Odmena byla vlozena do banky.", ClassAbilities.ClassMessageHue);
			}
		}

		private static List<WeeklyTask> Open(Player player, string kind) {
			WeeklySet set = (player == null) ? null : Of(player);
			return (set == null) ? new List<WeeklyTask>() : set.Tasks.FindAll(task => (task.Kind == kind) && !task.IsDone);
		}
		#endregion

		#region the triggers
		private static bool IsOfType(Item item, string type) {
			return (item.Type != null) && string.Equals(item.Type.PrettyDefname, type, StringComparison.OrdinalIgnoreCase);
		}

		private static bool Matches(Item item, string subject) {
			return string.Equals(item.Def.PrettyDefname, subject, StringComparison.OrdinalIgnoreCase) || IsOfType(item, subject);
		}

		/// <summary>e_weekly_itemDclick (W:800-843): eating or drinking, counted on the double click; food only while the
		/// player can still eat, a potion only off its cooldown. One click counts for one task.</summary>
		internal static void OnItemDClick(Player player, Item item) {
			if ((item == null) || player.Flag_Dead || ParalyzeEffectPluginDef.IsParalyzed(player)) {
				return;
			}
			foreach (WeeklyTask task in Open(player, EatFood).Concat(Open(player, DrinkPotion))) {
				if (!Matches(item, task.Subject)) {
					continue;
				}
				if ((IsOfType(item, "t_food") || IsOfType(item, "t_fruit")) && (PlayerHunger.FoodOf(player) >= PlayerHunger.MaxFood)) {
					return;
				}
				if ((IsOfType(item, "t_speedpotions") || IsOfType(item, "t_allpotions")) && !PotionDrinking.CanDrinkNow(player)) {
					return;
				}
				Increase(player, task, 1, true);
				return;
			}
		}

		/// <summary>creaturestart's tag(spawned_item) (systems/spawns.scp): what a monster carries once its gold is set, for every
		/// monster with i_monster_create that wasn't raised again, spawned or added by hand alike. The name misleads: it isn't
		/// about spawn gems.</summary>
		internal static void MarkSpawnedItems(NPC npc) {
			foreach (AbstractItem item in npc.Backpack) {
				item.SetTag(spawnedItemTK, 1);
			}
		}

		/// <summary>Corpse.InitFromChar: how much there was at death of each such item, Erebor's iic_amount.</summary>
		internal static void MarkCorpseLoot(Character dead, Corpse corpse) {
			if (!(dead is NPC)) {
				return;
			}
			foreach (AbstractItem item in corpse) {
				if (item.GetTag(spawnedItemTK) != null) {
					item.SetTag(corpseAmountTK, item.Amount);
				}
			}
		}

		/// <summary>e_weekly_itemLoot (W:847-881): an item a monster had from its creaturestart, lifted from its corpse with no more
		/// than it had at death (Erebor logged the attempt otherwise). Counted once.</summary>
		internal static void OnLoot(Player player, AbstractItem item) {
			if (!(item.TopObj() is Corpse corpse) || (corpse.Owner != null) || !(item.GetTag(corpseAmountTK) is int had)) {
				return;
			}
			List<WeeklyTask> tasks = Open(player, Loot);
			if (tasks.Count == 0) {
				return;
			}
			if (had < item.Amount) {
				Logger.WriteWarning("Weekly: " + player.Name + " lootl " + item.Name + "(" + item.Uid + ") z tela " + corpse.Name + "(" + corpse.Uid +
					") u nehoz se zmenil od smrti pocet kusu z " + had + " na " + item.Amount + " pri plneni Weekly.");
				return;
			}
			foreach (WeeklyTask task in tasks) {
				if (Matches((Item) item, task.Subject)) {
					item.RemoveTag(corpseAmountTK);
					Increase(player, task, item.Amount, true);
					return;
				}
			}
		}

		/// <summary>f_weekly_meetMaster, in spk_mistr after its ghost check: the teacher of the task's skill greets back.</summary>
		internal static void OnGreetTeacher(NPC teacher, Player player, SkillName skill) {
			foreach (WeeklyTask task in Open(player, MeetMaster)) {
				SkillName wanted;
				if (!ereborSkillNames.TryGetValue(task.Subject, out wanted) && !Enum.TryParse(task.Subject, true, out wanted)) {
					continue;
				}
				if (wanted == skill) {
					teacher.Say("Taky Te zdravim, " + player.Name + ".");
					Increase(player, task, 1, true);
					return;
				}
			}
		}

		/// <summary>f_weekly_quest_healing: what a bandage healed of a person of the healer's own realm, the healer too.</summary>
		internal static void OnBandageHeal(Character healer, Character patient, int healed) {
			if ((healer is Player player) && (healed > 0) && (healer.Realm == patient.Realm)) {
				foreach (WeeklyTask task in Open(player, HealPlayer)) {
					Increase(player, task, healed, true);
				}
			}
		}

		/// <summary>A blow's damage, both ways (W:889-953). Dealt: any target, as Erebor's never checked it was a monster.
		/// Taken: from a player or a spawned monster of another realm, by no GM; silent, as Erebor's.</summary>
		internal static void OnSwingDamage(Character attacker, Character defender, int damage) {
			if (damage <= 0) {
				return;
			}
			if (attacker is Player dealer) {
				foreach (WeeklyTask task in Open(dealer, DmgNpc)) {
					Increase(dealer, task, damage, false);
				}
			}
			if ((defender is Player taker) && CountsAsFoe(attacker, taker)) {
				foreach (WeeklyTask task in Open(taker, ReceiveDmg)) {
					if ((task.Subject == "prime") || (task.Subject == "jakekoli")) {
						Increase(taker, task, damage, false);
					}
				}
			}
		}

		/// <summary>The poison's tick, Erebor's i_memory_jed @timer: its strength, not the damage done (W:955-992).</summary>
		internal static void OnPoisonTick(Character self, Character dealer, int strength) {
			if ((self is Player taker) && ((dealer == null) || CountsAsFoe(dealer, taker))) {
				foreach (WeeklyTask task in Open(taker, ReceiveDmg)) {
					if ((task.Subject == "neprime") || (task.Subject == "jakekoli") || (task.Subject == "jedem")) {
						Increase(taker, task, strength, false);
					}
				}
			}
		}

		/// <summary>f_weekly_receiveDmg from the bleeding's tick (W:1007-1040).</summary>
		internal static void OnBleed(Character self, Character source, int damage) {
			if ((self is Player taker) && (damage > 0) && ((source == null) || CountsAsFoe(source, taker))) {
				foreach (WeeklyTask task in Open(taker, ReceiveDmg)) {
					if ((task.Subject == "neprime") || (task.Subject == "jakekoli") || (task.Subject == "krvacenim")) {
						Increase(taker, task, damage, false);
					}
				}
			}
		}

		private static bool CountsAsFoe(Character attacker, Player taker) {
			if ((attacker == null) || attacker.IsGM || (attacker.Realm == taker.Realm) || taker.Flag_Invulnerable) {
				return false;
			}
			return (attacker is Player) || ((attacker.GetTag(TagKey.Acquire("spawnGem")) is Thing gem) && !gem.IsDeleted);
		}

		/// <summary>e_weekly_stepOnWeb (W:1187-1201): each web counts once, for the first player with the task on it.</summary>
		internal static void OnStepOnItem(Player player, Item item) {
			if (player.Flag_Dead || !IsOfType(item, "t_web") || (item.GetTag(steppedByTK) != null)) {
				return;
			}
			foreach (WeeklyTask task in Open(player, StepOnWeb)) {
				item.SetTag(steppedByTK, (int) player.Uid);
				Increase(player, task, 1, true);
				return;
			}
		}

		/// <summary>@playerKill, for each player a monster's experience was shared to, with their share (W:1045-1132).</summary>
		internal static void OnKillCredit(Player player, NPC victim, int share) {
			WeeklySet set = (player == null) ? null : Of(player);
			if ((set == null) || (victim == null)) {
				return;
			}
			if (MonsterLoot.IsDungeonMapPlane(player.M)) {
				WeeklyTask best = null;
				foreach (WeeklyTask task in Open(player, KillInDung)) {
					int min = int.Parse(task.Subject, System.Globalization.CultureInfo.InvariantCulture);
					if ((min <= share) && ((best == null) || (min > int.Parse(best.Subject, System.Globalization.CultureInfo.InvariantCulture)))) {
						best = task;
					}
				}
				if (best != null) {
					Increase(player, best, 1, true);
				}
			}
			if (share >= set.Tasks.Count + 2) {
				foreach (WeeklyTask task in Open(player, KillStreak)) {
					long now = DateTime.UtcNow.Ticks;
					task.Streak = (now > set.StreakUntilTicks) ? 1 : (task.Streak + 1);
					set.StreakUntilTicks = now + StreakGap.Ticks;
					if (task.Streak >= int.Parse(task.Subject, System.Globalization.CultureInfo.InvariantCulture)) {
						task.Streak = 0;
						Increase(player, task, 1, true);
					}
				}
			}
		}
		#endregion
	}
}
