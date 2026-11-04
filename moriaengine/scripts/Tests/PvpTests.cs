using System;
using System.Collections.Generic;
using SteamEngine.Regions;

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
