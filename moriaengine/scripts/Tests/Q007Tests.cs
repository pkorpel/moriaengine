using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1266 - q007 (Haradir's piglet, the Inscription book, the adamantine hammer) on the server, through the dialogs' own
	/// buttons: the stages, rewards, the tags, the innkeeper's drinks, the pirates' chest, the 8th-circle gate, the pen, the hammer and
	/// Rodrigo. Tests/Protocol/Q007Tests.cs greets Haradir through a client first.</summary>
	public static class Q007Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q007test_npcs_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q007_CHECK: " + name + "=" + ok);
		}

		private static readonly string[] defs = { Q007.HaradirDef, Q007.InnkeeperDef, Q007.RachelDef, Q007.SmithDef, Q007.InscripterDef, Q007.DivousDef, Q007.RodrigoDef };

		/// <summary>Haradir beside the player, the others out of earshot; every q007 tag cleared.</summary>
		[SteamFunction]
		public static void TestQ007Setup(Character self) {
			Player player = (Player) self;
			NPC[] npcs = new NPC[defs.Length];
			for (int i = 0; i < defs.Length; i++) {
				int dx = (i == 0) ? 1 : 30 + (2 * i);
				npcs[i] = (NPC) ((NPCDef) ThingDef.GetByDefname(defs[i])).Create(player.X + dx, player.Y, player.Z, player.M);
				NPCAiTimer.Freeze(npcs[i], 900);
			}
			player.SetTag(npcsTK, npcs);
			Q007.SetStage(player, 0);
			Q007.SetInscripter(player, 0);
			Q007.SetSmith(player, 0);
			Q007.ForgetDrink(player);
			foreach (string item in new[] { Q007.Bow, Q007.Piglet, Q007.Book, Q007.Pen, Q007.Stones[0], Q007.Stones[1], Q007.Stones[2] }) {
				QuestItems.Take(player, item);
			}
			player.Level = 200;
			player.Experience = 0;
			player.SetRealSkillValue((int) SkillName.Blacksmith, 0);
			player.SetRealSkillValue((int) SkillName.Inscribe, 0);
			Console.WriteLine("Q007_SETUP_DONE: haradir=" + npcs[0].Uid);
		}

		[SteamFunction]
		public static void TestQ007Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q007_BUTTON: id=" + SingletonScript<D_Q007Haradir>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ007State(Character self) {
			Console.WriteLine("Q007_STATE: stage=" + Q007.GetStage((Player) self));
		}

		private static bool Offers(QuestDialogDef dialog, NPC npc, Player player, string target) {
			return dialog.BuildFor(npc, player).HasResponse(target);
		}

		private static int ExpGained(Player player, Action act) {
			int before = player.Experience;
			act();
			return player.Experience - before;
		}

		private static int CountIn(Player player, string defname) {
			return Q006.Count(player, defname);
		}

		[SteamFunction]
		public static void TestQ007Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			NPC haradir = npcs[0], innkeeper = npcs[1], rachel = npcs[2], smith = npcs[3], inscripter = npcs[4], divous = npcs[5], rodrigo = npcs[6];
			D_Q007Haradir dHaradir = SingletonScript<D_Q007Haradir>.Instance;
			D_Q007Hospodsky dInn = SingletonScript<D_Q007Hospodsky>.Instance;
			D_Q007Rachel dRachel = SingletonScript<D_Q007Rachel>.Instance;
			D_Q007Smith dSmith = SingletonScript<D_Q007Smith>.Instance;
			D_Q007Inscripter dInscripter = SingletonScript<D_Q007Inscripter>.Instance;
			D_Q007Divous dDivous = SingletonScript<D_Q007Divous>.Instance;
			D_Q007Rodrigo dRodrigo = SingletonScript<D_Q007Rodrigo>.Instance;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //a player: GMs pay nothing
			try {
				Report("theClientPromisedThePiglet", Q007.GetStage(player) == 1);
				Report("theJournalListsAllThree", QuestJournal.Entries.Contains(Q007.PigletEntry) && QuestJournal.Entries.Contains(Q007.BookEntry)
					&& QuestJournal.Entries.Contains(Q007.HammerEntry) && Q007.PigletEntry.IsStarted(player) && !Q007.BookEntry.IsStarted(player));

				//the innkeeper denies it, Haradir names Rachel, Rachel wants 1000 bat wings
				bool innOffersGivingUp = Offers(dInn, innkeeper, player, "hospodsky:5");
				dInn.Press("hospodsky:5", innkeeper, player);
				bool denied = Q007.GetStage(player) == 2;
				dHaradir.Press("haradir3:1", haradir, player);
				dRachel.Press("carodejnice2:1", rachel, player);
				Report("deniedThenSentToRachel", innOffersGivingUp && denied && (Q007.GetStage(player) == 4));
				Q006.Give(player, D_Q007Rachel.BatWing, 999);
				bool notWith999 = !Offers(dRachel, rachel, player, "carodejnice4:1");
				dRachel.Press("carodejnice4:1", rachel, player);
				bool refused999 = Q007.GetStage(player) == 4;
				Q006.Give(player, D_Q007Rachel.BatWing);
				dRachel.Press("carodejnice4:1", rachel, player);
				Report("rachelWantsAThousandBatWings", notWith999 && refused999 && (Q007.GetStage(player) == 5) && (CountIn(player, D_Q007Rachel.BatWing) == 0));

				//the Dryad and her bow (a tag from her corpse), the piglet for it
				dInn.Press("hospodsky:6", innkeeper, player);
				bool sentForTheDryad = Q007.GetStage(player) == 6;
				NPC dryad = (NPC) ((NPCDef) ThingDef.GetByDefname(Q007.DryadDef)).Create(player.X + 40, player.Y + 5, player.Z, player.M);
				bool sheCarriesTheBow = QuestItems.Carried(dryad).Contains(Q007.Bow);
				dryad.Delete();
				dInn.Press("hospodsky:7", innkeeper, player);
				bool noBowNoPiglet = (Q007.GetStage(player) == 6) && !QuestItems.Has(player, Q007.Piglet);
				QuestItems.Give(player, Q007.Bow);
				bool offersTheExchange = Offers(dInn, innkeeper, player, "hospodsky:7");
				int exp = ExpGained(player, () => dInn.Press("hospodsky:7", innkeeper, player));
				Report("theBowForThePiglet", sentForTheDryad && sheCarriesTheBow && noBowNoPiglet && offersTheExchange && (Q007.GetStage(player) == 7) && (exp == 500)
					&& QuestItems.Has(player, Q007.Piglet) && !QuestItems.Has(player, Q007.Bow));
				bool offersThePiglet = Offers(dHaradir, haradir, player, "haradir7:1");
				exp = ExpGained(player, () => dHaradir.Press("haradir7:1", haradir, player));
				Report("thePigletToHaradir", offersThePiglet && (Q007.GetStage(player) == 8) && (exp == 500) && !QuestItems.Has(player, Q007.Piglet) && Q007.PigletEntry.IsDone(player));

				//drinks: paid, one every 5 minutes
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Bank)).Amount = 1000;
				long gold = player.Gold;
				dInn.Press("drink:0", innkeeper, player);
				bool wine = (CountIn(player, "i_bottle_wine") == 1) && (player.Gold == gold - 150);
				dInn.Press("drink:3", innkeeper, player);
				bool tooSoon = (CountIn(player, "i_jug_cider") == 0) && (player.Gold == gold - 150);
				Q007.ForgetDrink(player);
				dInn.Press("drink:3", innkeeper, player);
				Report("theInnkeepersDrinks", wine && tooSoon && (CountIn(player, "i_jug_cider") == 1) && (player.Gold == gold - 400));

				//the smith and the inscripter want their skills, then the stones
				bool smithWantsHundred = !Offers(dSmith, smith, player, "kovar3:1");
				player.SetRealSkillValue((int) SkillName.Blacksmith, 1000);
				dSmith.Press("kovar3:1", smith, player);
				player.SetRealSkillValue((int) SkillName.Inscribe, 800);
				dInscripter.Press("maginscript3:1", inscripter, player);
				Report("bothSendForTheStones", smithWantsHundred && (Q007.GetSmith(player) == 1) && (Q007.GetInscripter(player) == 1) && Q007.NeedsStones(player));

				//the pirates' chest
				Item chest = (Item) ThingDef.GetByDefname(Q007.ChestDef).Create(player.X, player.Y + 1, player.Z, player.M);
				NPC pirate = (NPC) ((NPCDef) ThingDef.GetByDefname("c_pirat")).Create(chest.X + 3, chest.Y, chest.Z, chest.M);
				NPCAiTimer.Freeze(pirate, 900);
				pirate.AggroMode = AggroMode.Hostile;
				Q007.OpenChest(chest, player);
				bool guarded = !Q007.HasStones(player);
				pirate.Delete();
				Q007.OpenChest(chest, player);
				Report("theChestGivesTheStonesOncePiratesAreDead", guarded && Q007.HasStones(player));

				//the hammer
				exp = ExpGained(player, () => dSmith.Press("kovar5:1", smith, player));
				Item hammer = null;
				foreach (AbstractItem item in player.Backpack) {
					if (item.Def == ThingDef.GetByDefname(Q007.HammerDef)) {
						hammer = (Item) item;
					}
				}
				Report("theStonesForTheHammer", (Q007.GetSmith(player) == 2) && (exp == 2000) && (hammer != null) && !Q007.HasStones(player) && Q007.HammerEntry.IsDone(player));
				player.TryEquip(hammer);
				bool on = player.GetSkill((int) SkillName.Blacksmith) == 1100;
				player.TryEquip((Item) ThingDef.GetByDefname("i_hammer_smith").Create(player.Backpack)); //the same hand: takes the hammer off
				Report("theHammerGivesTen", on && (player.GetSkill((int) SkillName.Blacksmith) == 1000) && (hammer.GetTag(Q007.HammerBoostTK) == null));

				//the book, the 8th circle, then Divous's pen
				Q007.OpenChest(chest, player); //the inscripter still waits for his set
				chest.Delete();
				SpellDef eighth = SpellDef.GetById(60);
				bool noBookNoEighth = !Q007.MayInscribe(eighth, player) && Q007.MayInscribe(SpellDef.GetById(10), player);
				exp = ExpGained(player, () => dInscripter.Press("maginscript5:1", inscripter, player));
				Report("theStonesForTheBook", noBookNoEighth && (Q007.GetInscripter(player) == 2) && (exp == 2000) && QuestItems.Has(player, Q007.Book) && Q007.MayInscribe(eighth, player)
					&& Q007.BookEntry.IsDone(player));
				bool divousWantsIt = Offers(dDivous, divous, player, "divous2:1");
				dDivous.Press("divous2:1", divous, player);
				Report("theBookForThePen", divousWantsIt && (Q007.GetInscripter(player) == 3) && QuestItems.Has(player, Q007.Pen) && !QuestItems.Has(player, Q007.Book)
					&& !Q007.MayInscribe(eighth, player) && Q007.BookEntry.GetText(player).Contains("Divous"));

				//the pen writes a recall rune at Inscription 85
				Item rune = (Item) ThingDef.GetByDefname("i_rune_tinker").Create(player.Backpack);
				Q007.TryInscribeRune(rune, player);
				bool notAt80 = !rune.IsDeleted;
				player.SetRealSkillValue((int) SkillName.Inscribe, 900);
				Q007.TryInscribeRune(rune, player);
				Report("thePenMakesARecallRune", notAt80 && rune.IsDeleted && (CountIn(player, "i_rune_marker") == 1));

				//Rodrigo's rumours and the dolphin
				long before = player.Gold;
				dRodrigo.Press("rodrigo:2", rodrigo, player);
				bool tooPoor = !Q007.PaidRodrigo(player) && (player.Gold == before);
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Bank)).Amount = 8000 + D_Q007RodrigoNews.DolphinPrice;
				dRodrigo.Press("rodrigo:2", rodrigo, player);
				bool paid = Q007.PaidRodrigo(player) && (player.Gold == before + D_Q007RodrigoNews.DolphinPrice);
				exp = ExpGained(player, () => SingletonScript<D_Q007RodrigoNews>.Instance.Press("rodrigo_informace:1", rodrigo, player));
				Report("rodrigoSellsRumoursAndDolphins", tooPoor && paid && Q007.AdoptedDolphin(player) && (exp == 200) && (player.Gold == before));

				Report("eachNpcOpensItsOwnDialog", (Q007.DialogOf(haradir) is D_Q007Haradir) && (Q007.DialogOf(innkeeper) is D_Q007Hospodsky) && (Q007.DialogOf(rachel) is D_Q007Rachel)
					&& (Q007.DialogOf(smith) is D_Q007Smith) && (Q007.DialogOf(inscripter) is D_Q007Inscripter) && (Q007.DialogOf(divous) is D_Q007Divous)
					&& (Q007.DialogOf(rodrigo) is D_Q007Rodrigo));
				Report("journalTextsArePlain", Plain(Q007.PigletTexts) && Plain(Q007.BookTexts) && Plain(Q007.HammerTexts));
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("Q007_CHAIN_DONE: stage=" + Q007.GetStage(player));
		}

		private static bool Plain(string[] texts) {
			foreach (string text in texts) {
				foreach (char c in text) {
					if (c > 127) {
						return false;
					}
				}
			}
			return true;
		}
	}
}
