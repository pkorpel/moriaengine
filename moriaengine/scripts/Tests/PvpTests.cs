using System;
using System.Collections.Generic;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1116 - server side of Tests/Protocol/PvpTests.cs.</summary>
	public static class PvpTests {
		private static FlaggedRegion Area(string defname) {
			return (FlaggedRegion) StaticRegion.GetByDefname(defname);
		}

		private static Player victim;

		private static void MakeEvaluable(Player player, Realm realm) {
			player.Realm = realm;
			player.Profession = Professions.Warrior;
			((Player) player).SkillRank = SkillRank.Grandmaster;
		}

		/// <summary>#1116 step 2 - this one (the killer, made a Mordor player) and the "Pvp Victim" (Gondor) side by side.</summary>
		[SteamFunction]
		public static void TestLeagueReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			victim = null;
			foreach (AbstractCharacter ch in SteamEngine.Networking.GameServer.GetAllPlayers()) {
				if ((ch is Player player) && (player.Name == "Pvp Victim")) {
					victim = player;
				}
			}
			self.Account.PLevel = 1;
			MakeEvaluable((Player) self, Realm.Mordor);
			MakeEvaluable(victim, Realm.Gondor);
			victim.Go(self.X + 1, self.Y, self.Z, self.M);
			PvpLeague.Reset();
			Console.WriteLine("LEAGUE_READY: victim=" + (victim != null));
		}

		/// <summary>A real death after a fight pays the killer 3; then each rule that makes a death not count.</summary>
		[SteamFunction]
		public static void TestLeagueKill(Character self) {
			FightMemory.Record(victim, self);
			FightMemory.Record(self, victim);
			victim.CauseDeath(self);
			int afterKill = PvpLeague.PointsOf(self);
			victim.Resurrect();
			string rules = "";
			FightMemory.Record(victim, self);
			PvpLeague.OnDeath(victim, self, true);
			rules += " recentlyDead=" + PvpLeague.PointsOf(self);
			victim.Realm = Realm.Mordor;
			PvpLeague.OnDeath(victim, self, false);
			rules += " sameRealm=" + PvpLeague.PointsOf(self);
			victim.Realm = Realm.Gondor;
			victim.Profession = Professions.Craftsman;
			PvpLeague.OnDeath(victim, self, false);
			rules += " craftsman=" + PvpLeague.PointsOf(self);
			victim.Profession = Professions.Warrior;
			victim.SkillRank = SkillRank.Master;
			PvpLeague.OnDeath(victim, self, false);
			rules += " rank2=" + PvpLeague.PointsOf(self);
			victim.SkillRank = SkillRank.Grandmaster;
			Point4D before = new Point4D(victim.X, victim.Y, victim.Z, victim.M);
			Point4D arena = ((StaticRegion) StaticRegion.GetByDefname("a_Mordorska_bojova_arena")).P;
			victim.Go(arena);
			PvpLeague.OnDeath(victim, self, false);
			rules += " arena=" + PvpLeague.PointsOf(self);
			victim.Go(before);
			self.Account.PLevel = 7;
			PvpLeague.OnDeath(victim, self, false);
			rules += " gm=" + PvpLeague.PointsOf(self);
			self.Account.PLevel = 1;
			PvpLeague.OnDeath(victim, self, false);
			rules += " again=" + PvpLeague.PointsOf(self);
			self.Account.PLevel = 7;
			Console.WriteLine("LEAGUE_KILL: kill=" + afterKill + rules + " |");
		}

		/// <summary>The board: the top places and ties with the last; then a season roll.</summary>
		[SteamFunction]
		public static void TestLeagueBoard(Character self) {
			PvpLeague.Reset();
			List<NPC> npcs = new List<NPC>();
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_wolf");
			int stored = PvpLeagueSettings.instance.playersStored;
			for (int i = 0; i < stored + 3; i++) {
				NPC npc = (NPC) def.Create(self.X, self.Y + 2, self.Z, self.M);
				npc.DeleteTimer(SteamEngine.Timers.TimerKey.Acquire("_npcAiTimer_"));
				npcs.Add(npc);
				PvpLeague.AddPoints(npc, (i < stored - 2) ? (100 - i) : 5); //five tied on 5, one of whom then climbs to the top
			}
			PvpLeague.AddPoints(npcs[stored + 2], 200); //the last to come goes to the top
			List<PvpLeagueEntry> board = PvpLeague.Board;
			int seasonBefore = PvpLeague.Season;
			bool topIsNewcomer = board[0].player == npcs[stored + 2];
			PvpLeague.EndSeasonYesterday();
			PvpLeague.AddPoints(npcs[0], 1);
			string line = "LEAGUE_BOARD: shown=" + board.Count + " topIsNewcomer=" + topIsNewcomer + " lastTie=" + board[board.Count - 1].points +
				" season=" + seasonBefore + "->" + PvpLeague.Season + " afterRoll=" + PvpLeague.PointsOf(npcs[0]) + "," + PvpLeague.PointsOf(npcs[1]) +
				" endsDay=" + PvpLeague.SeasonEnd.Day;
			foreach (NPC npc in npcs) {
				npc.Delete();
			}
			Console.WriteLine(line + " |");
		}

		/// <summary>#1116 step 3 - 7 points of one's own, for the journal's PvP side.</summary>
		[SteamFunction]
		public static void TestLeagueOwnPoints(Character self) {
			self.Account.PLevel = 1;
			PvpLeague.AddPoints(self, 7);
			self.Account.PLevel = 7;
			Console.WriteLine("LEAGUE_OWN: points=" + PvpLeague.PointsOf(self) + " season=" + PvpLeague.Season + " end=" + PvpLeague.SeasonEnd.ToString("d.M. yyyy", System.Globalization.CultureInfo.InvariantCulture));
		}

		/// <summary>#1133 - a season's end: me 1st of 12 gets a 2 h booster (the eleven wolves below me hold places, and get nothing
		/// as they aren't players).</summary>
		[SteamFunction]
		public static void TestLeagueSeasonReward(Character self) {
			Player player = (Player) self;
			PvpLeague.Reset();
			Boosters.Clear(player); //#1471
			self.Account.PLevel = 1;
			PvpLeague.AddPoints(player, 100);
			self.Account.PLevel = 7;
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_wolf");
			List<NPC> npcs = new List<NPC>();
			for (int i = 0; i < 11; i++) {
				NPC npc = (NPC) def.Create(self.X, self.Y + 2, self.Z, self.M);
				npc.DeleteTimer(SteamEngine.Timers.TimerKey.Acquire("_npcAiTimer_"));
				npcs.Add(npc);
				PvpLeague.AddPoints(npc, 50 - i);
			}
			int season = PvpLeague.Season;
			PvpLeague.EndSeasonYesterday();
			PvpLeague.CheckExpiration();
			string boosters = "";
			foreach (KeyValuePair<int, int> row in Boosters.Stored(player)) {
				boosters += row.Key + "x" + row.Value + ",";
			}
			foreach (NPC npc in npcs) {
				npc.Delete();
			}
			Console.WriteLine("LEAGUE_REWARD: season=" + season + "->" + PvpLeague.Season + " boosters=" + boosters + " |");
		}

		/// <summary>#1116 step 4 - the RP rank's bonus on a gain of 1000, at rank 0, 7 and -3 (Erebor's table).</summary>
		[SteamFunction]
		public static void TestRpRankGain(Character self) {
			Player player = (Player) self;
			string line = "";
			foreach (int rank in new[] { 0, 7, -3 }) {
				RpRanks.Set(player, rank, "test");
				int before = player.Experience + (player.Level * 1000000);
				LevelingSystem.AddExperience(player, 1000);
				line += " rank" + rank + "=" + RpRanks.Bonus(player, 1000);
			}
			RpRanks.Set(player, 0, "back");
			Console.WriteLine("RP_GAIN:" + line + " refused=" + (RpRanks.Set(player, 8, "x") != null) + " log=" + RpRanks.Log(player).Count + " |");
		}

		[SteamFunction]
		public static void TestRpRankState(Character self) {
			List<string> log = RpRanks.Log(self);
			Console.WriteLine("RP_STATE: rank=" + RpRanks.Of(self) + " last=" + ((log.Count > 0) ? log[log.Count - 1] : "") + " |");
		}

		private static NPC agent;

		/// <summary>#1116 step 5 - a Gondor player with no murders, karma 50, fame 100, 200 gold, beside the arena agent.</summary>
		[SteamFunction]
		public static void TestArenaReady(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			player.Home = new Point4D(self.X, self.Y, self.Z, self.M);
			self.Account.PLevel = 1;
			player.Realm = Realm.Gondor;
			Notoriety.SetMurderCount(player, 0);
			player.Karma = 50;
			player.Fame = 100;
			foreach (AbstractItem item in new List<AbstractItem>(player.Backpack)) {
				item.Delete();
			}
			((Item) ThingDef.GetByDefname("i_gold").Create(player.Backpack)).Amount = 200;
			agent = (NPC) ThingDef.GetByDefname("c_arena_obchodnik").Create(self.X + 1, self.Y, self.Z, self.M);
			agent.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			Console.WriteLine("ARENA_READY");
		}

		/// <summary>Skip the rest of the 30 s countdown.</summary>
		[SteamFunction]
		public static void TestArenaHurry(Character self) {
			ArenaCountdown timer = self.GetTimer(ArenaCountdown.Key) as ArenaCountdown;
			if (timer != null) {
				timer.left = 0;
			}
			Console.WriteLine("ARENA_HURRY: counting=" + (timer != null));
		}

		[SteamFunction]
		public static void TestArenaState(Character self) {
			Player player = (Player) self;
			FlaggedRegion area = RegionEventSystem.AreaOf(self.Region);
			Point4D home = player.Home;
			Console.WriteLine("ARENA_STATE: in=" + Arena.IsIn(player) + " area=" + ((area == null) ? "" : area.Defname) + " gold=" + GoldUtils.CountGoldIn(player.Backpack) +
				" murders=" + Notoriety.GetMurderCount(player) + " karma=" + player.Karma + " fame=" + player.Fame + " realm=" + player.Realm +
				" atHome=" + ((home != null) && (self.M == home.M) && (Point2D.GetSimpleDistance(self, home) <= 1)) +
				" recallRefused=" + Arena.RefusesSpell(self, SpellDef.GetById(32)) + " |");
		}

		/// <summary>What the arena may change: murders, karma, fame.</summary>
		[SteamFunction]
		public static void TestArenaFight(Character self) {
			Notoriety.SetMurderCount((Player) self, 3);
			self.Karma = -100;
			self.Fame = 0;
			Console.WriteLine("ARENA_FIGHT");
		}

		/// <summary>Out through the exit gate, or moved out of the area.</summary>
		[SteamFunction]
		public static void TestArenaExit(Character self, ScriptArgs sa) {
			string how = (sa == null) ? "" : sa.Args.Trim().Trim('"');
			if (how == "gate") {
				Item gate = (Item) ThingDef.GetByDefname(Arena.ExitGateDef).Create(self.X, self.Y, self.Z, self.M);
				Arena.OnStep(self, gate);
				gate.Delete();
			} else {
				self.Go(((Player) self).Home);
			}
			Console.WriteLine("ARENA_EXIT");
		}

		[SteamFunction]
		public static void TestArenaEnterDirectly(Character self) {
			Arena.Enter((Player) self);
			self.Go(Arena.ArenaFor(Realm.Gondor).P);
			Console.WriteLine("ARENA_ENTERED");
		}

		private static Character portZoneHorse;

		/// <summary>15 tiles east of Arena Mordor's spawn point, as a GM, for .PortZonePoint.</summary>
		[SteamFunction]
		public static void TestPortZoneGoAside(Character self) {
			Point4D spot = Area("a_Arena_Mordor").P;
			self.Go(spot.X + 15, spot.Y, spot.Z, spot.M);
			self.Account.PLevel = 7;
			Console.WriteLine("PORTZONE_ASIDE: at=" + self.X + "," + self.Y + "," + self.Z + "," + self.M + " |");
		}

		/// <summary>#1116 step 6 - a Mordor player with 5 gold dies at Arena Mordor's spawn point, riding a pet horse or
		/// ("far") with the horse 30 tiles off.</summary>
		[SteamFunction]
		public static void TestPortZoneDie(Character self, ScriptArgs sa) {
			string how = (sa == null) ? "" : sa.Args.Trim().Trim('"');
			Player player = (Player) self;
			player.Go(Area("a_Arena_Mordor").P);
			self.Account.PLevel = 1;
			player.Realm = Realm.Mordor;
			foreach (AbstractItem item in new List<AbstractItem>(player.Backpack)) {
				item.Delete();
			}
			((Item) ThingDef.GetByDefname("i_gold").Create(player.Backpack)).Amount = 5;
			if (portZoneHorse != null) {
				portZoneHorse.Delete();
			}
			portZoneHorse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
			portZoneHorse.ControlMaster = self;
			self.Mount = portZoneHorse;
			if (how == "far") {
				self.Dismount();
				portZoneHorse.Go(self.X + 30, self.Y, self.Z, self.M);
			}
			player.CauseDeath(null);
			Console.WriteLine("PORTZONE_DIED: dead=" + player.Flag_Dead + " porter=" + (self.GetTimer(PortZoneTimer.Key) != null) + " portZone=" + PortZone.IsPortZone(RegionEventSystem.AreaOf(self.Region)) + " |");
		}

		[SteamFunction]
		public static void TestPortZoneState(Character self) {
			Player player = (Player) self;
			self.Account.PLevel = 7;
			Console.WriteLine("PORTZONE_STATE: dead=" + player.Flag_Dead + " at=" + self.X + "," + self.Y + "," + self.Z + "," + self.M +
				" gold=" + GoldUtils.CountGoldIn(player.Backpack) + " riding=" + (self.Mount == portZoneHorse) +
				" horseFar=" + (Point2D.GetSimpleDistance(self, portZoneHorse) > PortZone.MountReach) + " |");
		}

		private static Item tournamentChest, tournamentDagger;
		private static PvpTournamentStone tournamentStone;

		/// <summary>#1133 - me a GM warrior with a dagger; a chest (a robe, 10 bandages, an iron katana) and the stone beside me.</summary>
		[SteamFunction]
		public static void TestTournamentReady(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			player.Account.PLevel = 7;
			player.Profession = Professions.Warrior;
			PvpTournament.NewTournament();
			foreach (AbstractItem item in new List<AbstractItem>(player.Backpack)) {
				item.Delete();
			}
			tournamentDagger = (Item) ThingDef.GetByDefname("i_dagger").Create(player.Backpack);
			tournamentChest = (Item) ThingDef.GetByDefname("i_backpack").Create(self.X + 1, self.Y, self.Z, self.M);
			ThingDef.GetByDefname("i_robe").Create(tournamentChest);
			((Item) ThingDef.GetByDefname("i_bandage").Create(tournamentChest)).Amount = 10;
			ThingDef.GetByDefname("i_katana_iron").Create(tournamentChest);
			tournamentStone = (PvpTournamentStone) ThingDef.GetByDefname("i_stone_pvp_turnaj").Create(self.X - 1, self.Y, self.Z, self.M);
			string key = PvpTournament.KeyOf(player);
			int button = (key.EndsWith("_zena", StringComparison.Ordinal) ? D_PvpTournamentChests.WomenFirst : D_PvpTournamentChests.MenFirst) + Array.IndexOf(PvpTournament.Classes, "war");
			Console.WriteLine("TOURNAMENT_READY: chest=" + tournamentChest.FlaggedUid + " stone=" + tournamentStone.FlaggedUid + " me=" + self.FlaggedUid +
				" chestButton=" + button + " slotButton=" + D_PvpTournamentTeams.ButtonOf(2, 1) + " |");
		}

		/// <summary>Back beside the stone as a plain player.</summary>
		[SteamFunction]
		public static void TestTournamentBack(Character self) {
			self.Account.PLevel = 1;
			self.Go(tournamentStone.X + 1, tournamentStone.Y, tournamentStone.Z, tournamentStone.M);
			Console.WriteLine("TOURNAMENT_BACK");
		}

		[SteamFunction]
		public static void TestTournamentState(Character self) {
			int robes = 0, robeColor = 0, bandages = 0, katanas = 0;
			foreach (AbstractItem item in self.Backpack) {
				string defname = item.Def.PrettyDefname;
				if (defname == "i_robe") {
					robes++;
					robeColor = item.Color;
				} else if (defname == "i_bandage") {
					bandages += item.Amount;
				} else if (defname == "i_katana_iron") {
					katanas++;
				}
			}
			Console.WriteLine("TOURNAMENT_STATE: team=" + PvpTournament.TeamOf(self) + " chestSet=" + (PvpTournament.ChestOf(PvpTournament.KeyOf((Player) self)) == tournamentChest) +
				" at=" + self.X + "," + self.Y + " robes=" + robes + " robeColor=" + robeColor.ToString("x", System.Globalization.CultureInfo.InvariantCulture) +
				" bandages=" + bandages + " katanas=" + katanas + " daggerInBank=" + (tournamentDagger.TopObj() == self && tournamentDagger.Cont != self.Backpack) + " |");
		}

		[SteamFunction]
		public static void TestTournamentNew(Character self) {
			PvpTournament.NewTournament();
			self.Account.PLevel = 7;
			Console.WriteLine("TOURNAMENT_NEW");
		}

		/// <summary>The converter's tag.arena and tag.bf, as the regions loaded them.</summary>
		[SteamFunction]
		public static void TestPvpRegionTags(Character self) {
			string arenas = "";
			foreach (string name in new[] { "a_Arena_Mordor", "a_Mordorska_bojova_arena", "a_Nova_Arena_Equip_Room", "a_Arena_Lamy", "a_Murdoc" }) {
				arenas += " " + name + "=" + Area(name).Flag_Arena;
			}
			string fronts = "";
			foreach (string name in new[] { "a_Pelenorska_pole", "a_Anorien", "a_Vychodni_breh", "a_Minas_Morgul", "a_Rhovanion" }) {
				fronts += " " + name + "=" + Area(name).Battlefield;
			}
			Console.WriteLine("PVP_REGIONS:" + arenas + fronts + " |");
		}
	}
}
