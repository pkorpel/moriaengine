using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1268 - q017 Alchymista Terlak on the server: the innkeeper's potions, the entry gate's rules, the run, the exit's
	/// profession reward and its equip rules, the time-out and death, the crystals' drops. Tests/Protocol/Q017Tests.cs has a client
	/// take the free potion first.</summary>
	public static class Q017Tests {
		private static readonly TagKey innTK = TagKey.Acquire("_q017test_inn_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q017_CHECK: " + name + "=" + ok);
		}

		/// <summary>A warrior of level 40 past q007's piglet, the innkeeper beside them.</summary>
		[SteamFunction]
		public static void TestQ017Setup(Character self) {
			Player player = (Player) self;
			NPC innkeeper = (NPC) ((NPCDef) ThingDef.GetByDefname(Q007.InnkeeperDef)).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(innkeeper, 900);
			player.SetTag(innTK, innkeeper);
			Q007.SetStage(player, 8);
			Q017.SetProtection(player, 0);
			Q017.SetRuns(player, 0);
			Q017.AddCrystals(player, -Q017.RedCrystals(player), -Q017.BlueCrystals(player));
			player.Profession = ProfessionDef.GetByDefname("profession_warrior");
			player.Level = 40;
			player.Experience = 0;
			Console.WriteLine("Q017_SETUP_DONE: inn=" + innkeeper.Uid);
		}

		[SteamFunction]
		public static void TestQ017Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q017_BUTTON: id=" + SingletonScript<D_Q007Hospodsky>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ017State(Character self) {
			Console.WriteLine("Q017_STATE: protection=" + Q017.GetProtection((Player) self));
		}

		private static bool Offers(NPC npc, Player player, string target) {
			return SingletonScript<D_Q007Hospodsky>.Instance.BuildFor(npc, player).HasResponse(target);
		}

		private static bool At(Player player, Point4D p) {
			return (player.X == p.X) && (player.Y == p.Y) && (player.M == p.M);
		}

		[SteamFunction]
		public static void TestQ017Chain(Character self) {
			Player player = (Player) self;
			NPC innkeeper = (NPC) player.GetTag(innTK);
			D_Q007Hospodsky dInn = SingletonScript<D_Q007Hospodsky>.Instance;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //a player: GMs pay nothing
			try {
				Report("theClientDrankTheFreePotion", (Q017.GetProtection(player) == 2) && (player.Experience == 200) && Q017.Journal.GetText(player).Contains("uvaril ochranny lektvar"));

				//later potions: the bag of 200 deer eyes and 10,000 gold, never free again
				Q017.SetProtection(player, 0);
				Q017.SetRuns(player, 2);
				dInn.Press("hospodsky:8", innkeeper, player);
				bool notFreeAgain = Q017.GetProtection(player) == 0;
				bool offersTheBag = Offers(innkeeper, player, "hospodsky:9");
				dInn.Press("hospodsky:9", innkeeper, player);
				Collector bag = Collectors.FindIn(player, Q017.DeerEyesBag);
				Report("laterPotionsWantDeerEyes", notFreeAgain && offersTheBag && (Q017.GetProtection(player) == 1) && (bag != null)
					&& bag.HasAttribute(ItemAttributes.attr_newbie) && bag.HasAttribute(ItemAttributes.attr_move_never));
				bag.Delete(); //lost: another one for the asking, as Erebor's button 9
				bool offersAnotherBag = Offers(innkeeper, player, "hospodsky:9");
				dInn.Press("hospodsky:9", innkeeper, player);
				bag = Collectors.FindIn(player, Q017.DeerEyesBag);
				Report("aLostBagCanBeAskedForAgain", offersAnotherBag && (bag != null) && (Q017.GetProtection(player) == 1));
				dInn.Press("hospodsky:9", innkeeper, player);
				int bags = 0;
				foreach (AbstractItem item in player.Backpack) {
					if (item.Def == ThingDef.GetByDefname(Q017.DeerEyesBag)) {
						bags++;
					}
				}
				dInn.Press("hospodsky:8", innkeeper, player);
				bool notWithAnEmptyBag = (Q017.GetProtection(player) == 1) && (bags == 1);
				bag.SetTag(Collector.CountTK, 200);
				bool offersWithAFullBag = Offers(innkeeper, player, "hospodsky:8");
				long gold = player.Gold;
				dInn.Press("hospodsky:8", innkeeper, player);
				bool notWithoutGold = (Q017.GetProtection(player) == 1) && !bag.IsDeleted;
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Bank)).Amount = Q017.PotionPrice;
				int exp = player.Experience;
				dInn.Press("hospodsky:8", innkeeper, player);
				Report("theFullBagAndGoldForAPotion", notWithAnEmptyBag && offersWithAFullBag && notWithoutGold && (Q017.GetProtection(player) == 2) && bag.IsDeleted
					&& (player.Gold == gold) && (player.Experience == exp + 200));

				//the entry gate
				Q017.SetRuns(player, 0);
				Item gate = (Item) ThingDef.GetByDefname(Q017.EntryGateDef).Create(player.X, player.Y + 1, player.Z, player.M);
				Point4D gateWas = new Point4D(gate.X, gate.Y, gate.Z, gate.M);
				player.Level = 30;
				Q017.TryEnter(gate, player);
				bool levelGate = !Q017.IsOnRun(player);
				player.Level = 40;
				Q017.TryEnter(gate, player);
				bool crystalGate = !Q017.IsOnRun(player);
				Q017.AddCrystals(player, 1, 1);
				Q017.SetProtection(player, 0);
				int hits = player.Hits;
				Q017.TryEnter(gate, player);
				bool potionGate = !Q017.IsOnRun(player) && (player.Hits < hits) && (Q017.RedCrystals(player) == 1);
				player.Hits = player.MaxHits;
				Q017.SetProtection(player, 2);
				Q017.TryEnter(gate, player);
				bool entered = Q017.IsOnRun(player) && At(player, Q017.DungeonStart) && (Q017.RedCrystals(player) == 0) && (Q017.BlueCrystals(player) == 0)
					&& (Q017.GetProtection(player) == 0) && (gate.GetTag(Q017.LastEntryTK) != null)
					&& Array.Exists(Q017.GateSpots, p => (p.X == gate.X) && (p.Y == gate.Y) && (p.M == gate.M));
				Report("theEntryGatesRules", levelGate && crystalGate && potionGate && entered);

				//the exit: the profession's magic item, the run count up by 2, back where they came in
				Q017.TryLeave(player);
				Item shield = null;
				foreach (AbstractItem item in player.Backpack) {
					if ((item is Item mine) && (Q017.RewardOf(mine) != null)) {
						shield = mine;
					}
				}
				Report("theExitGivesTheProfessionsItem", !Q017.IsOnRun(player) && (Q017.GetRuns(player) == 2) && At(player, gateWas) && (shield != null)
					&& (shield.Name == "Stit obrance") && (shield.Color == Q017.RewardColor) && (MagicItems.Value(shield, 134) == 10) && (MagicItems.Value(shield, 136) == 5)
					&& MagicItems.IsMagic(shield) && !MagicItemShop.IsRealMagicItem(shield) && Q017.Journal.IsDone(player));

				//its rules: its profession, level 35, one at a time
				player.Level = 30;
				Wear(player, shield);
				bool lowRefused = shield.Cont is Container; //still in the pack
				NPC friend = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 1, player.Y + 1, player.Z, player.M);
				Wear(friend, player, shield); //someone else drops it on the paperdoll: the wearer's rules count
				bool droppedOnRefused = shield.Cont is Container;
				friend.Delete();
				player.Level = 40;
				Wear(player, shield);
				bool worn = (shield.Cont == player) && (MagicItems.WearerValue(player, 134) == 10);
				player.Profession = ProfessionDef.GetByDefname("profession_priest");
				Item priestShield = Q017.GiveReward(player);
				player.Profession = ProfessionDef.GetByDefname("profession_warrior");
				Wear(player, priestShield);
				bool otherProfessionRefused = priestShield.Cont is Container;
				player.Profession = ProfessionDef.GetByDefname("profession_mage");
				Item hat = Q017.GiveReward(player);
				Wear(player, hat);
				bool twoRefused = (hat.Cont is Container) && (hat.Name == "Klobouk maga") && (MagicItems.Value(hat, 25) == 10);
				player.Profession = ProfessionDef.GetByDefname("profession_warrior");
				Report("theRewardsRules", lowRefused && droppedOnRefused && worn && otherProfessionRefused && twoRefused);

				//later entries want as many crystals as the run count; one hero in 4 hours
				Q017.AddCrystals(player, 2, 2);
				Q017.SetProtection(player, 2);
				Q017.TryEnter(gate, player);
				bool lockedOut = !Q017.IsOnRun(player) && (Q017.RedCrystals(player) == 2);
				gate.RemoveTag(Q017.LastEntryTK);
				Q017.AddCrystals(player, -1, 0);
				Q017.TryEnter(gate, player);
				bool wantsTwo = !Q017.IsOnRun(player);
				Q017.AddCrystals(player, 1, 0);
				Q017.TryEnter(gate, player);
				Report("laterRunsWantMoreCrystalsAndWaitFourHours", lockedOut && wantsTwo && Q017.IsOnRun(player) && (Q017.RedCrystals(player) == 0));
				Q017.EndRun(player);
				gate.Delete();

				//the crystals: from monsters in the gate's dungeons, for a player on the quest
				int chance = QuestPortSettings.instance.terlakCrystalChance;
				QuestPortSettings.instance.terlakCrystalChance = 100;
				NPC far = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(Q017.GateSpots[0].X, Q017.GateSpots[0].Y, Q017.GateSpots[0].Z, Q017.GateSpots[0].M);
				NPC near = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 2, player.Y, player.Z, player.M);
				Q017.OnKillCredit(player, near);
				bool notOutside = (Q017.RedCrystals(player) + Q017.BlueCrystals(player)) == 0;
				Q017.OnKillCredit(player, far);
				bool dropped = (Q017.RedCrystals(player) + Q017.BlueCrystals(player)) == 1;
				int runs = Q017.GetRuns(player);
				Q017.SetRuns(player, 0);
				Q017.SetProtection(player, 0);
				Q017.OnKillCredit(player, far);
				bool notOffQuest = (Q017.RedCrystals(player) + Q017.BlueCrystals(player)) == 1;
				Q017.SetRuns(player, runs);
				QuestPortSettings.instance.terlakCrystalChance = chance;
				far.Delete();
				near.Delete();
				Report("crystalsDropInTheGatesDungeons", notOutside && dropped && notOffQuest && (QuestPortSettings.instance.terlakCrystalChance == 3));
				Report("journalTextsArePlain", Plain());
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("Q017_CHAIN_DONE: runs=" + Q017.GetRuns(player));
		}

		/// <summary>A run about to time out, and one ended by a death, both on the timer: checked by TestQ017RunsEndedCheck.</summary>
		[SteamFunction]
		public static void TestQ017RunsEndedStart(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			player.Go(Q017.DungeonStart);
			Q017.StartRun(player);
			Q017RunTimer timer = (Q017RunTimer) player.GetTimer(Q017.RunTimerTK);
			bool started = Q017.IsOnRun(player) && (timer != null);
			if (sa.Args.Trim().Trim('"') == "death") {
				Q017.OnDeath(player); //what Player.On_Death calls
			} else {
				started &= Q017.SecondsLeft(player) == Q017.RunSeconds;
				Q017.SetSecondsLeft(player, 0);
				timer.DueInSeconds = 1;
			}
			Console.WriteLine("Q017_RUN_STARTED: onRun=" + started);
		}

		[SteamFunction]
		public static void TestQ017RunsEndedCheck(Character self) {
			Player player = (Player) self;
			Console.WriteLine("Q017_RUN_ENDED: onRun=" + Q017.IsOnRun(player) + " out=" + ((player.X == Q017.FailPoint.X) && (player.Y == Q017.FailPoint.Y) && (player.M == Q017.FailPoint.M))
				+ " timer=" + (player.GetTimer(Q017.RunTimerTK) != null));
		}

		//putting it on as a drag onto the paperdoll does: the item's @DenyEquip first (TryEquipItemOnChar needs the item dragged, and a
		//test's pickup from a pack the client never opened is refused), then on if allowed
		private static void Wear(Player player, Item item) {
			Wear(player, player, item);
		}

		private static void Wear(Character putter, Player wearer, Item item) {
			DenyEquipArgs args = new DenyEquipArgs(putter, item, wearer, item.Layer);
			item.On_DenyEquip(args);
			if (args.Result.Allow) {
				wearer.TryEquip(item);
			}
		}

		private static bool Plain() {
			foreach (string text in Q017.JournalTexts) {
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
