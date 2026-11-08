using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1278 - q041 (the smuggler) on the server, through the dialogs' own buttons: who he talks to, the bribe, the Gondor
	/// hero's elf and the Mordor hero's gifts for Borek, the price, the portal, the realm gate both ways and its minute of quiet.
	/// Tests/Protocol/Q041Q099Tests.cs greets him through a client first.</summary>
	public static class Q041Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q041test_npcs_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q041_CHECK: " + name + "=" + ok);
		}

		private static NPC Make(Player player, string def, int dx) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname(def)).Create(player.X + dx, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(npc, 900);
			return npc;
		}

		[SteamFunction]
		public static void TestQ041Setup(Character self) {
			Player player = (Player) self;
			NPC paserak = Make(player, Q041.PaserakDef, 1);
			NPC borek = Make(player, Q041.BorekDef, 30);
			player.SetTag(npcsTK, new[] { paserak, borek });
			player.Realm = Realm.Gondor;
			player.Level = 20;
			Q041.SetStage(player, 0);
			player.RemoveTag(Q30.SwitchedRealmTK);
			player.RemoveTag(Q041.PassedGateTK);
			Console.WriteLine("Q041_SETUP_DONE: paserak=" + paserak.Uid);
		}

		[SteamFunction]
		public static void TestQ041Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q041_BUTTON: id=" + SingletonScript<D_Q041Paserak>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ041State(Character self) {
			Console.WriteLine("Q041_STATE: stage=" + Q041.GetStage((Player) self));
		}

		[SteamFunction]
		public static void TestQ041Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			Point4D start = new Point4D(player.X, player.Y, player.Z, player.M);
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //GMs pay nothing and pass the gates
			try {
				Chain(player, npcs[0], npcs[1]);
			} finally {
				player.Account.PLevel = plevel;
				player.Realm = Realm.Gondor;
				player.Go(start);
			}
			npcs[0].Delete();
			npcs[1].Delete();
			Console.WriteLine("Q041_CHAIN_DONE: stage=" + Q041.GetStage(player));
		}

		private static void Chain(Player player, NPC paserak, NPC borek) {
			D_Q041Paserak dialog = SingletonScript<D_Q041Paserak>.Instance;
			D_Q041Borek boreksDialog = SingletonScript<D_Q041Borek>.Instance;
			Report("theClientPromisedTheMoney", (Q041.GetStage(player) == 1) && Q041.Journal.IsStarted(player));

			//level 15, Gondor or Mordor; outcasts never
			player.Level = 10;
			bool young = Q041.Refusal(player).Contains("silnejsi");
			player.Level = 20;
			player.Realm = Realm.GondorOutcast;
			bool outcast = Q041.Refusal(player).Contains("silnejsi");
			player.Realm = Realm.None;
			bool none = Q041.Refusal(player).Contains("ani ja");
			player.Realm = Realm.Gondor;
			Report("whoHeTalksTo", young && outcast && none && (Q041.Refusal(player) == null));

			//10 000 gold for a word; a new character's own gold out of the way first
			long own = GoldUtils.CountGoldOn(player);
			if (own > 0) {
				GoldUtils.Pay(player, own, PaySource.Usual, "");
			}
			dialog.Press("q041_give_money", paserak, player);
			bool notWithoutMoney = (Q041.GetStage(player) == 1) && !dialog.BuildFor(paserak, player).HasResponse("q041_give_money");
			GoldUtils.Grant(player, (uint) Q041.Bribe);
			bool offered = dialog.BuildFor(paserak, player).HasResponse("q041_give_money");
			dialog.Press("q041_give_money", paserak, player);
			Report("theBribe", notWithoutMoney && offered && (Q041.GetStage(player) == 2) && (GoldUtils.CountGoldOn(player) == 0));

			//Gondor: the elf
			dialog.Press("q041_want_to", paserak, player);
			bool wants = Q041.GetStage(player) == 3;
			dialog.Press("q041_give_him", paserak, player); //Mordor's answer
			bool notMordors = Q041.GetStage(player) == 3;
			dialog.Press("q041_will_kill", paserak, player);
			bool promised = Q041.GetStage(player) == 4;
			NPC elf = (NPC) ((NPCDef) ThingDef.GetByDefname(Q041.ElfDef)).Create(player.X + 2, player.Y + 2, player.Z, player.M);
			NPCAiTimer.Freeze(elf, 900);
			elf.CauseDeath(player); //a real death: its @death moves the killer on
			if (!elf.IsDeleted) {
				elf.Delete();
			}
			bool killed = Q041.GetStage(player) == 5;
			bool offeredKill = dialog.BuildFor(paserak, player).HasResponse("q041_killed_elf");
			dialog.Press("q041_killed_elf", paserak, player);
			Report("gondorKillsTheElf", wants && notMordors && promised && killed && offeredKill && (Q041.GetStage(player) == 8));

			//an elf killed at another stage counts nothing
			Q041.SetStage(player, 3);
			NPC other = (NPC) ((NPCDef) ThingDef.GetByDefname(Q041.ElfDef)).Create(player.X + 2, player.Y + 2, player.Z, player.M);
			other.CauseDeath(player);
			if (!other.IsDeleted) {
				other.Delete();
			}
			Report("theElfOnlyAtFour", Q041.GetStage(player) == 3);

			//Mordor: Borek's gifts
			player.Realm = Realm.Mordor;
			dialog.Press("q041_will_kill", paserak, player);
			bool notGondors = Q041.GetStage(player) == 3;
			bool noBargainYet = !boreksDialog.BuildFor(borek, player).HasResponse("q041_paserak_send_him");
			dialog.Press("q041_give_him", paserak, player);
			QuestDialogContent d = boreksDialog.BuildFor(borek, player);
			bool bargain = d.HasResponse("q041_paserak_send_him") && d.HasResponse("q041_doesnt_have_items") && !d.HasResponse("q041_has_items");
			boreksDialog.Press("q041_has_items", borek, player);
			bool notEmptyHanded = Q041.GetStage(player) == 6;
			Q006.Give(player, Q041.FishDef, Q041.FishSteaks);
			Q006.Give(player, Q041.RefreshDef, Q041.RefreshPotions);
			bool hasThem = boreksDialog.BuildFor(borek, player).HasResponse("q041_has_items");
			boreksDialog.Press("q041_has_items", borek, player);
			bool helped = (Q041.GetStage(player) == 7) && (Q006.Count(player, Q041.FishDef) == 0) && (Q006.Count(player, Q041.RefreshDef) == 0);
			dialog.Press("q041_gave_him", paserak, player);
			Report("mordorHelpsBorek", notGondors && noBargainYet && bargain && notEmptyHanded && hasThem && helped && (Q041.GetStage(player) == 8));

			//the price: 10 ingots, a bag per change + 1, a roast pig, level x 10 000 + 1 000 000
			player.Level = 20; //the elf's experience may have raised it
			bool price = (Q041.Price(player) == 1200000) && (Q041.BagsWanted(player) == 1);
			bool notYet = dialog.BuildFor(paserak, player).HasResponse("q041_dont_have_yet");
			dialog.Press("q041_have_items", paserak, player);
			notYet &= Q041.GetStage(player) == 8;
			Q006.Give(player, Q041.IngotDef, Q041.Ingots);
			Q006.Give(player, Q041.BagDef);
			Q006.Give(player, Q041.PigDef);
			GoldUtils.Grant(player, (uint) Q041.Price(player)); //the transfer is from the bank, as Erebor's pay_from_bank
			bool notFromThePack = !dialog.BuildFor(paserak, player).HasResponse("q041_have_items");
			GoldUtils.Pay(player, Q041.Price(player), PaySource.Pack, "");
			GoldUtils.GrantToBank(player, (uint) Q041.Price(player));
			bool ready = dialog.BuildFor(paserak, player).HasResponse("q041_have_items");
			dialog.Press("q041_have_items", paserak, player);
			Report("thePrice", price && notYet && notFromThePack && ready && (Q041.GetStage(player) == 9) && (GoldUtils.CountGoldOn(player) == 0)
				&& (Q006.Count(player, Q041.IngotDef) == 0) && (Q006.Count(player, Q041.BagDef) == 0) && (Q006.Count(player, Q041.PigDef) == 0));

			//the portal: level 15 and stage 9, else back to the shore
			Item gate = (Item) ThingDef.GetByDefname(Q041.GateDef).Create(player.X + 3, player.Y, player.Z, player.M);
			Item portal = (Item) ThingDef.GetByDefname(Q041.PortalDef).Create(player.X + 6, player.Y, player.Z, player.M);
			portal.MoreP = new Point4D(gate.X + 1, gate.Y, gate.Z, gate.M); //beside the gate: a hero lands there and steps in
			E_Q041Portal portals = SingletonScript<E_Q041Portal>.Instance;
			player.Level = 10;
			portals.On_Step(portal, player);
			bool youngRefused = (player.X == Q041.RefusedSpot.X) && (player.Y == Q041.RefusedSpot.Y);
			player.Level = 20;
			Q041.SetStage(player, 8);
			player.Go(portal.X, portal.Y + 2, portal.Z, portal.M);
			portals.On_Step(portal, player);
			bool notReady = (player.X == Q041.RefusedSpot.X) && (player.Y == Q041.RefusedSpot.Y);
			Q041.SetStage(player, 9);
			portals.On_Step(portal, player); //called, not walked: a walk onto it would also fire its own trigger
			Report("thePortal", youngRefused && notReady && (player.X == portal.MoreP.X) && (player.Y == portal.MoreP.Y) && (Q041.GetStage(player) == 9));

			//the gate: an outcast never, the others at stage 9; Mordor to Gondor
			E_Q041Gate gates = SingletonScript<E_Q041Gate>.Instance;
			player.Realm = Realm.MordorOutcast;
			gates.On_Step(gate, player);
			bool outcastStays = (player.Realm == Realm.MordorOutcast) && (Q041.GetStage(player) == 9);
			player.Realm = Realm.Mordor;
			Q041.SetStage(player, 8);
			gates.On_Step(gate, player);
			bool notBefore = (player.Realm == Realm.Mordor) && (Q041.GetStage(player) == 8);
			Q041.SetStage(player, 9);
			Notoriety.SetMurderCount(player, 7);
			player.Karma = -3000;
			gates.On_Step(gate, player);
			Point4D gondorHome = NationData.GetHome(Nation.Gondoran);
			Report("toGondor", outcastStays && notBefore && (player.Realm == Realm.Gondor) && (Notoriety.GetMurderCount(player) == 0) && (player.Karma == 0)
				&& (player.Home.X == gondorHome.X) && (player.Home.Y == gondorHome.Y) && (player.X == gondorHome.X) && (player.Y == gondorHome.Y) && (Q041.GetStage(player) == Q041.Done)
				&& (Q041.Changes(player) == 1) && Q041.Journal.IsDone(player));

			//#1307 - Gondor's posts: an office, a city's mayoralty and citizenship, the realm uniform; all stay behind at the switch
			bool newOffice = RealmOffice.For(Realm.Gondor) == null;
			RealmOffice office = RealmOffice.For(Realm.Gondor) ?? new RealmOffice { realm = Realm.Gondor };
			Character holderBefore = office.holder;
			office.holder = player;
			Town town = Town.ForCity("Minas_Tirith");
			Item tempStone = null;
			if (town == null) {
				tempStone = (Item) ThingDef.GetByDefname("i_townstone").Create(player.X, player.Y, player.Z, player.M);
				town = Town.ForStone(tempStone, "Minas_Tirith");
			}
			town.AddCitizen(player);
			town.SetMayor(player);
			RealmUniform.Grant(player, 12, null);
			bool heldThem = (office.holder == player) && (town.FindCitizen(player) != null) && (town.mayor == player) && (RealmUniform.EntryOf(player) != null);

			//a minute's quiet after a change, then Gondor to Mordor; the next price grows
			Q041.SetStage(player, 9);
			gates.On_Step(gate, player);
			bool quiet = (player.Realm == Realm.Gondor) && (Q041.GetStage(player) == 9);
			player.SetTag(Q041.PassedGateTK, Globals.TimeInSeconds - Q041.GateQuietSeconds - 1);
			gates.On_Step(gate, player);
			Point4D mordorHome = NationData.GetHome(Nation.Mordoran);
			Report("toMordorAndAgain", quiet && (player.Realm == Realm.Mordor) && (player.Karma == Q041.MordorKarma) && (player.Home.X == mordorHome.X) && (player.Home.Y == mordorHome.Y)
				&& (Q041.Changes(player) == 2) && (Q041.BagsWanted(player) == 3) && dialog.BuildFor(paserak, player).HasResponse("q041_bring_money"));
			Report("theOldRealmsPostsStayBehind", heldThem && (office.holder == null) && (town.FindCitizen(player) == null) && (town.mayor != player)
				&& (RealmUniform.EntryOf(player) == null));
			office.holder = holderBefore;
			if (newOffice) {
				office.Remove();
			}
			if (tempStone != null) {
				tempStone.Delete();
			}
			gate.Delete();
			portal.Delete();

			bool plain = true;
			for (int stage = 1; stage <= Q041.Done; stage++) {
				Q041.SetStage(player, stage);
				string text = Q041.GetJournalText(player);
				plain &= (text != null) && Q009Tests.Plain(text);
			}
			Report("journalTextsArePlain", plain);
		}
	}
}
