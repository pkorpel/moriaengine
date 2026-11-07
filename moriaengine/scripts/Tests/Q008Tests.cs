using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1274 - q008 (Black Mustang) on the server, through the dialogs' own buttons: who Ancam talks to, the crate's reagents,
	/// the four monsters' kills, the corral gate and its pads, taming, the wait path, the repeat and the ban.
	/// Tests/Protocol/Q008Q31Tests.cs greets Ancam through a client first.</summary>
	public static class Q008Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q008test_npcs_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q008_CHECK: " + name + "=" + ok);
		}

		private static NPC Mustang(Player player) {
			NPC mustang = (NPC) ((NPCDef) ThingDef.GetByDefname(Q008.MustangDef)).Create(player.X + 8, player.Y + 3, player.Z, player.M);
			NPCAiTimer.Freeze(mustang, 900);
			return mustang;
		}

		/// <summary>Ancam beside a ranger with Taming and Animal Lore 100, his mustang in the corral.</summary>
		[SteamFunction]
		public static void TestQ008Setup(Character self) {
			Player player = (Player) self;
			NPC ancam = (NPC) ((NPCDef) ThingDef.GetByDefname(Q008.AncamDef)).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(ancam, 900);
			NPC mustang = Mustang(player);
			player.SetTag(npcsTK, new[] { ancam, mustang });
			player.Profession = Professions.Ranger;
			player.SetRealSkillValue((int) SkillName.AnimalTaming, 1000);
			player.SetRealSkillValue((int) SkillName.AnimalLore, 1000);
			player.Level = 200;
			player.Experience = 0;
			Q008.SetStage(player, 0);
			Q008.SetHandedIn(player, false);
			Q008.ForgetKills(player);
			player.RemoveTag(TagKey.Acquire("_q008Rejected_"));
			Console.WriteLine("Q008_SETUP_DONE: ancam=" + ancam.Uid);
		}

		[SteamFunction]
		public static void TestQ008Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q008_BUTTON: id=" + SingletonScript<D_Q008Ancam>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ008State(Character self) {
			Console.WriteLine("Q008_STATE: stage=" + Q008.GetStage((Player) self));
		}

		private static Item Give(Player player, string defname, int amount) {
			return Q006.Give(player, defname, amount);
		}

		private static void FillCrate(Player player, Item crate) {
			Q008.Deposit(crate, player, Give(player, "i_reag_ginseng", Q008.ReagentsNeeded));
			Q008.Deposit(crate, player, Give(player, "i_reag_ginseng", 1));
		}

		private static void KillAll(Player player) {
			foreach (string def in Q008.MonsterDefs) {
				NPC monster = (NPC) ((NPCDef) ThingDef.GetByDefname(def)).Create(player.X + 2, player.Y + 2, player.Z, player.M);
				NPCAiTimer.Freeze(monster, 900);
				monster.CauseDeath(player); //a real death: its @Death tags the killer
				if (!monster.IsDeleted) {
					monster.Delete();
				}
			}
		}

		[SteamFunction]
		public static void TestQ008Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			NPC ancam = npcs[0], mustang = npcs[1];
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //the gate and the pads let GMs through
			try {
				Chain(player, ancam, mustang);
			} finally {
				player.Account.PLevel = plevel;
			}
			ancam.Delete(); //the next test's greeting mustn't reach him
			Console.WriteLine("Q008_CHAIN_DONE: stage=" + Q008.GetStage(player));
		}

		private static void Chain(Player player, NPC ancam, NPC mustang) {
			D_Q008Ancam dialog = SingletonScript<D_Q008Ancam>.Instance;
			Item crate = Q008.FindCrate(player);
			Report("theClientTookTheCrate", (Q008.GetStage(player) == 1) && (crate != null) && crate.Flag_NonMovable);

			//rangers with both skills only
			player.Profession = Professions.Warrior;
			bool notWarriors = Q008.DialogNumber(ancam, player) == 0;
			player.Profession = Professions.Ranger;
			Report("rangersOnly", notWarriors && (Q008.DialogNumber(ancam, player) == 3));

			//2500 reagents counted, the next deposit fills the crate; the four kills
			Q008.Deposit(crate, player, Give(player, "i_reag_ginseng", 2400));
			bool counted = (Q008.CrateReagents(crate) == 2400) && !Q008.CrateFull(player);
			Q008.Deposit(crate, player, Give(player, "i_reag_bone", 200));
			bool over = (Q008.CrateReagents(crate) == 2600) && !Q008.CrateFull(player);
			Q008.Deposit(crate, player, Give(player, "i_reag_bone", 1));
			bool full = Q008.CrateFull(player);
			bool notWithoutKills = Q008.DialogNumber(ancam, player) == 3;
			KillAll(player);
			Report("reagentsAndKills", counted && over && full && notWithoutKills && Q008.DialogNumber(ancam, player) == 7);

			//the crate handed in with the mustang there: the gate is theirs
			bool offered = dialog.BuildFor(ancam, player).HasResponse("ancam_7:1");
			dialog.Press("ancam_7:1", ancam, player);
			Report("theCrateHandedIn", offered && (Q008.GetStage(player) == 2) && crate.IsDeleted && !Q008.AllKilled(player));

			//the corral: the outside pad unlocks the gate, the inside one locks it behind them; out only once the mustang is tamed
			Item gate = (Item) ThingDef.GetByDefname(Q008.GateDef).Create(player.X + 5, player.Y + 3, player.Z, player.M);
			gate.AddTriggerGroup(SingletonScript<E_Q008Gate>.Instance);
			gate.LockUid = gate.Uid;
			Item padOut = (Item) ThingDef.GetByDefname(Q008.PadOutsideDef).Create(gate.X, gate.Y - 1, gate.Z, gate.M);
			Item padIn = (Item) ThingDef.GetByDefname(Q008.PadInsideDef).Create(gate.X, gate.Y + 1, gate.Z, gate.M);
			E_Q008Pad pads = SingletonScript<E_Q008Pad>.Instance;
			bool lockedFirst = !Q008.UseGate(gate, player) && !gate.IsDoorOpen;
			bool notTheirsYet = Q008.RefusesTaming(player, mustang);
			pads.On_Step(padOut, player);
			bool opens = Q008.UseGate(gate, player) && gate.IsDoorOpen;
			int before = player.Experience;
			pads.On_Step(padIn, player);
			bool lockedBehind = (Q008.GetStage(player) == 3) && !gate.IsDoorOpen && (player.Experience - before == Q008.CorralExp) && !Q008.UseGate(gate, player);
			bool nowTheirs = !Q008.RefusesTaming(player, mustang);
			mustang.ControlMaster = player; //tamed
			bool wayOut = Q008.UseGate(gate, player) && gate.IsDoorOpen;
			pads.On_Step(padOut, player);
			bool done = (Q008.GetStage(player) == 4) && !gate.IsDoorOpen && Q008.Journal.IsDone(player) && !Q008.UseGate(gate, player);
			Report("theCorral", lockedFirst && notTheirsYet && opens && lockedBehind && nowTheirs && wayOut && done);

			//the gate: the outside pad's unlock lasts a minute and only for a hero at stage 2; no key opens it (the open-door macro)
			mustang.ControlMaster = null;
			Q008.SetStage(player, 2);
			pads.On_Step(padOut, player);
			Q008.SetStage(player, 0);
			bool notForOthers = !Q008.UseGate(gate, player);
			Q008.SetStage(player, 2);
			gate.SetTag(Q008.UnlockedTK, Globals.TimeInSeconds - Q008.UnlockSeconds - 1);
			bool expires = !Q008.UseGate(gate, player);
			Item key = (Item) ThingDef.GetByDefname("i_key_copper").Create(player.Backpack);
			bool noKey = !gate.TryUseDoor(player) && !gate.IsDoorOpen;
			key.Delete();
			mustang.ControlMaster = player;
			Q008.SetStage(player, 4);
			Report("gateRules", notForOthers && expires && noKey);

			//kills count only on the quest (stage 1), a pet's for its master
			KillAll(player);
			bool notAfter = !Q008.AllKilled(player);
			//a new mustang: another round
			mustang.Delete();
			mustang = Mustang(player);
			bool again = (Q008.DialogNumber(ancam, player) == 10) && dialog.BuildFor(ancam, player).HasResponse("ancam_10:1");
			dialog.Press("ancam_10:1", ancam, player);
			crate = Q008.FindCrate(player);
			Report("anotherRound", again && (Q008.GetStage(player) == 1) && (crate != null));
			NPC pet = (NPC) ((NPCDef) ThingDef.GetByDefname("c_rabbit")).Create(player.X + 3, player.Y, player.Z, player.M);
			pet.ControlMaster = player;
			NPC victim = (NPC) ((NPCDef) ThingDef.GetByDefname(Q008.MonsterDefs[0])).Create(player.X + 4, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(victim, 900);
			victim.CauseDeath(pet);
			Report("killsOnlyOnTheQuest", notAfter && Q008.Killed(player, 0) && !Q008.Killed(player, 1));
			pet.Delete();
			if (crate == null) {
				return;
			}

			//handed in before a mustang came: Ancam keeps the crate, then sends the hero once one has (+400)
			FillCrate(player, crate);
			KillAll(player);
			mustang.ControlMaster = player;
			bool four = Q008.DialogNumber(ancam, player) == 4;
			dialog.Press("ancam_4:1", ancam, player);
			bool waiting = Q008.HandedIn(player) && crate.IsDeleted && (Q008.GetStage(player) == 0) && (Q008.DialogNumber(ancam, player) == 5);
			mustang.Delete();
			mustang = Mustang(player);
			before = player.Experience;
			dialog.Press("ancam_6:1", ancam, player);
			Report("theWaitPath", four && waiting && (Q008.GetStage(player) == 2) && !Q008.HandedIn(player) && (player.Experience - before == Q008.WaitExp));

			//a greedy answer bans the hero for good; buttons of a dialog not shown do nothing
			dialog.Press("ancam_2:2", ancam, player);
			bool notShown = !Q008.IsRejected(player);
			Q008.SetStage(player, 0);
			dialog.Press("ancam_2:2", ancam, player);
			Report("banned", notShown && Q008.IsRejected(player) && (Q008.GetStage(player) == 0));

			bool plain = true;
			foreach (string text in Q008.JournalTexts) {
				plain &= Q009Tests.Plain(text);
			}
			Report("journalTextsArePlain", plain);
			mustang.Delete();
			gate.Delete();
			padOut.Delete();
			padIn.Delete();
		}
	}
}
