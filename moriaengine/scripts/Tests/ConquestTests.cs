using System;
using System.Globalization;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1116 - server side of Tests/Protocol/ConquestTests.cs.</summary>
	public static class ConquestTests {
		private static WarDeclarator stone;

		private static Player Named(string name) {
			foreach (AbstractCharacter ch in GameServer.GetAllPlayers()) {
				if ((ch is Player player) && player.Name.EndsWith(name, StringComparison.Ordinal)) { //"Kapitan " may lead
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

		private static FortressPalantir palantir;
		private static FortCashBox cashBox;

		/// <summary>#1116 step 10 - me a Gondor attacker with the right, "Conquest Defender" a Mordor player beside me, Mordor_1's
		/// palantir and cash box here, its last capture 1000 s before noon on 4.11, and Gondor's war on it running since 20:00 on 5.11.</summary>
		[SteamFunction]
		public static void TestPalantirReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			Conquest.Reset();
			Palantirs.ticksByHand = true;
			Conquest.clock = new DateTime(2026, 11, 4, 12, 0, 0);
			MakeAttacker((Player) self);
			foreach (AbstractItem item in new System.Collections.Generic.List<AbstractItem>(self.Backpack)) {
				item.Delete();
			}
			Player defender = Named("Conquest Defender");
			defender.Account.PLevel = 1;
			defender.Realm = Realm.Mordor;
			defender.Go(self.X, self.Y + 1, self.Z, self.M);
			if ((palantir != null) && !palantir.IsDeleted) {
				palantir.Delete();
			}
			if ((cashBox != null) && !cashBox.IsDeleted) {
				cashBox.Delete();
			}
			palantir = (FortressPalantir) ThingDef.GetByDefname("i_fortress_palantir").Create(self.X + 1, self.Y, self.Z, self.M);
			palantir.SetFort("Mordor_1");
			cashBox = (FortCashBox) ThingDef.GetByDefname("i_fort_cashbox").Create(self.X - 1, self.Y, self.Z, self.M);
			cashBox.Fort = "Mordor_1";
			Conquest.Keep("Mordor_1").lastConquered = Conquest.clock.Value.AddSeconds(-1000);
			WarFront front = Conquest.Front(Realm.Gondor);
			Conquest.Declare((Player) self, front, 5, 11, 20, out bool done, out _);
			Conquest.clock = new DateTime(2026, 11, 5, 20, 0, 0);
			Conquest.StartWar(front);
			Console.WriteLine("PALANTIR_READY: palantir=" + palantir.FlaggedUid + " box=" + cashBox.FlaggedUid + " declared=" + done + " name=" + palantir.Name + " |");
		}

		/// <summary>Where ConquestSetup would put each keep's palantir and cash box.</summary>
		[SteamFunction]
		public static void TestConquestSpots(Character self) {
			string line = "";
			foreach (string area in new[] { "a_Osgiliath_zapadni_breh", "a_Vychodni_Marka", "a_Osgiliath_vychodni_breh", "a_Minas_Morgul" }) {
				SteamEngine.Regions.FlaggedRegion region = SteamEngine.Regions.StaticRegion.GetByDefname(area) as SteamEngine.Regions.FlaggedRegion;
				line += " " + area + "=" + ((region != null) && (ConquestSetup.SpotIn(region, self, 0) != null)) + "," + ((region != null) && (ConquestSetup.SpotIn(region, self, 1) != null));
			}
			Console.WriteLine("CONQUEST_SPOTS:" + line + " |");
		}

		[SteamFunction]
		public static void TestPalantirTicks(Character self, ScriptArgs sa) {
			int n = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			for (int i = 0; i < n; i++) {
				Palantirs.Tick(palantir);
			}
			Console.WriteLine("PALANTIR_TICKS");
		}

		[SteamFunction]
		public static void TestPalantirHold(Character self, ScriptArgs sa) {
			Conquest.Keep("Mordor_1").hold = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture);
			Console.WriteLine("PALANTIR_HOLD");
		}

		[SteamFunction]
		public static void TestPalantirState(Character self) {
			KeepState keep = Conquest.Keep("Mordor_1");
			int cheque = 0;
			foreach (AbstractItem item in self.Backpack) {
				if (item is CashiersItem check) {
					cheque += check.CheckValue;
				}
			}
			Console.WriteLine("PALANTIR_STATE: owner=" + keep.owner + " breached=" + keep.breached + " conqueror=" + keep.conqueror + " hold=" + keep.hold +
				" color=" + palantir.Color + " gondorDeclared=" + (Conquest.Front(Realm.Gondor).declaredOn ?? "none") +
				" gondorConquered=" + Conquest.Front(Realm.Gondor).conquered + " mordorConquered=" + Conquest.Front(Realm.Mordor).conquered +
				" right=" + Conquest.HasRight(self) + " cash=" + keep.cashGold + " collector=" + ((keep.cashCollector == null) ? "none" : keep.cashCollector.Name) +
				" cheque=" + cheque + " |");
		}

		private static CaptainStone captainStone;

		/// <summary>#1116 step 11 - me a GM and "Conquest Captain" a Gondor player, both in Gondor_1's keep beside its captain stone.</summary>
		[SteamFunction]
		public static void TestCaptainReady(Character self) {
			Conquest.Reset();
			SteamEngine.Regions.FlaggedRegion area = (SteamEngine.Regions.FlaggedRegion) SteamEngine.Regions.StaticRegion.GetByDefname("a_Osgiliath_zapadni_breh");
			int[] spot = ConquestSetup.SpotIn(area, self, 3);
			self.Go(spot[0], spot[1], spot[2], area.P.M);
			self.Account.PLevel = 7;
			if ((captainStone != null) && !captainStone.IsDeleted) {
				captainStone.Delete();
			}
			captainStone = (CaptainStone) ThingDef.GetByDefname("i_captainstone").Create(self.X + 1, self.Y, self.Z, self.M);
			captainStone.SetFort("Gondor_1");
			Player captain = Named("Conquest Captain");
			captain.Account.PLevel = 1;
			captain.Realm = Realm.Gondor;
			KeepCaptains.DismissFrom(captain);
			captain.Go(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("CAPTAIN_READY: stone=" + captainStone.FlaggedUid + " captain=" + captain.FlaggedUid + " inKeep=" + KeepCaptains.InKeep(self, "Gondor_1") + " |");
		}

		[SteamFunction]
		public static void TestCaptainState(Character self) {
			KeepState keep = Conquest.Keep("Gondor_1");
			GuardForce force = KeepCaptains.ForceOf(keep);
			string guards = "";
			foreach (HiredGuard guard in force.guards) {
				guards += guard.defname + (guard.IsStanding ? "(standing)" : "") + ",";
			}
			Player captain = Named("Conquest Captain");
			Console.WriteLine("CAPTAIN_STATE: captain=" + ((keep.captain == null) ? "none" : keep.captain.Name) + " isCaptain=" + KeepCaptains.IsCaptain(captain) +
				" captainName=" + captain.Name + " owner=" + keep.owner + " guards=" + guards + " alive=" + KeepCaptains.AnyGuardAlive("Gondor_1") + " |");
		}

		/// <summary>The keep's first guard posted "near" (2 tiles from the stone) or "far" (15 tiles), as its captain.</summary>
		[SteamFunction]
		public static void TestCaptainPost(Character self, ScriptArgs sa) {
			Player captain = Named("Conquest Captain");
			GuardForce force = KeepCaptains.ForceOf(Conquest.Keep("Gondor_1"));
			int dx = (sa.Args.Trim().Trim('"') == "far") ? 15 : 2;
			bool posted = force.TryPost(captain, force.guards[0], new Point3D(captainStone.X + dx, captainStone.Y, captainStone.Z));
			Console.WriteLine("CAPTAIN_POST: posted=" + posted + " |");
		}

		/// <summary>Mordor takes Gondor_1 with me as its war lord, a Mordor player.</summary>
		[SteamFunction]
		public static void TestCaptainCapture(Character self) {
			self.Account.PLevel = 1;
			self.Realm = Realm.Mordor;
			KeepState keep = Conquest.Keep("Gondor_1");
			keep.owner = Realm.Mordor;
			KeepCaptains.OnCapture("Gondor_1", (Player) self);
			Console.WriteLine("CAPTAIN_CAPTURE: lord=" + ((keep.captain == null) ? "none" : keep.captain.Name) + " |");
			KeepCaptains.DismissFrom(self);
			self.Account.PLevel = 7;
		}

		private static NPC keepGuard;

		/// <summary>#1116 step 12 - "Conquest Ban Captain" the captain of Gondor_1, with a guard posted, "Conquest Outlaw" a Gondor
		/// player beside them, all in the keep.</summary>
		[SteamFunction]
		public static void TestBanReady(Character self) {
			Conquest.Reset();
			SteamEngine.Regions.FlaggedRegion area = (SteamEngine.Regions.FlaggedRegion) SteamEngine.Regions.StaticRegion.GetByDefname("a_Osgiliath_zapadni_breh");
			int[] spot = ConquestSetup.SpotIn(area, self, 3);
			Player captain = (Player) self;
			KeepCaptains.DismissFrom(captain);
			captain.Go(spot[0], spot[1], spot[2], area.P.M);
			captain.Account.PLevel = 1;
			captain.Realm = Realm.Gondor;
			if ((captainStone != null) && !captainStone.IsDeleted) {
				captainStone.Delete();
			}
			captainStone = (CaptainStone) ThingDef.GetByDefname("i_captainstone").Create(self.X + 1, self.Y, self.Z, self.M);
			captainStone.SetFort("Gondor_1");
			KeepState keep = Conquest.Keep("Gondor_1");
			KeepCaptains.Appoint(captain, keep, captain);
			GuardForce force = KeepCaptains.ForceOf(keep);
			force.Hire(captain, force.HireableDefnames()[0]);
			force.TryPost(captain, force.guards[0], new Point3D(captainStone.X + 2, captainStone.Y, captainStone.Z));
			keepGuard = force.guards[0].npc;
			keepGuard.DeleteTimer(SteamEngine.Timers.TimerKey.Acquire("_npcAiTimer_"));
			Player outlaw = Named("Conquest Outlaw");
			outlaw.Account.PLevel = 1;
			outlaw.Realm = Realm.Gondor;
			outlaw.Go(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("BAN_READY: outlaw=" + outlaw.FlaggedUid + " captain=" + KeepCaptains.IsCaptain(captain) + " guard=" + (keepGuard != null) + " |");
		}

		[SteamFunction]
		public static void TestBanState(Character self) {
			KeepState keep = Conquest.Keep("Gondor_1");
			Player outlaw = Named("Conquest Outlaw");
			Console.WriteLine("BAN_STATE: bans=" + keep.bans.Count + " guardAttacks=" + GuardSpeech.AttacksOnSight(keepGuard, outlaw) + " |");
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
