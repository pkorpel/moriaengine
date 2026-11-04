using System;
using System.Globalization;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1116 - server side of Tests/Protocol/ConquestTests.cs.</summary>
	public static class ConquestTests {
		private static WarDeclarator stone;

		private static Player Named(string name) {
			foreach (AbstractCharacter ch in GameServer.GetAllPlayers()) {
				if ((ch is Player player) && (player.Name == name)) {
					return player;
				}
			}
			return null;
		}

		private static void MakeAttacker(Player player) {
			player.Account.PLevel = 1;
			player.Realm = Realm.Gondor;
			Conquest.GiveRight(player);
		}

		/// <summary>Both testers Gondor players with the right, a Gondor declarator beside them, the clock at noon on 4.11.2026.</summary>
		[SteamFunction]
		public static void TestConquestReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			Conquest.Reset();
			Conquest.clock = new DateTime(2026, 11, 4, 12, 0, 0);
			if ((stone != null) && !stone.IsDeleted) {
				stone.Delete();
			}
			stone = (WarDeclarator) ThingDef.GetByDefname("i_war_declarator").Create(self.X + 1, self.Y, self.Z, self.M);
			stone.SetRealm(Realm.Gondor);
			MakeAttacker((Player) self);
			Player second = Named("Conquest Second");
			if (second != null) {
				MakeAttacker(second);
				second.Go(self.X, self.Y + 1, self.Z, self.M);
			}
			Console.WriteLine("CONQUEST_READY: stone=" + stone.FlaggedUid + " name=" + stone.Name + " second=" + (second != null) + " |");
		}

		[SteamFunction]
		public static void TestConquestState(Character self) {
			WarFront front = Conquest.Front(Realm.Gondor);
			Console.WriteLine("CONQUEST_STATE: declared=" + (front.declaredOn ?? "none") + " lord=" + ((front.lord == null) ? "none" : front.lord.Name) +
				" begins=" + front.begins.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " queue=" + front.queue.Count +
				" running=" + front.IsRunning + " remaining=" + front.remaining + " right=" + Conquest.HasRight(self) + " |");
		}

		/// <summary>The clock to "yyyy-MM-dd HH:mm", then one tick of the Gondor front's war clock.</summary>
		[SteamFunction]
		public static void TestConquestTick(Character self, ScriptArgs sa) {
			string when = sa.Args.Trim().Trim('"');
			Conquest.clock = DateTime.ParseExact(when, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
			Conquest.Tick(Conquest.Front(Realm.Gondor));
			Console.WriteLine("CONQUEST_TICK");
		}

		/// <summary>The war's last 10 s.</summary>
		[SteamFunction]
		public static void TestConquestRunOut(Character self) {
			Conquest.Front(Realm.Gondor).remaining = 10;
			Conquest.Tick(Conquest.Front(Realm.Gondor));
			Console.WriteLine("CONQUEST_RUNOUT");
		}

		private static NPC lieutenant;

		/// <summary>#1116 step 9 - a Gondor player of rank 3 and the given level, 100 000 gold, no right, Norbert beside them.</summary>
		[SteamFunction]
		public static void TestLieutenantReady(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			player.Account.PLevel = 1;
			player.Realm = Realm.Gondor;
			player.SkillRank = SkillRank.Grandmaster;
			player.Level = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			Conquest.Reset();
			Conquest.TakeRight(player);
			foreach (AbstractItem item in new System.Collections.Generic.List<AbstractItem>(player.Backpack)) {
				item.Delete();
			}
			((Item) ThingDef.GetByDefname("i_gold").Create(player.Backpack)).Amount = WarLieutenant.Price;
			lieutenant = (NPC) ThingDef.GetByDefname(WarLieutenant.GondorDef).Create(self.X + 1, self.Y, self.Z, self.M);
			lieutenant.DeleteTimer(SteamEngine.Timers.TimerKey.Acquire("_npcAiTimer_"));
			Console.WriteLine("LIEUTENANT_READY: npc=" + lieutenant.FlaggedUid + " |");
		}

		[SteamFunction]
		public static void TestLieutenantLevel(Character self, ScriptArgs sa) {
			((Player) self).Level = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			Console.WriteLine("LIEUTENANT_LEVEL");
		}

		[SteamFunction]
		public static void TestLieutenantState(Character self) {
			Console.WriteLine("LIEUTENANT_STATE: right=" + Conquest.HasRight(self) + " gold=" + GoldUtils.CountGoldIn(self.Backpack) + " |");
		}

		/// <summary>A declaration for the next evening, then a tick the day after it: it lapses.</summary>
		[SteamFunction]
		public static void TestConquestLapse(Character self) {
			Conquest.Front(Realm.Gondor).Clear();
			string declared = Conquest.Declare((Player) self, Conquest.Front(Realm.Gondor), 6, 11, 19, out bool done, out _);
			Conquest.clock = new DateTime(2026, 11, 7, 12, 0, 0);
			Conquest.Tick(Conquest.Front(Realm.Gondor));
			Console.WriteLine("CONQUEST_LAPSE: declared=" + done + " after=" + (Conquest.Front(Realm.Gondor).declaredOn ?? "none") + " |" + declared);
			Conquest.clock = null;
		}
	}
}
