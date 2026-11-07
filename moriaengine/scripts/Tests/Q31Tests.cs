using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1274 - q31 (Kuchar Mrkvicka) on the server, through the dialog's own buttons: the bag, the 50 birds, the llama figurine
	/// and its llama, the cooks' realms, the three ways to lose him and the bag's replacement. Tests/Protocol/Q008Q31Tests.cs greets
	/// him through a client first.</summary>
	public static class Q31Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q31test_npcs_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q31_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestQ31Setup(Character self) {
			Player player = (Player) self;
			NPC gondor = (NPC) ((NPCDef) ThingDef.GetByDefname(Q31.GondorCookDef)).Create(player.X + 1, player.Y, player.Z, player.M);
			NPC mordor = (NPC) ((NPCDef) ThingDef.GetByDefname(Q31.MordorCookDef)).Create(player.X + 30, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(gondor, 900);
			NPCAiTimer.Freeze(mordor, 900);
			player.SetTag(npcsTK, new[] { gondor, mordor });
			player.Realm = Realm.Gondor;
			Q31.SetStage(player, 0);
			Q31.SetLost(player, false);
			player.Level = 200;
			player.Experience = 0;
			Console.WriteLine("Q31_SETUP_DONE: cook=" + gondor.Uid);
		}

		[SteamFunction]
		public static void TestQ31Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q31_BUTTON: id=" + SingletonScript<D_Q31Kuchar>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ31State(Character self) {
			Console.WriteLine("Q31_STATE: stage=" + Q31.GetStage((Player) self));
		}

		private static PetFigurine FindFigurine(Player player) {
			foreach (AbstractItem item in player.Backpack) {
				if (item is PetFigurine figurine) {
					return figurine;
				}
			}
			return null;
		}

		[SteamFunction]
		public static void TestQ31Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			NPC cook = npcs[0], mordorCook = npcs[1];
			D_Q31Kuchar dialog = SingletonScript<D_Q31Kuchar>.Instance;
			Collector bag = Collectors.FindIn(player, Q31.BagDef);
			Report("theClientTookTheBag", (Q31.GetStage(player) == 1) && (bag != null) && bag.Flag_NonMovable && Q31.Journal.IsStarted(player));

			//not before the bag is full; then the llama and 50 experience
			dialog.Press("kuchar_2:1", cook, player);
			bool notYet = (Q31.GetStage(player) == 1) && !bag.IsDeleted;
			bag.Collected = 50;
			int before = player.Experience;
			dialog.Press("kuchar_2:1", cook, player);
			PetFigurine figurine = FindFigurine(player);
			bool rewarded = (Q31.GetStage(player) == 2) && bag.IsDeleted && (player.Experience - before == Q31.RewardExp) && (figurine != null)
				&& (figurine.petDef == ThingDef.GetByDefname(Q31.LlamaDef)) && Q31.Journal.IsDone(player);
			Character llama = null;
			if (figurine != null) {
				figurine.On_DClick(player);
				foreach (AbstractCharacter ch in player.GetMap().GetCharsInRange(player.X, player.Y, 3)) {
					if ((ch.Def == ThingDef.GetByDefname(Q31.LlamaDef)) && (ch is Character c) && (c.ControlMaster == player)) {
						llama = c;
					}
				}
			}
			Report("fiftyBirdsForALlama", notYet && rewarded && (llama != null) && figurine.IsDeleted);
			if (llama != null) {
				llama.Delete();
			}

			//each cook serves his own side
			bool mordorRefuses = (Q31.Refusal(mordorCook, player) ?? "").Contains("Gondori") && (Q31.Refusal(cook, player) == null);
			player.Realm = Realm.Mordor;
			bool gondorRefuses = (Q31.Refusal(cook, player) ?? "").Contains("Mordorani") && (Q31.Refusal(mordorCook, player) == null);
			player.Realm = Realm.Gondor;
			Report("eachCookHisOwnSide", mordorRefuses && gondorRefuses);

			//a lie loses him for good; so does turning him down, or giving up (he keeps the bag)
			Q31.SetStage(player, 0);
			dialog.Press("kuchar_1:3", cook, player);
			bool lie = Q31.IsLost(player) && (Q31.GetStage(player) == 0) && (Q31.Refusal(cook, player) ?? "").Contains("nemam o cem bavit")
				&& Q31.Journal.GetText(player).Contains("nepromluvi");
			dialog.Press("kuchar_1:1", cook, player);
			bool staysLost = (Q31.GetStage(player) == 0) && (Collectors.FindIn(player, Q31.BagDef) == null);
			Q31.SetLost(player, false);
			dialog.Press("kuchar_1:2", cook, player);
			bool refused = Q31.IsLost(player);
			Q31.SetLost(player, false);
			dialog.Press("kuchar_1:1", cook, player);
			bool second = (Q31.GetStage(player) == 1) && (Collectors.FindIn(player, Q31.BagDef) != null);
			dialog.Press("kuchar_2:3", cook, player);
			bool gaveUp = Q31.IsLost(player) && (Collectors.FindIn(player, Q31.BagDef) == null);
			Report("lostForGood", lie && staysLost && refused && second && gaveUp);

			//a lost bag comes back
			Q31.SetLost(player, false);
			Q31.SetStage(player, 1);
			dialog.Press("kuchar_2:4", cook, player);
			Report("aNewBag", Collectors.FindIn(player, Q31.BagDef) != null);

			bool plain = true;
			foreach (string text in Q31.JournalTexts) {
				plain &= Q009Tests.Plain(text);
			}
			Report("journalTextsArePlain", plain);
			cook.Delete(); //the next test's greeting mustn't reach him
			mordorCook.Delete();
			Console.WriteLine("Q31_CHAIN_DONE: stage=" + Q31.GetStage(player));
		}
	}
}
