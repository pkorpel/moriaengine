using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1090 - server side of the weekly quests' tests (Tests/Protocol/QuestJournalTests.cs).</summary>
	public static class WeeklyQuestTests {
		private static void Report(string name, bool ok) {
			Console.WriteLine("WEEKLY: " + name + "=" + ok);
		}

		private static Player Ready(Character self, int level) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = Professions.Warrior;
			player.Realm = Realm.Gondor;
			player.Level = level;
			player.RemoveTag(TagKey.Acquire("_weekly_"));
			return player;
		}

		/// <summary>The set: how many by level, Erebor's worked example of a reward, no blocked or repeated task, expiry.</summary>
		[SteamFunction]
		public static void TestWeeklySets(Character self) {
			Player player = Ready(self, 9);
			Report("underTenNothing", !WeeklyQuests.Check(player) && (WeeklyQuests.Of(player) == null));
			bool counts = true, blockedNever = true, noRepeat = true, pools = true;
			foreach (int[] levelCount in new[] { new[] { 10, 3 }, new[] { 20, 4 }, new[] { 39, 4 }, new[] { 40, 5 }, new[] { 80, 7 }, new[] { 100, 8 }, new[] { 190, 8 } }) {
				for (int round = 0; round < 40; round++) {
					Ready(self, levelCount[0]);
					WeeklyQuests.Check(player);
					WeeklySet set = WeeklyQuests.Of(player);
					counts &= set.Tasks.Count == levelCount[1];
					HashSet<string> seen = new HashSet<string>();
					for (int slot = 0; slot < set.Tasks.Count; slot++) {
						WeeklyTask task = set.Tasks[slot];
						noRepeat &= seen.Add(task.Pool + "/" + task.Id);
						blockedNever &= !((task.Pool == WeeklyQuests.Medium) && (task.Id >= 5) && (task.Id <= 8)) && !((task.Pool == WeeklyQuests.High) && (task.Id != 0) && (task.Id != 3));
						pools &= task.Pool == ((slot < 4) ? WeeklyQuests.Low : ((slot < 6) ? WeeklyQuests.Medium : WeeklyQuests.High));
						pools &= (task.Total >= 1) && (task.Kind != null) && (task.Description != null);
					}
				}
			}
			Report("countsByLevel", counts);
			Report("blockedNeverDrawn", blockedNever);
			Report("noTaskTwice", noRepeat);
			Report("slotsByPool", pools);

			//W:416-443 at level 10: start 7666, a low task of reward size 60 pays 1839 gp and 209 xp
			bool example = false;
			for (int round = 0; (round < 200) && !example; round++) {
				Ready(self, 10);
				WeeklyQuests.Check(player);
				WeeklyTask task = WeeklyQuests.Of(player).Tasks.Find(t => (t.Pool == WeeklyQuests.Low) && (t.Id == 0));
				example = (task != null) && (task.RewGold == 1839) && (task.RewXP == 209) && (task.RewMI == 0);
			}
			Report("ereborsWorkedExample", example);

			Player crafter = Ready(self, 50);
			crafter.Profession = Professions.Craftsman;
			Report("noSetForACraftsman", !WeeklyQuests.Check(crafter) && (WeeklyQuests.Of(crafter) == null));
			crafter.Profession = Professions.Warrior;

			Ready(self, 30);
			WeeklyQuests.Check(player);
			WeeklySet first = WeeklyQuests.Of(player);
			bool lasts = Math.Abs(first.ExpiresTicks - first.ReceivedTicks - TimeSpan.FromDays(7).Ticks) < TimeSpan.TicksPerSecond;
			Report("aWeekLong", lasts && !WeeklyQuests.Check(player) && (WeeklyQuests.Of(player) == first));
			first.ExpiresTicks = DateTime.UtcNow.Ticks - 1;
			Report("expiredIsRenewed", WeeklyQuests.Check(player) && (WeeklyQuests.Of(player) != first));
			Console.WriteLine("WEEKLY_SETS_DONE");
		}

		/// <summary>A set of level 50 whose tasks are made what a test needs.</summary>
		private static WeeklySet Fixed(Player player, params WeeklyTask[] tasks) {
			player.Level = 50;
			player.RemoveTag(TagKey.Acquire("_weekly_"));
			WeeklyQuests.Check(player);
			WeeklySet set = WeeklyQuests.Of(player);
			set.Tasks.Clear();
			set.Tasks.AddRange(tasks);
			return set;
		}

		private static WeeklyTask Task(string kind, string subject, int total) {
			return new WeeklyTask { Kind = kind, Subject = subject, Total = total, Description = kind, RewGold = 1000, RewXP = 10 };
		}

		[SteamFunction]
		public static void TestWeeklyTriggers(Character self) {
			Player player = Ready(self, 50);
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);

			//eating counts on the double click, only while there's room to eat
			WeeklyTask eat = Task("eatFood", "i_fruit_apple", 5);
			WeeklySet set = Fixed(player, eat);
			Item apple = (Item) ((ItemDef) ThingDef.GetByDefname("i_fruit_apple")).Create(player.Backpack);
			PlayerHunger.SetFood(player, 5);
			WeeklyQuests.OnItemDClick(player, apple);
			PlayerHunger.SetFood(player, PlayerHunger.MaxFood);
			WeeklyQuests.OnItemDClick(player, apple);
			Report("eatingCountsWhileHungry", eat.Done == 1);
			apple.Delete();

			//a skill teacher greets back the one sent to it
			WeeklyTask meet = Task("meetMaster", "Anatomy", 1);
			Fixed(player, meet, Task("stepOnWeb", null, 2));
			NPC teacher = (NPC) ((NPCDef) ThingDef.GetByDefname("c_m_anatomy")).Create(player.X + 1, player.Y, player.Z, player.M);
			WeeklyQuests.OnGreetTeacher(teacher, player, SkillName.Anatomy);
			Report("theTeacherGreetsBack", meet.IsDone);
			teacher.Delete();

			//a web counts once
			set = WeeklyQuests.Of(player);
			WeeklyTask web = set.Tasks[1];
			Item spiderweb = (Item) ((ItemDef) ThingDef.GetByDefname("i_spider_web")).Create(player.X, player.Y + 1, player.Z, player.M);
			WeeklyQuests.OnStepOnItem(player, spiderweb);
			WeeklyQuests.OnStepOnItem(player, spiderweb);
			Report("aWebCountsOnce", web.Done == 1);
			spiderweb.Delete();

			//the user, live: a titan's gold didn't count. Erebor's spawned_item is creaturestart's mark on every monster with gold,
			//spawn gem or not; what comes into the pack later isn't marked
			WeeklyTask lootGold = Task("loot", "i_gold", 1000000), lootBandages = Task("loot", "i_bandage", 1000);
			Fixed(player, lootGold, lootBandages);
			Item gem = (Item) ((ItemDef) ThingDef.GetByDefname("i_worldgem_bit")).Create(player.X, player.Y, player.Z, player.M);
			NPC orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(player.X + 1, player.Y, player.Z, player.M);
			MonsterLoot.FlushCreatureStart(orc);
			((Item) ((ItemDef) ThingDef.GetByDefname("i_bandage")).Create(orc.Backpack)).Amount = 40;
			orc.CauseDeath(null);
			int goldInCorpse = 0;
			foreach (Thing thing in player.GetMap().GetThingsInRectangle(new ImmutableRectangle(player.X, player.Y - 1, player.X + 2, player.Y + 1))) {
				if (thing is Corpse corpse) {
					foreach (AbstractItem inside in new List<AbstractItem>(corpse)) {
						if (inside.Def == ThingDef.GetByDefname("i_gold")) {
							goldInCorpse += inside.Amount;
						}
						if ((inside.Def == ThingDef.GetByDefname("i_gold")) || (inside.Def == ThingDef.GetByDefname("i_bandage"))) {
							WeeklyQuests.OnLoot(player, inside);
							inside.Delete();
						}
					}
					corpse.Delete();
				}
			}
			Report("aMonstersGoldCountsSpawnedOrNot", (goldInCorpse > 0) && (lootGold.Done == goldInCorpse));
			Report("whatCameLaterDoesnt", lootBandages.Done == 0);

			//blows: dealt to anything, taken only from a foe of another realm
			WeeklyTask dealt = Task("dmgNPC", null, 100000), taken = Task("receiveDmg", "jakekoli", 100000), bled = Task("receiveDmg", "krvacenim", 100000);
			Fixed(player, dealt, taken, bled);
			NPC wolf = (NPC) ((NPCDef) ThingDef.GetByDefname("c_wolf")).Create(player.X + 1, player.Y, player.Z, player.M);
			WeeklyQuests.OnSwingDamage(player, wolf, 30);
			WeeklyQuests.OnSwingDamage(wolf, player, 20);
			wolf.SetTag(TagKey.Acquire("spawnGem"), gem);
			WeeklyQuests.OnSwingDamage(wolf, player, 20);
			WeeklyQuests.OnBleed(player, wolf, 7);
			Report("damageDealtCounts", dealt.Done == 30);
			Report("onlyASpawnedFoesBlowCounts", (taken.Done == 27) && (bled.Done == 7)); //20 from the blow, 7 from the bleeding, which "jakekoli" takes too
			wolf.Delete();
			gem.Delete();

			//a streak: kills close together, each worth enough
			WeeklyTask streak = Task("KillStreak", "2", 1);
			set = Fixed(player, streak, Task("eatFood", "i_fruit_apple", 99));
			NPC victim = (NPC) ((NPCDef) ThingDef.GetByDefname("c_wolf")).Create(player.X + 1, player.Y, player.Z, player.M);
			WeeklyQuests.OnKillCredit(player, victim, 3); //needs qCount + 2 = 4
			bool tooSmall = streak.Done == 0;
			WeeklyQuests.OnKillCredit(player, victim, 4);
			WeeklyQuests.OnKillCredit(player, victim, 4);
			Report("aStreakOfTwoCounts", tooSmall && streak.IsDone);
			victim.Delete();

			//done pays at once: experience, gold to the bank; all done adds the set's reward
			Item bankGold = null;
			foreach (AbstractItem item in new List<AbstractItem>(player.Bank)) {
				item.Delete();
			}
			WeeklyTask last = set.Tasks[1];
			set.RewGold = 500;
			set.RewXP = 0;
			set.RewMI = 1;
			long bankBefore = GoldUtils.CountGoldIn(player.Bank);
			WeeklyQuests.Increase(player, last, 99, true);
			foreach (AbstractItem item in player.Bank) {
				if (item.Def == ThingDef.GetByDefname("i_mi_bag")) {
					bankGold = (Item) item;
				}
			}
			Report("aTaskAndTheSetPayIntoTheBank", set.AllDone && (GoldUtils.CountGoldIn(player.Bank) == bankBefore + 1000 + 500) && (bankGold != null));
			Console.WriteLine("WEEKLY_TRIGGERS_DONE");
		}

		/// <summary>For the journal: a fresh level 50 set.</summary>
		[SteamFunction]
		public static void TestWeeklyFreshSet(Character self) {
			Player player = Ready(self, 50);
			WeeklyQuests.Check(player);
			Console.WriteLine("WEEKLY_FRESH: tasks=" + WeeklyQuests.Of(player).Tasks.Count + " first=" + WeeklyQuests.Of(player).Tasks[0].Description.Replace("<br>", " ") + " |");
		}

		/// <summary>The first task done, which shows the popup.</summary>
		[SteamFunction]
		public static void TestWeeklyFinishFirst(Character self) {
			Player player = (Player) self;
			WeeklyTask task = WeeklyQuests.Of(player).Tasks[0];
			WeeklyQuests.Increase(player, task, task.Total, true);
			Console.WriteLine("WEEKLY_FINISHED: " + task.IsDone);
		}
	}
}
