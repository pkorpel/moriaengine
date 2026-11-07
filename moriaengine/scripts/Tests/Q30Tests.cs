using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1276 - q30 (the realm's envoy) on the server, through the dialogs' own buttons: the 14 hunts, their level gates and
	/// experience, the cloak and who may wear it, the repeated last hunt, the realms, a realm changer's half experience and the thief.
	/// Tests/Protocol/Q30Tests.cs greets Gondor's envoy through a client first.</summary>
	public static class Q30Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q30test_npcs_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q30_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestQ30Setup(Character self) {
			Player player = (Player) self;
			NPC gondor = (NPC) ((NPCDef) ThingDef.GetByDefname(GrandmasterQuest.GondorEnvoyDef)).Create(player.X + 1, player.Y, player.Z, player.M);
			NPC mordor = (NPC) ((NPCDef) ThingDef.GetByDefname(GrandmasterQuest.MordorEnvoyDef)).Create(player.X + 30, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(gondor, 900);
			NPCAiTimer.Freeze(mordor, 900);
			player.SetTag(npcsTK, new[] { gondor, mordor });
			player.Realm = Realm.Gondor;
			player.Profession = Professions.Warrior;
			player.Level = 200;
			player.Experience = 0;
			Q30.SetStage(player, true, 0);
			Q30.SetStage(player, false, 0);
			Q30.SetFinished(player, 0);
			Q30.ForgetPresel(player);
			player.RemoveTag(Q30.SwitchedRealmTK);
			Console.WriteLine("Q30_SETUP_DONE: envoy=" + gondor.Uid);
		}

		[SteamFunction]
		public static void TestQ30Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q30_BUTTON: id=" + SingletonScript<D_Q000Envoy>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ30State(Character self) {
			Console.WriteLine("Q30_STATE: stage=" + Q30.GetStage((Player) self, true));
		}

		private static void Fill(Player player, int stage, bool gondor) {
			Collector bag = Collectors.FindIn(player, Q30.BagOf(stage, gondor));
			if (bag != null) {
				bag.Collected = bag.MaxCount;
			}
		}

		private static Item FindCloak(Player player, Item except) {
			foreach (AbstractItem item in player.Backpack) {
				if ((item.Def == ThingDef.GetByDefname(Q30.CloakDef)) && (item != except)) {
					return (Item) item;
				}
			}
			return null;
		}

		[SteamFunction]
		public static void TestQ30Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			NPC gondor = npcs[0], mordor = npcs[1];
			D_Q000Envoy dGondor = SingletonScript<D_Q000Envoy>.Instance;
			D_Q000EnvoyMordor dMordor = SingletonScript<D_Q000EnvoyMordor>.Instance;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1;
			try {
				Report("theClientTookTheFirstHunt", (Q30.GetStage(player, true) == 1) && (Collectors.FindIn(player, Q30.BagOf(1, true)) != null)
					&& Q30.GondorJournal.IsStarted(player));

				//nothing for an unfilled bag; a full one for the hunt's experience
				dGondor.Press("work_giveCollector", gondor, player);
				bool notYet = Q30.GetStage(player, true) == 1;
				bool offered = !dGondor.BuildFor(gondor, player).HasResponse("work_giveCollector");
				Fill(player, 1, true);
				offered &= dGondor.BuildFor(gondor, player).HasResponse("work_giveCollector");
				int before = player.Experience;
				dGondor.Press("work_giveCollector", gondor, player);
				Report("aFullBagForExperience", notYet && offered && (Q30.GetStage(player, true) == 2) && (player.Experience - before == 150)
					&& (Collectors.FindIn(player, Q30.BagOf(1, true)) == null));

				//each hunt has its level
				player.Level = 2;
				dGondor.Press("work_accept", gondor, player);
				bool tooYoung = Q30.GetStage(player, true) == 2;
				player.Level = 200;
				Report("levelGates", tooYoung);

				//the other twelve hunts and the last; then the cloak
				before = player.Experience;
				for (int stage = 2; stage < Q30.Done; stage += 2) {
					dGondor.Press("work_accept", gondor, player);
					Fill(player, stage + 1, true);
					dGondor.Press("work_giveCollector", gondor, player);
				}
				int rest = 0;
				for (int i = 1; i < Q30.Experience.Length; i++) {
					rest += Q30.Experience[i];
				}
				Item cloak = FindCloak(player, null);
				Report("allFourteenAndTheCloak", (Q30.GetStage(player, true) == Q30.Done) && (player.Experience - before == rest) && (cloak != null)
					&& (MagicItems.Value(cloak, 70) == Q30.RegenValue) && (MagicItems.Value(cloak, 102) == Q30.DragonValue) && (cloak.Color == Q30.GondorCloakColor)
					&& (cloak.GetTag(Q30.CloakOwnerTK) == player) && (Q30.Finished(player) == 1) && Q30.GondorJournal.IsDone(player));

				//the cloak is its owner's only
				Q009Tests.Wear(player, player, cloak);
				bool worn = cloak.Cont == player;
				cloak.Cont = player.Backpack; //off again
				cloak.SetTag(Q30.CloakOwnerTK, null);
				Q009Tests.Wear(player, player, cloak);
				bool notAStranger = cloak.Cont != player;
				cloak.SetTag(Q30.CloakOwnerTK, player);
				Report("theCloaksOwner", worn && notAStranger);

				//a lost cloak: the last hunt again, 8 then 12 hearts, no experience
				bool asks = dGondor.BuildFor(gondor, player).HasResponse("work_info");
				dGondor.Press("work_accept", gondor, player);
				Collector hearts = Collectors.FindIn(player, Q30.BagOf(Q30.Done - 1, true));
				bool eight = (Q30.GetStage(player, true) == Q30.Done - 1) && (hearts != null) && (hearts.MaxCount == 8);
				Fill(player, Q30.Done - 1, true);
				before = player.Experience;
				dGondor.Press("work_giveCollector", gondor, player);
				Item second = FindCloak(player, cloak);
				bool again = (Q30.GetStage(player, true) == Q30.Done) && (player.Experience == before) && (second != null) && (Q30.Finished(player) == 2);
				dGondor.Press("work_accept", gondor, player);
				hearts = Collectors.FindIn(player, Q30.BagOf(Q30.Done - 1, true));
				Report("aLostCloak", asks && eight && again && (hearts != null) && (hearts.MaxCount == 12));
				hearts.Delete();
				Q30.SetStage(player, true, Q30.Done);

				//each envoy his own realm
				bool mordorRefuses = (Q30.Refusal(player, false) ?? "").Contains("Gondoru") && (Q30.Refusal(player, true) == null);

				//a realm changer: the old ladder goes, half experience until past it; a thief who began it isn't taken
				player.Realm = Realm.Mordor;
				player.SetTag(Q30.SwitchedRealmTK, 1);
				player.Profession = Professions.Thief;
				bool thief = Q30.Refusal(player, false) == "Jdi pryc";
				player.Profession = Professions.Warrior;
				bool taken = (Q30.Refusal(player, false) == null) && (Q30.Presel(player) == Q30.Done) && (Q30.GetStage(player, true) == 0);
				dMordor.Press("work_accept", mordor, player);
				Fill(player, 1, false);
				before = player.Experience;
				dMordor.Press("work_giveCollector", mordor, player);
				Report("realmsAndChangers", mordorRefuses && thief && taken && (Q30.GetStage(player, false) == 2) && (player.Experience - before == 75));

				bool plain = true;
				foreach (bool g in new[] { true, false }) {
					for (int stage = 1; stage <= Q30.Done; stage++) {
						Q30.SetStage(player, g, stage);
						string text = Q30.JournalText(player, g);
						plain &= (text != null) && Q009Tests.Plain(text);
					}
				}
				Report("journalTextsArePlain", plain);
			} finally {
				player.Account.PLevel = plevel;
				player.Realm = Realm.Gondor;
				player.RemoveTag(Q30.SwitchedRealmTK);
			}
			gondor.Delete();
			mordor.Delete();
			Console.WriteLine("Q30_CHAIN_DONE: stage=" + Q30.GetStage(player, true));
		}
	}
}
