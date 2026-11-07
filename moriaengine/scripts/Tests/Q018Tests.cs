using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1270 - q018 (the golden salmon) on the server, through the dialogs' own buttons: Mirrana, Elnassir's salve and its
	/// price, the golden rune, the wheel's thread and Tailoring, the fisherman's reward. Tests/Protocol/Q009Q018Tests.cs greets the
	/// fisherman through a client first.</summary>
	public static class Q018Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q018test_npcs_");
		private static readonly string[] reagents = { "i_reag_spider_silk", "i_reag_nightshade", "i_reag_bone" };
		private static readonly int[] amounts = { 20, 80, 150 };

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q018_CHECK: " + name + "=" + ok);
		}

		/// <summary>The fisherman beside the player, Mirrana and Elnassir out of earshot; every q018 tag cleared.</summary>
		[SteamFunction]
		public static void TestQ018Setup(Character self) {
			Player player = (Player) self;
			string[] defs = { Q018.FishermanDef, Q018.MirranaDef, Q018.ElnassirDef };
			NPC[] npcs = new NPC[defs.Length];
			for (int i = 0; i < defs.Length; i++) {
				int dx = (i == 0) ? 1 : 30 + (2 * i);
				npcs[i] = (NPC) ((NPCDef) ThingDef.GetByDefname(defs[i])).Create(player.X + dx, player.Y, player.Z, player.M);
				NPCAiTimer.Freeze(npcs[i], 900);
			}
			player.SetTag(npcsTK, npcs);
			Q018.SetStage(player, 0);
			QuestItems.Take(player, Q018.Salve);
			QuestItems.Take(player, Q018.Thread);
			player.Level = 200;
			player.Experience = 0;
			player.SetRealSkillValue((int) SkillName.Tailoring, 500);
			Console.WriteLine("Q018_SETUP_DONE: fisherman=" + npcs[0].Uid);
		}

		[SteamFunction]
		public static void TestQ018Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q018_BUTTON: id=" + SingletonScript<D_Q018Fisherman>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ018State(Character self) {
			Console.WriteLine("Q018_STATE: stage=" + Q018.GetStage((Player) self));
		}

		private static int ExpGained(Player player, Action act) {
			int before = player.Experience;
			act();
			return player.Experience - before;
		}

		private static bool Near(Player player, Point4D spot) {
			return (player.M == spot.M) && (Math.Abs(player.X - spot.X) <= 4) && (Math.Abs(player.Y - spot.Y) <= 4);
		}

		[SteamFunction]
		public static void TestQ018Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			NPC fisherman = npcs[0], mirrana = npcs[1], elnassir = npcs[2];
			D_Q018Fisherman dFisherman = SingletonScript<D_Q018Fisherman>.Instance;
			D_Q018Mirrana dMirrana = SingletonScript<D_Q018Mirrana>.Instance;
			D_Q018Elnassir dElnassir = SingletonScript<D_Q018Elnassir>.Instance;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //a player: GMs pay nothing
			try {
				Report("theClientTookTheTask", (Q018.GetStage(player) == 1) && (Q006.Count(player, "i_fish_small") >= 1));
				Report("theJournalListsIt", QuestJournal.Entries.Contains(Q018.Journal) && Q018.Journal.IsStarted(player));

				//Mirrana sends the hero to Elnassir, he names his price
				bool mirranaHears = dMirrana.BuildFor(mirrana, player).HasResponse("mirrana:1");
				dMirrana.Press("mirrana:1", mirrana, player);
				dElnassir.Press("elnassir:1", elnassir, player);
				Report("sentToElnassir", mirranaHears && (Q018.GetStage(player) == 3));

				//the salve: everything in the pack and 10,000 gold, nothing short of it
				for (int i = 0; i < reagents.Length; i++) {
					Q006.Give(player, reagents[i], amounts[i]);
				}
				Q006.Consume(player, "i_reag_bone", 1);
				bool notShort = !dElnassir.BuildFor(elnassir, player).HasResponse("elnassir:2");
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Bank)).Amount = Q018.SalveGold;
				dElnassir.Press("elnassir:2", elnassir, player);
				bool refused = !QuestItems.Has(player, Q018.Salve) && (Q006.Count(player, "i_reag_nightshade") == 80);
				Q006.Give(player, "i_reag_bone", 1);
				long gold = player.Gold;
				bool offered = dElnassir.BuildFor(elnassir, player).HasResponse("elnassir:2");
				dElnassir.Press("elnassir:2", elnassir, player);
				bool allTaken = (Q006.Count(player, reagents[0]) == 0) && (Q006.Count(player, reagents[1]) == 0) && (Q006.Count(player, reagents[2]) == 0);
				Report("elnassirsSalve", notShort && refused && offered && QuestItems.Has(player, Q018.Salve) && allTaken && (player.Gold == gold - Q018.SalveGold));

				//the rune does nothing before Mirrana has her salve
				Item rune = (Item) ThingDef.GetByDefname(Q018.RuneDef).Create(player.X, player.Y + 1, player.Z, player.M);
				Point4D before = new Point4D(player.X, player.Y, player.Z, player.M);
				Q018.UseRune(player);
				bool runeWaits = (player.X == before.X) && (player.Y == before.Y);
				bool mirranaTakesIt = dMirrana.BuildFor(mirrana, player).HasResponse("mirrana:2");
				int exp = ExpGained(player, () => dMirrana.Press("mirrana:2", mirrana, player));
				Report("theSalveForTheRune", runeWaits && mirranaTakesIt && (exp == 500) && (Q018.GetStage(player) == 4) && !QuestItems.Has(player, Q018.Salve));

				//the rune takes the hero to the wheel, the wheel spins the thread and sends them back
				Q018.UseRune(player);
				bool atTheWheel = Near(player, Q018.WheelSpot);
				Item wheel = (Item) ThingDef.GetByDefname(Q018.WheelDef).Create(player.X, player.Y, player.Z, player.M);
				exp = ExpGained(player, () => Q018.UseWheel(wheel, player));
				bool spun = QuestItems.Has(player, Q018.Thread) && (exp == 1500) && (player.GetRealSkillValue(SkillName.Tailoring) == 530) && (Q018.GetStage(player) == 5);
				bool back = Near(player, Q018.RuneSpot);
				Q018.UseWheel(wheel, player);
				bool once = player.GetRealSkillValue(SkillName.Tailoring) == 530;
				wheel.Delete();
				rune.Delete();
				Report("theRuneAndTheWheel", atTheWheel && spun && back && once);

				//the thread to the fisherman
				player.Go(fisherman.X - 1, fisherman.Y, fisherman.Z, fisherman.M);
				gold = player.Gold;
				bool offersThread = dFisherman.BuildFor(fisherman, player).HasResponse("rybar:2");
				exp = ExpGained(player, () => dFisherman.Press("rybar:2", fisherman, player));
				Report("theFishermansReward", offersThread && (exp == 1000) && (player.Gold == gold + Q018.RewardGold) && (Q018.GetStage(player) == Q018.Done)
					&& !QuestItems.Has(player, Q018.Thread) && Q018.Journal.IsDone(player));

				Report("eachNpcOpensItsOwnDialog", (Q018.DialogOf(fisherman) is D_Q018Fisherman) && (Q018.DialogOf(mirrana) is D_Q018Mirrana) && (Q018.DialogOf(elnassir) is D_Q018Elnassir));
				bool plain = true;
				foreach (string text in Q018.JournalTexts) {
					plain &= Q009Tests.Plain(text);
				}
				Report("journalTextsArePlain", plain);
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("Q018_CHAIN_DONE: stage=" + Q018.GetStage(player));
		}
	}
}
