using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1270 - q009 (Talathwen's ring) on the server, through the dialogs' own buttons: the exact cheque, the wait and how
	/// asking early and leaving politely move it, the ring and who may wear it, her last dialog. Tests/Protocol/Q009Q018Tests.cs
	/// greets her through a client first.</summary>
	public static class Q009Tests {
		private static readonly TagKey npcTK = TagKey.Acquire("_q009test_npc_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q009_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestQ009Setup(Character self) {
			Player player = (Player) self;
			NPC tailor = (NPC) ((NPCDef) ThingDef.GetByDefname(Q009.TailorDef)).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(tailor, 900);
			player.SetTag(npcTK, tailor);
			Q009.SetStage(player, 0);
			Q009.ForgetRingTime(player);
			player.SetRealSkillValue((int) SkillName.Tailoring, 0);
			Console.WriteLine("Q009_SETUP_DONE: tailor=" + tailor.Uid);
		}

		[SteamFunction]
		public static void TestQ009Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q009_BUTTON: id=" + SingletonScript<D_Q009Krejci>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ009State(Character self) {
			Console.WriteLine("Q009_STATE: stage=" + Q009.GetStage((Player) self));
		}

		private static CashiersItem Cheque(Player player, int value) {
			CashiersItem cheque = (CashiersItem) ThingDef.GetByDefname(CashiersItem.CheckDef).Create(player.Backpack);
			cheque.Value = value;
			return cheque;
		}

		private static bool Near(double a, double b) {
			return Math.Abs(a - b) < 5;
		}

		[SteamFunction]
		public static void TestQ009Chain(Character self) {
			Player player = (Player) self;
			NPC tailor = (NPC) player.GetTag(npcTK);
			D_Q009Krejci dialog = SingletonScript<D_Q009Krejci>.Instance;
			Report("theClientAgreed", Q009.GetStage(player) == 1);
			Report("theJournalListsIt", QuestJournal.Entries.Contains(Q009.Journal) && Q009.Journal.IsStarted(player) && !Q009.Journal.IsDone(player));

			//only a cheque of exactly 5M
			CashiersItem wrong = Cheque(player, Q009.Price - 1);
			bool notForLess = !dialog.BuildFor(tailor, player).HasResponse("krejci_3:1");
			dialog.Press("krejci_3:1", tailor, player);
			bool keptIt = !wrong.IsDeleted && (Q009.GetStage(player) == 1);
			wrong.Delete();
			CashiersItem cheque = Cheque(player, Q009.Price);
			bool offered = dialog.BuildFor(tailor, player).HasResponse("krejci_3:1");
			dialog.Press("krejci_3:1", tailor, player);
			Report("exactlyFiveMillion", notForLess && keptIt && offered && cheque.IsDeleted && (Q009.GetStage(player) == 2)
				&& Near(Q009.SecondsToRing(player), Q009.WaitSeconds));

			//asking early puts it off, a polite leave brings it nearer, the ring isn't handed over early
			bool notReady = dialog.BuildFor(tailor, player).HasResponse("krejci_4:1");
			bool putOff = Near(Q009.SecondsToRing(player), Q009.WaitSeconds + Q009.NagSeconds);
			dialog.Press("krejci_4:1", tailor, player);
			bool nearer = Near(Q009.SecondsToRing(player), Q009.WaitSeconds + Q009.NagSeconds - Q009.PoliteSeconds);
			dialog.Press("krejci_5:1", tailor, player);
			Report("theWaitMoves", notReady && putOff && nearer && (Q009.GetStage(player) == 2) && (FindRing(player) == null));

			//ready: the ring, bound to its owner
			Q009.SetReadyIn(player, -1);
			bool offersRing = dialog.BuildFor(tailor, player).HasResponse("krejci_5:1");
			dialog.Press("krejci_5:1", tailor, player);
			Item ring = FindRing(player);
			Report("theRing", offersRing && (ring != null) && (ring.GetTag(Q009.RingOwnerTK) == player) && (Q009.GetStage(player) == 3) && Q009.Journal.IsDone(player));

			//its owner at Tailoring 100 only: it gives +10 and takes it back
			Wear(player, player, ring);
			bool lowRefused = ring.Cont is Container;
			player.SetRealSkillValue((int) SkillName.Tailoring, 1000);
			ring.SetTag(Q009.RingOwnerTK, null);
			Wear(player, player, ring);
			bool strangerRefused = ring.Cont is Container;
			ring.SetTag(Q009.RingOwnerTK, player);
			NPC friend = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 1, player.Y + 1, player.Z, player.M);
			Wear(friend, player, ring); //put on the owner's paperdoll by another: the owner's rules count
			friend.Delete();
			bool worn = (ring.Cont == player) && (player.GetSkill((int) SkillName.Tailoring) == 1100);
			player.TryEquip((Item) ThingDef.GetByDefname("i_ring_gold").Create(player.Backpack)); //same layer: takes the ring off
			bool off = (player.GetSkill((int) SkillName.Tailoring) == 1000) && (ring.GetTag(Q009.RingBoostTK) == null);
			Report("theRingsRules", lowRefused && strangerRefused && worn && off);

			//her last dialog, and her shop from every dialog
			bool last = dialog.BuildFor(tailor, player).HasResponse("krejci_6:1") && dialog.BuildFor(tailor, player).HasResponse("krejci_6:2");
			dialog.Press("krejci_6:2", tailor, player);
			Report("herLastDialogAndShop", last && (Q009.GetStage(player) == 3));
			Report("journalTextsArePlain", Plain(Q009.GetJournalText(player)));
			Console.WriteLine("Q009_CHAIN_DONE: stage=" + Q009.GetStage(player));
		}

		private static Item FindRing(Player player) {
			foreach (AbstractItem item in player.Backpack) {
				if (item.Def == ThingDef.GetByDefname(Q009.RingDef)) {
					return (Item) item;
				}
			}
			return null;
		}

		//as a client's drop on the paperdoll: the deny check first (TryEquip skips it)
		internal static void Wear(Character putter, Player wearer, Item item) {
			DenyEquipArgs args = new DenyEquipArgs(putter, item, wearer, item.Layer);
			item.On_DenyEquip(args);
			if (args.Result.Allow) {
				wearer.TryEquip(item);
			}
		}

		internal static bool Plain(string text) {
			foreach (char c in text) {
				if (c > 127) {
					return false;
				}
			}
			return true;
		}
	}
}
