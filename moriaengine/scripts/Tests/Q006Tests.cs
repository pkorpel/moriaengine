using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1264 - q006 Erioluv vnuk on the server: the whole chain through the dialogs' own buttons, what each page offers, the
	/// rewards, Unas's level gate, the parchment tag, both endings and the robe. Tests/Protocol/Q006Tests.cs talks to Eriol through a
	/// client first.</summary>
	public static class Q006Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q006test_npcs_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q006_CHECK: " + name + "=" + ok);
		}

		private static NPC[] Npcs(Player player) {
			return (NPC[]) player.GetTag(npcsTK);
		}

		/// <summary>Eriol beside the player, Qworet, Unas and the innkeeper out of earshot; the quest from its start.</summary>
		[SteamFunction]
		public static void TestQ006Setup(Character self) {
			Player player = (Player) self;
			string[] defs = { Q006.EriolDef, Q006.QworetDef, Q006.UnasDef, Q006.InnkeeperDef };
			NPC[] npcs = new NPC[defs.Length];
			for (int i = 0; i < defs.Length; i++) {
				int dx = (i == 0) ? 1 : 30 + (2 * i);
				npcs[i] = (NPC) ((NPCDef) ThingDef.GetByDefname(defs[i])).Create(player.X + dx, player.Y, player.Z, player.M);
				npcs[i].Flag_Insubst = false;
				NPCAiTimer.Freeze(npcs[i], 900);
			}
			player.SetTag(npcsTK, npcs);
			Q006.SetStage(player, 0);
			Q006.SetParchment(player, false);
			player.Level = 10;
			player.Experience = 0;
			player.SetRealSkillValue((int) SkillName.Alchemy, 0);
			Console.WriteLine("Q006_SETUP_DONE: eriol=" + npcs[0].Uid);
		}

		/// <summary>The exit button's id in Eriol's dialog, for the client.</summary>
		[SteamFunction]
		public static void TestQ006Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q006_BUTTON: id=" + SingletonScript<D_Q006Eriol>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ006State(Character self) {
			Console.WriteLine("Q006_STATE: stage=" + Q006.GetStage((Player) self));
		}

		private static bool Offers(QuestDialogDef dialog, NPC npc, Player player, string target) {
			return dialog.BuildFor(npc, player).HasResponse(target);
		}

		private static int ExpGained(Player player, Action act) {
			int before = player.Experience;
			act();
			return player.Experience - before;
		}

		/// <summary>From stage 1 (the client accepted the torches) to the end, the dialogs' buttons only.</summary>
		[SteamFunction]
		public static void TestQ006Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = Npcs(player);
			NPC eriol = npcs[0], qworet = npcs[1], unas = npcs[2], innkeeper = npcs[3];
			D_Q006Eriol dEriol = SingletonScript<D_Q006Eriol>.Instance;
			D_Q006Qworet dQworet = SingletonScript<D_Q006Qworet>.Instance;
			D_Q006Unas dUnas = SingletonScript<D_Q006Unas>.Instance;
			D_Q007Hospodsky dInn = SingletonScript<D_Q007Hospodsky>.Instance;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //a player: GMs pay nothing
			try {
				Report("theClientAcceptedTheTorches", Q006.GetStage(player) == 1);
				Report("theJournalListsIt", (Q006.Journal != null) && QuestJournal.Entries.Contains(Q006.Journal) && Q006.Journal.IsStarted(player)
					&& !Q006.Journal.IsDone(player) && Q006.Journal.GetText(player).Contains("deset pochodni"));

				//torches
				Q006.Give(player, "i_torch", 9);
				bool notWithNine = !Offers(dEriol, eriol, player, "gq006_haveSubQuestResources_1");
				dEriol.Press("gq006_giveSubQuestResources_1", eriol, player);
				bool nineRefused = Q006.GetStage(player) == 1;
				Q006.Give(player, "i_torch");
				bool offersWithTen = Offers(dEriol, eriol, player, "gq006_haveSubQuestResources_1");
				int exp = ExpGained(player, () => dEriol.Press("gq006_giveSubQuestResources_1", eriol, player));
				Report("tenTorchesAndNoFewer", notWithNine && nineRefused && offersWithTen && (Q006.GetStage(player) == 2) && (exp == 50) && (Q006.Count(player, "i_torch") == 0));

				//the dog: a shrunk c_dog, not any figurine
				dEriol.Press("gq006_acceptSubquest_2", eriol, player);
				PetFigurine horse = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(player.Backpack);
				horse.petDef = (NPCDef) ThingDef.GetByDefname("c_wolf");
				bool horseRefused = (Q006.FindDog(player) == null) && !Offers(dEriol, eriol, player, "gq006_haveSubQuestResources_2");
				PetFigurine dog = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(player.Backpack);
				dog.petDef = (NPCDef) ThingDef.GetByDefname("c_dog");
				exp = ExpGained(player, () => dEriol.Press("gq006_giveSubQuestResources_2", eriol, player));
				Report("aShrunkDogOnly", horseRefused && dog.IsDeleted && !horse.IsDeleted && (Q006.GetStage(player) == 4) && (exp == 100));
				horse.Delete();

				//cures
				dEriol.Press("gq006_acceptSubquest_3", eriol, player);
				Q006.Give(player, "i_potion_cureless", 3);
				exp = ExpGained(player, () => dEriol.Press("gq006_giveSubQuestResources_3", eriol, player));
				bool less = (Q006.GetStage(player) == 6) && (exp == 50) && (Q006.Count(player, "i_potion_cureless") == 0);
				dEriol.Press("gq006_acceptSubquest_4", eriol, player);
				Q006.Give(player, "i_potion_curegreat", 10);
				exp = ExpGained(player, () => dEriol.Press("gq006_giveSubQuestResources_4", eriol, player));
				Report("threeLesserThenTenGreaterCures", less && (Q006.GetStage(player) == 8) && (exp == 125) && (Q006.Count(player, "i_potion_curegreat") == 0));

				//thighs in Eriol's bag + 3 greater refresh
				dEriol.Press("gq006_acceptSubquest_5", eriol, player);
				Collector bag = Collectors.FindIn(player, Q006.ThighBag);
				Q006.Give(player, "i_potion_refreshgreater", 3);
				bool notWithAnEmptyBag = (bag != null) && !Offers(dEriol, eriol, player, "gq006_haveSubQuestResources_5");
				Report("theQuestBagsCantBeLost", bag.HasAttribute(ItemAttributes.attr_newbie) && bag.HasAttribute(ItemAttributes.attr_move_never));
				bag.SetTag(Collector.CountTK, 10);
				exp = ExpGained(player, () => dEriol.Press("gq006_giveSubQuestResources_5", eriol, player));
				Report("aFullBagOfThighs", notWithAnEmptyBag && bag.IsDeleted && (Q006.GetStage(player) == 10) && (exp == 200) && (Q006.Count(player, "i_potion_refreshgreater") == 0));

				//Qworet
				bool qworetNotYet = !Offers(dQworet, qworet, player, "q006_eriol");
				dEriol.Press("gq006_acceptSubquest_6", eriol, player);
				bool qworetOffers = Offers(dQworet, qworet, player, "q006_leave");
				exp = ExpGained(player, () => dQworet.Press("q006_leave", qworet, player));
				Report("qworetKnowsNothing", qworetNotYet && qworetOffers && (Q006.GetStage(player) == 12) && (exp == 50));
				exp = ExpGained(player, () => dEriol.Press("q006_goToUnas", eriol, player));
				Report("eriolSendsToUnas", (Q006.GetStage(player) == 13) && (exp == 100));

				//Unas: level 15, four bags, the parchment as a tag
				dUnas.Press("q006_esentials", unas, player);
				bool levelGate = (Q006.GetStage(player) == 13) && Offers(dUnas, unas, player, "q006_idleExit") && !Offers(dUnas, unas, player, "q006_esentials");
				player.Level = 200;
				dUnas.Press("q006_esentials", unas, player);
				Report("unasWantsLevelFifteen", levelGate && (Q006.GetStage(player) == 14) && (Collectors.FindIn(player, Q006.EssenceBags[0]) != null));
				bool bagsOk = true;
				int unasExp = 0;
				for (int stage = 14; stage <= 17; stage++) {
					Collector essences = Collectors.FindIn(player, Q006.EssenceBags[stage - 14]);
					bagsOk &= (essences != null) && essences.HasAttribute(ItemAttributes.attr_newbie) && essences.HasAttribute(ItemAttributes.attr_move_never);
					dUnas.Press((stage == 17) ? "q006_takePergamen" : "q006_esentials", unas, player);
					bagsOk &= (essences != null) && !essences.IsDeleted && (Q006.GetStage(player) == stage); //not full yet
					essences.SetTag(Collector.CountTK, 20);
					bagsOk &= Offers(dUnas, unas, player, (stage == 17) ? "q006_takePergamen" : "q006_esentials");
					unasExp += ExpGained(player, () => dUnas.Press((stage == 17) ? "q006_takePergamen" : "q006_esentials", unas, player));
					bagsOk &= essences.IsDeleted && (Q006.GetStage(player) == stage + 1);
					if (stage < 17) {
						bagsOk &= Collectors.FindIn(player, Q006.EssenceBags[stage - 13]) != null;
					}
				}
				Report("unasFourBagsThenTheParchment", bagsOk && (unasExp == 2000) && Q006.HasParchment(player) && (Q006.GetStage(player) == 18));

				//Eriol reads it, the innkeeper takes it
				bool innkeeperNotYet = !Offers(dInn, innkeeper, player, "q006_innkeeper");
				exp = ExpGained(player, () => dEriol.Press("q006_goToInnkeeper", eriol, player));
				bool eriol19 = (Q006.GetStage(player) == 19) && (exp == 300) && Q006.HasParchment(player);
				exp = ExpGained(player, () => dInn.Press("q006_toldInnkeeper", innkeeper, player));
				Report("theInnkeeperTakesTheParchment", innkeeperNotYet && eriol19 && (Q006.GetStage(player) == 20) && (exp == 300) && !Q006.HasParchment(player));
				exp = ExpGained(player, () => dEriol.Press("gq006_acceptSubquest_7", eriol, player));
				Report("eriolAsksForTheIngredients", (Q006.GetStage(player) == 21) && (exp == 100));

				//without Alchemy 100: the ingredients handed in, stage 22, no robe
				Q006.Give(player, "i_potion_curegreat", 3);
				Q006.Give(player, "i_srdiczko", 2);
				Q006.Give(player, "i_bylina");
				bool noMixing = !Offers(dEriol, eriol, player, "gq006_createPurityPotion") && Offers(dEriol, eriol, player, "gq006_giveSubQuestResources_7");
				dEriol.Press("gq006_createPurityPotion", eriol, player);
				bool stillAt21 = Q006.GetStage(player) == 21;
				exp = ExpGained(player, () => dEriol.Press("gq006_giveSubQuestResources_7", eriol, player));
				Report("withoutAlchemyItEndsWithoutTheRobe", noMixing && stillAt21 && (Q006.GetStage(player) == Q006.DoneWithoutRobe) && (exp == 2000)
					&& !Q006.HasPotionIngredients(player) && (Q006.Count(player, Q006.RobeDef) == 0) && !Q006.Journal.IsDone(player));

				//Alchemy 100 later: Eriol mixes it with the ingredients he kept, the potion (a tag) for the robe
				bool notBelowHundred = !Offers(dEriol, eriol, player, "gq006_takePurityPotionResources");
				dEriol.Press("gq006_takePurityPotionResources", eriol, player);
				bool refused = Q006.GetStage(player) == Q006.DoneWithoutRobe;
				player.SetRealSkillValue((int) SkillName.Alchemy, 1000);
				exp = ExpGained(player, () => dEriol.Press("gq006_takePurityPotionResources", eriol, player));
				Report("atAlchemyHundredEriolsIngredientsMakeThePotion", notBelowHundred && refused && (Q006.GetStage(player) == 23) && Q006.HasPotion(player) && (exp == 0)
					&& !Q006.HasPotionIngredients(player));
				Q006.SetPotion(player, false);
				bool notWithoutThePotion = !Offers(dEriol, eriol, player, "gq006_havePurityPotion");
				dEriol.Press("gq006_givePurityPotion", eriol, player);
				bool stillAt23 = Q006.GetStage(player) == 23;
				Q006.SetPotion(player, true);
				dEriol.Press("gq006_givePurityPotion", eriol, player);
				Item robe = null;
				foreach (AbstractItem item in player.Backpack) {
					if (item.Def.PrettyDefname == Q006.RobeDef) {
						robe = (Item) item;
					}
				}
				Report("thePotionForTheRobe", notWithoutThePotion && stillAt23 && (Q006.GetStage(player) == Q006.Done) && (robe != null) && (robe.GetTag(Q006.RobeOwnerTK) == player)
					&& !Q006.HasPotion(player) && Q006.Journal.IsDone(player));

				//the robe: +10 Alchemy for its owner at 100, back when taken off
				player.TryEquip(robe);
				bool on = player.GetSkill((int) SkillName.Alchemy) == 1100;
				player.TryEquip((Item) ThingDef.GetByDefname("i_robe").Create(player.Backpack)); //same layer: takes the robe off
				bool off = (player.GetSkill((int) SkillName.Alchemy) == 1000) && (robe.GetTag(Q006.RobeBoostTK) == null);
				robe.RemoveTag(Q006.RobeOwnerTK);
				player.TryEquip(robe);
				bool ownerless = player.GetSkill((int) SkillName.Alchemy) == 1000;
				Report("theRobeGivesItsOwnerTen", on && off && ownerless);

				//a new robe for 2,000,000
				int robesBefore = Q006.Count(player, Q006.RobeDef);
				long goldBefore = player.Gold; //a new character's few coins: not enough
				dEriol.Press("gq006_acceptRobe", eriol, player);
				bool notWithoutGold = (Q006.Count(player, Q006.RobeDef) == robesBefore) && (player.Gold == goldBefore);
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Bank)).Amount = Q006.RobePrice;
				dEriol.Press("gq006_acceptRobe", eriol, player);
				Report("aNewRobeForTwoMillion", Offers(dEriol, eriol, player, "gq006_acceptRobe") && notWithoutGold && (Q006.Count(player, Q006.RobeDef) == robesBefore + 1)
					&& (player.Gold == goldBefore));

				Report("journalTextsArePlain", JournalPlain());

				//an alchemist at stage 21 mixes it at Eriol's at once: the ingredients go, the potion is a tag
				Q006.SetStage(player, 21);
				Q006.Give(player, "i_potion_curegreat", 3);
				Q006.Give(player, "i_srdiczko", 2);
				Q006.Give(player, "i_bylina");
				bool offersMixing = Offers(dEriol, eriol, player, "gq006_createPurityPotion") && !Offers(dEriol, eriol, player, "gq006_giveSubQuestResources_7");
				exp = ExpGained(player, () => dEriol.Press("gq006_createPurityPotion", eriol, player));
				Report("anAlchemistMixesItAtEriols", offersMixing && (Q006.GetStage(player) == 23) && (exp == 2000)
					&& !Q006.HasPotionIngredients(player) && Q006.HasPotion(player) && (Q006.Count(player, "i_potion_ocisteni") == 0));
				Q006.SetPotion(player, false);
				Q006.SetStage(player, Q006.Done);

				ItemDef potion = (ItemDef) ThingDef.GetByDefname("i_potion_ocisteni");
				ItemDef refresh = (ItemDef) ThingDef.GetByDefname("i_potion_RefreshGreater");
				CraftmenuCategory alchemyMenu = CraftmenuContents.MainCategories[SkillName.Alchemy];
				Report("thePotionIsNotBrewable", (potion != null) && !InMenu(alchemyMenu, potion) && InMenu(alchemyMenu, refresh)); //the control: a potion that is in a section
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("Q006_CHAIN_DONE: stage=" + Q006.GetStage(player));
		}

		//anywhere in the menu, sections too
		private static bool InMenu(CraftmenuCategory category, ItemDef def) {
			foreach (ICraftmenuElement element in category.Contents) {
				if (((element is CraftmenuItem made) && (made.itemDef == def)) || ((element is CraftmenuCategory inner) && InMenu(inner, def))) {
					return true;
				}
			}
			return false;
		}

		private static bool JournalPlain() {
			foreach (string text in Q006.JournalTexts) {
				foreach (char c in text) {
					if (c > 127) {
						return false;
					}
				}
			}
			return Q006.JournalTexts.Length == Q006.Done;
		}
	}
}
