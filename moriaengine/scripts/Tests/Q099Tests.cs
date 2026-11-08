using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1278 - q099 (Podivin) on the server, through the dialog's own buttons: outcasts only, the four bags, Mining 40, the
	/// cheque of exactly 250 000, the 15 levels, both realms back and a new exile starting again.
	/// Tests/Protocol/Q041Q099Tests.cs greets him through a client first.</summary>
	public static class Q099Tests {
		private static readonly TagKey npcTK = TagKey.Acquire("_q099test_npc_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q099_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestQ099Setup(Character self) {
			Player player = (Player) self;
			NPC podivin = (NPC) ((NPCDef) ThingDef.GetByDefname(Q099.PodivinDef)).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(podivin, 900);
			player.SetTag(npcTK, podivin);
			player.Realm = Realm.GondorOutcast;
			player.Level = 40;
			player.SetRealSkillValue((int) SkillName.Mining, 0);
			Q099.SetStage(player, 0);
			Console.WriteLine("Q099_SETUP_DONE: podivin=" + podivin.Uid);
		}

		[SteamFunction]
		public static void TestQ099Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q099_BUTTON: id=" + SingletonScript<D_Q099Podivin>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ099State(Character self) {
			Console.WriteLine("Q099_STATE: stage=" + Q099.GetStage((Player) self));
		}

		private static CashiersItem Cheque(Player player, int value) {
			CashiersItem cheque = (CashiersItem) ThingDef.GetByDefname(Q099.ChequeDef).Create(player.Backpack);
			cheque.Value = value;
			return cheque;
		}

		[SteamFunction]
		public static void TestQ099Chain(Character self) {
			Player player = (Player) self;
			NPC podivin = (NPC) player.GetTag(npcTK);
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1;
			try {
				Chain(player, podivin);
			} finally {
				player.Account.PLevel = plevel;
				player.Realm = Realm.Gondor;
			}
			podivin.Delete();
			Console.WriteLine("Q099_CHAIN_DONE: stage=" + Q099.GetStage(player));
		}

		private static void Chain(Player player, NPC podivin) {
			D_Q099Podivin dialog = SingletonScript<D_Q099Podivin>.Instance;
			Report("theClientTookTheFirstBag", (Q099.GetStage(player) == 2) && (Collectors.FindIn(player, Q099.BagDefs[0]) != null) && Q099.Journal.IsStarted(player));

			//outcasts only
			player.Realm = Realm.Gondor;
			bool turnedAway = dialog.BuildFor(podivin, player).HtmlOf("Pozdrav").Contains("Di pryc");
			Collectors.FindIn(player, Q099.BagDefs[0]).Collected = 100;
			dialog.Press("q099:2", podivin, player);
			bool notForOthers = Q099.GetStage(player) == 2;
			player.Realm = Realm.GondorOutcast;
			Report("outcastsOnly", turnedAway && notForOthers);

			//the four bags; a button of another stage does nothing
			dialog.Press("q099:4", podivin, player);
			bool wrongButton = Q099.GetStage(player) == 2;
			bool allBags = true;
			for (int stage = 2; stage <= 8; stage += 2) {
				string bag = Q099.BagAt(stage);
				Collector collector = Collectors.FindIn(player, bag);
				allBags &= (collector != null) && (Q099.GetStage(player) == stage);
				if (collector == null) {
					break;
				}
				collector.Collected = collector.MaxCount - 1;
				bool notFull = !dialog.BuildFor(podivin, player).HasResponse("q099:" + stage);
				dialog.Press("q099:" + stage, podivin, player);
				notFull &= Q099.GetStage(player) == stage;
				collector.Collected = collector.MaxCount;
				allBags &= notFull && dialog.BuildFor(podivin, player).HasResponse("q099:" + stage);
				dialog.Press("q099:" + stage, podivin, player);
				allBags &= (Q099.GetStage(player) == stage + 1) && collector.IsDeleted;
				if (stage < 8) {
					allBags &= dialog.BuildFor(podivin, player).HasResponse("q099:" + (stage + 1));
					dialog.Press("q099:" + (stage + 1), podivin, player);
				}
			}
			Report("theFourBags", wrongButton && allBags && (Q099.GetStage(player) == Q099.Ready));

			//Mining 40 first
			bool noMining = !dialog.BuildFor(podivin, player).HasResponse("q099:9");
			dialog.Press("q099:9", podivin, player);
			noMining &= Q099.GetStage(player) == Q099.Ready;
			player.SetRealSkillValue((int) SkillName.Mining, Q099.MiningNeeded);
			Report("miningForty", noMining && dialog.BuildFor(podivin, player).HasResponse("q099:9"));

			//exactly 250 000 in a cheque, then 10 levels for the realm (#1307)
			dialog.Press("q099:9", podivin, player);
			bool noCheque = Q099.GetStage(player) == Q099.Ready;
			CashiersItem small = Cheque(player, 100000);
			dialog.Press("q099:9", podivin, player);
			bool tooLittle = (Q099.GetStage(player) == Q099.Ready) && !small.IsDeleted;
			small.Delete();
			CashiersItem cheque = Cheque(player, Q099.Price);
			player.AddTimer(RealmAbilities.HonorTK, new RealmStatEffect(0)).DueInSeconds = 600; //#1307 - not while magic changes the stats
			dialog.Press("q099:9", podivin, player);
			bool notUnderMagic = (Q099.GetStage(player) == Q099.Ready) && !cheque.IsDeleted && (player.Level == 40);
			player.RemoveTimer(RealmAbilities.HonorTK);
			Notoriety.SetMurderCount(player, 5);
			player.Experience = 100;
			dialog.Press("q099:9", podivin, player);
			Report("redeemedToGondor", noCheque && tooLittle && notUnderMagic && cheque.IsDeleted && (player.Realm == Realm.Gondor) && (player.Level == 40 - Q099.LevelsTaken) && (player.Experience == 0)
				&& (Notoriety.GetMurderCount(player) == 0) && (player.Karma == 0) && (Q099.GetStage(player) == Q099.Done) && Q099.Journal.IsDone(player));

			//a new exile starts again; Mordor's outcast goes back to Mordor
			player.Realm = Realm.MordorOutcast;
			Q099.Talk(podivin, player);
			bool again = Q099.GetStage(player) == 1;
			Q099.SetStage(player, Q099.Ready);
			Cheque(player, Q099.Price);
			dialog.Press("q099:9", podivin, player);
			Report("mordorBackAndAgain", again && (player.Realm == Realm.Mordor) && (player.Karma == Q099.MordorKarma) && (player.Level == 40 - (2 * Q099.LevelsTaken)));

			//never below level 1
			player.Level = 5;
			Q099.TakeLevels(player, Q099.LevelsTaken);
			Report("levelFloor", player.Level == 1);

			//#1307 - the points the lost levels gave are taken back, spent or not: unspent first, then the bought skills' steps
			//(the change back when a step cost more), the stats over the lower budget, and class abilities over the level reset
			ProfessionDef profession = player.Profession;
			player.Profession = Professions.Warrior;
			player.Level = 30;
			player.UnspentSkillPoints = 10;
			//five skills at 75.0, a master's gate: the steps down to 55.0 were bought with points (4 x 2 each), the step from 50.0 was taught
			List<SkillName> bought = new List<SkillName>();
			foreach (SkillDef def in SkillDef.AllSkillDefs) {
				SkillName name = (SkillName) def.Id;
				if ((bought.Count < 5) && LevelingSystem.IsSkillPointSpendable(name) && (Professions.Warrior.GetSkillMinimum(def) <= 550) && (Professions.Warrior.GetSkillCap(def) >= 750)) {
					bought.Add(name);
					player.SetRealSkillValue((int) name, 750);
				}
			}
			int allocated = player.StrPointsAllocated + player.DexPointsAllocated + player.IntPointsAllocated + player.VitPointsAllocated;
			player.Str = (short) (player.Str + player.AttributePointBudget);
			player.StrPointsAllocated += player.AttributePointBudget; //every stat point of level 30 spent
			string ability = ClassAbilityTable.Rear[0].Name;
			ClassAbilities.SetPoints(player, ability, 30 * 2); //every ability point of level 30 spent
			int strBefore = player.Str;
			Q099.TakeLevels(player, Q099.LevelsTaken);
			int taken = 10; //the unspent first
			bool gatesKept = bought.Count == 5;
			foreach (SkillName name in bought) {
				int now = player.GetRealSkillValue(name);
				gatesKept &= now >= 550; //never the taught step from 50.0
				for (int value = now; value < 750; value += LevelingSystem.SkillPointGain) {
					taken += LevelingSystem.SkillRaiseCost(value);
				}
				player.SetRealSkillValue((int) name, Professions.Warrior.GetSkillMinimum(name));
			}
			Report("pointsTakenBack", (Q099.LevelsTaken == 10) && (player.Level == 30 - Q099.LevelsTaken) && //#1307 - the user's 10 levels
				 (taken - player.UnspentSkillPoints == Q099.LevelingSkillPointsPerLevel * Q099.LevelsTaken)
				&& (player.UnspentSkillPoints < 3) && gatesKept && (player.AttributePointBudget == 0) && (player.Str < strBefore)
				&& (ClassAbilities.Points(player, ability) == 0) && (ClassAbilities.Spare(player) == player.Level * 2));
			player.Profession = profession;

			bool plain = true;
			for (int stage = 1; stage <= Q099.Done; stage++) {
				Q099.SetStage(player, stage);
				string text = Q099.GetJournalText(player);
				plain &= (text != null) && Q009Tests.Plain(text);
			}
			Report("journalTextsArePlain", plain);
		}
	}
}
