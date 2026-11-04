using System;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1107 - server side of Tests/Protocol/ModerationTests.cs.</summary>
	public static class ModerationTests {
		private static Point4D before;

		[SteamFunction]
		public static void TestJailReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			before = new Point4D(self.X, self.Y, self.Z, self.M);
			Console.WriteLine("JAIL_READY: uid=" + self.Uid + " jailPoint=" + Jail.JailPoint);
		}

		/// <summary>After .jail: where it is, whether a move out lands in jail again, a login, and a monster's interest.</summary>
		[SteamFunction]
		public static void TestJailReport(Character self) {
			Point4D jail = Jail.JailPoint;
			bool inJail = Jail.IsJailed(self) && (self.M == jail.M) && (Point2D.GetSimpleDistance(self, jail) <= 2);
			self.Go(before);
			bool goRedirected = (self.M == jail.M) && (Point2D.GetSimpleDistance(self, jail) <= 2);
			self.P(before.X, before.Y, before.Z, before.M);
			Jail.OnLogin(self);
			bool loginRedirected = (self.M == jail.M) && (Point2D.GetSimpleDistance(self, jail) <= 2);
			NPC wolf = (NPC) ((NPCDef) ThingDef.GetByDefname("c_wolf")).Create(self.X + 1, self.Y, self.Z, self.M);
			wolf.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			wolf.AggroMode = AggroMode.Hostile;
			self.Account.PLevel = 1;
			bool ignored = !wolf.ShouldAggroOn(self);
			self.Account.PLevel = 7;
			wolf.Delete();
			Console.WriteLine("JAIL_REPORT: inJail=" + inJail + " goRedirected=" + goRedirected + " loginRedirected=" + loginRedirected + " monstersIgnore=" + ignored);
		}

		private static Item kept;
		private static NPC guard;

		/// <summary>A sword in the pack, a level-0 account, then the GM's .lagr on itself.</summary>
		[SteamFunction]
		public static void TestCampReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Camp.SetLevel(self.Account, false, 0);
			Camp.SetLevel(self.Account, true, 0);
			self.Name = "Camp Tester";
			self.SetRealSkillValue((int) SkillName.Lumberjacking, 777);
			((Player) self).Home = new Point4D(self.X, self.Y, self.Z, self.M);
			kept = (Item) ThingDef.GetByDefname("i_sword_viking").Create(self.Backpack);
			Console.WriteLine("CAMP_READY: uid=" + self.Uid);
		}

		/// <summary>After .lagr: renamed, in the camp, Erebor's level 1 (Lumberjacking 20.0, 10 coins), the sword banked,
		/// the hatchet given; then the guard is placed beside the convict.</summary>
		[SteamFunction]
		public static void TestCampReport(Character self) {
			bool inArea = Camp.IsInCampArea(self) && (self.M == 1);
			Console.WriteLine("CAMP_REPORT: name=" + self.Name + " inCamp=" + Camp.IsInCamp(self) + " inArea=" + inArea +
				" level=" + Camp.Level(self.Account, false) + " lumber=" + self.GetSkill(SkillName.Lumberjacking) + " coinsOwed=" + (Camp.Sentence(self) / Camp.CoinValue) +
				" swordBanked=" + (kept.TopObj() == self && kept.Cont == self.Bank) + " hatchet=" + (Camp.Count(self, "i_hatchet") + ((self.FindLayer(LayerNames.Hand1) ?? self.FindLayer(LayerNames.Hand2)) is Item hand && hand.Def == ThingDef.GetByDefname("i_hatchet") ? 1 : 0)));
			guard = (NPC) ThingDef.GetByDefname("c_h_strazce_lagru").Create(self.X + 1, self.Y, self.Z, self.M);
			guard.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
		}

		/// <summary>Too much to carry stops a chop and takes a log (Erebor's 250 stones; a camp log is 1, nastaveni_cm_weight_i_log_lager=10);
		/// then 200 camp logs, enough for two exchanges.</summary>
		[SteamFunction]
		public static void TestCampLogs(Character self) {
			self.Str = 100;
			Item heavy = (Item) ThingDef.GetByDefname("i_ingot_iron").Create(self.Backpack);
			heavy.Amount = 5000;
			ThingDef.GetByDefname("i_log_lager").Create(self.Backpack);
			bool overloadedStops = !Camp.AllowsChop(self) && (Camp.Count(self, "i_log_lager") == 0);
			heavy.Delete();
			Item logs = (Item) ThingDef.GetByDefname("i_log_lager").Create(self.Backpack);
			logs.Amount = 200;
			Console.WriteLine("CAMP_LOGS: overloadedStops=" + overloadedStops + " free=" + Camp.AllowsChop(self));
		}

		/// <summary>The rest of the coins, as if chopped and exchanged.</summary>
		[SteamFunction]
		public static void TestCampGrantCoins(Character self) {
			Item coins = (Item) ThingDef.GetByDefname("i_gold_lagr").Create(self.Backpack);
			coins.Amount = (ushort) (Camp.Sentence(self) / Camp.CoinValue);
			Console.WriteLine("CAMP_GRANTED: coins=" + Camp.Count(self, "i_gold_lagr"));
		}

		[SteamFunction]
		public static void TestCampCoins(Character self) {
			Console.WriteLine("CAMP_COINS: coins=" + Camp.Count(self, "i_gold_lagr") + " logs=" + Camp.Count(self, "i_log_lager") + " owed=" + (Camp.Sentence(self) / Camp.CoinValue));
		}

		/// <summary>After propustit: everything back but the banked sword, home; the level stays.</summary>
		[SteamFunction]
		public static void TestCampReleased(Character self) {
			Point4D home = ((Player) self).Home;
			Console.WriteLine("CAMP_RELEASED: name=" + self.Name + " inCamp=" + Camp.IsInCamp(self) + " convict=" + Camp.IsConvict(self) + " lumber=" + self.GetSkill(SkillName.Lumberjacking) +
				" home=" + ((self.M == home.M) && (Point2D.GetSimpleDistance(self, home) <= 1)) + " coins=" + Camp.Count(self, "i_gold_lagr") + " level=" + Camp.Level(self.Account, false));
			if (guard != null) {
				guard.Delete();
			}
		}

		/// <summary>Past level 7: a recidivist with Lumberjacking 0, nothing to earn.</summary>
		[SteamFunction]
		public static void TestCampRecidivist(Character self) {
			Camp.SetLevel(self.Account, false, 7);
			Camp.Send(self, false);
			Console.WriteLine("CAMP_RECIDIVIST: level=" + Camp.Level(self.Account, false) + " lumber=" + self.GetSkill(SkillName.Lumberjacking) + " owed=" + Camp.Sentence(self) + " recidivist=" + Camp.IsRecidivist(self));
			Camp.Release(self);
			Camp.SetLevel(self.Account, false, 0);
		}

		[SteamFunction]
		public static void TestCampLevels(Character self) {
			Console.WriteLine("CAMP_LEVELS: level=" + Camp.Level(self.Account, false) + " afk=" + Camp.Level(self.Account, true));
		}

		/// <summary>#1107 - a clean slate for the AFK check, a 2 s warning.</summary>
		[SteamFunction]
		public static void TestAfkReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			if (Camp.IsInCamp(self)) {
				Camp.Release(self);
			}
			Camp.SetLevel(self.Account, true, 0);
			while (AfkCheck.Failures(self) > 0) {
				AfkCheck.ForgiveFailure(self);
			}
			AfkSettings.instance.countdownSeconds = 2;
			Console.WriteLine("AFK_READY: uid=" + self.Uid);
		}

		[SteamFunction]
		public static void TestAfkState(Character self) {
			string letters = (self.GetTimer(AfkCheckTimer.Key) as AfkCheckTimer)?.letters ?? "";
			int dots = 0;
			foreach (char letter in letters) {
				dots += D_AfkCheck.Points(letter).Count;
			}
			Console.WriteLine("AFK_STATE: check=" + self.HasTimer(AfkCheckTimer.Key) + " countdown=" + self.HasTimer(AfkCountdownTimer.Key) + " letters=" + letters + " dots=" + dots +
				" fails=" + AfkCheck.Failures(self) + " inCamp=" + Camp.IsInCamp(self) + " afkLevel=" + Camp.Level(self.Account, true) + " owed=" + Camp.Sentence(self) +
				" logged=" + AfkLogBook.Lines.Count + " |");
		}

		[SteamFunction]
		public static void TestAfkRestore(Character self) {
			AfkSettings.instance.countdownSeconds = 15;
			if (Camp.IsInCamp(self)) {
				Camp.Release(self);
			}
			Camp.SetLevel(self.Account, true, 0);
			Console.WriteLine("AFK_RESTORED");
		}

		/// <summary>#1107 step 4 - one camp line in an otherwise empty AFK log, Mining 50.0, AFK level 2.</summary>
		[SteamFunction]
		public static void TestAfkLogSetup(Character self) {
			AfkLogBook.Lines.Clear();
			self.SetRealSkillValue((int) SkillName.Mining, 500);
			Camp.SetLevel(self.Account, true, 2);
			AfkLogBook.Add(self, "makrar do lagru za Skill_Mining", "Skill_Mining");
			AfkLogBook.Add(self, "zacala AFK kontrola (15 s)", null);
			Console.WriteLine("AFKLOG_SETUP: index=0 count=" + AfkLogBook.Lines.Count);
		}

		[SteamFunction]
		public static void TestAfkLogState(Character self) {
			Console.WriteLine("AFKLOG_STATE: mining=" + self.GetSkill(SkillName.Mining) + " afkLevel=" + Camp.Level(self.Account, true) + " count=" + AfkLogBook.Lines.Count + " |");
		}

		[SteamFunction]
		public static void TestForgiveReport(Character self) {
			bool back = (self.M == before.M) && (Point2D.GetSimpleDistance(self, before) <= 1);
			Console.WriteLine("FORGIVE_REPORT: jailed=" + Jail.IsJailed(self) + " backWhereItWas=" + back);
		}
	}
}
