using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's PvP league (questy/pvpLeague.scp): rank points for killing players of the other realm, per
	/// season of PvpLeagueSettings.monthsInSeason calendar months. The killing blow pays 1 and everyone who fought the
	/// victim 2 (the killer is one of them, so a kill is worth 3), each only when PvpRules.IsEvaluable. GMs get nothing.
	/// The board keeps the top PvpLeagueSettings.playersStored and anyone tied with the last of them, in Erebor's order:
	/// a player moves up past everyone with fewer points, so among equals the earlier stays above.
	/// A season ends at the end of day 1 of the month monthsInSeason after it began (Erebor's isTimeDateExceeded is true
	/// only past the day), and the next point starts a new one; old points go. Erebor never wrote a season reward (a TODO),
	/// so there's none. Erebor kept this on a chest of season books; here it's saved with the world.</summary>
	[HasSavedMembers]
	public static class PvpLeague {
		[SavedMember]
		private static int season;

		[SavedMember]
		private static DateTime seasonEnd;

		[SavedMember]
		private static List<PvpLeagueEntry> standings = new List<PvpLeagueEntry>();

		internal const int KillingBlowPoints = 1, FightPoints = 2;

		public static int Season {
			get { return season; }
		}

		public static DateTime SeasonEnd {
			get { return seasonEnd; }
		}

		/// <summary>f_pvpLeague_checkExpiration: no season yet, or today is past its end, starts the next.</summary>
		public static void CheckExpiration() {
			if ((season == 0) || (DateTime.Now.Date > seasonEnd.Date)) {
				StartSeason();
			}
		}

		/// <summary>f_pvpLeague_initSeason + i_pvpLeague's @create: the end is day 1 of the month monthsInSeason on.</summary>
		private static void StartSeason() {
			season++;
			standings.Clear();
			DateTime now = DateTime.Now;
			seasonEnd = new DateTime(now.Year, now.Month, 1).AddMonths(PvpLeagueSettings.instance.monthsInSeason);
			Logger.WriteInfo(true, "PvP league season " + season + " starts, ends " + seasonEnd.ToString("d.M.yyyy", System.Globalization.CultureInfo.InvariantCulture));
		}

		/// <summary>This season's points, 0 for none.</summary>
		public static int PointsOf(Character player) {
			PvpLeagueEntry entry = standings.Find(e => e.player == player);
			return (entry == null) ? 0 : entry.points;
		}

		/// <summary>The board: the first playersStored, and anyone with the last one's points.</summary>
		public static List<PvpLeagueEntry> Board {
			get {
				List<PvpLeagueEntry> board = new List<PvpLeagueEntry>();
				int stored = PvpLeagueSettings.instance.playersStored;
				foreach (PvpLeagueEntry entry in standings) {
					if ((entry.player == null) || entry.player.IsDeleted) {
						continue;
					}
					if ((board.Count >= stored) && (entry.points < board[stored - 1].points)) {
						break;
					}
					board.Add(entry);
				}
				return board;
			}
		}

		/// <summary>f_pvpLeague_increaseRank + f_pvpLeague_storeRank.</summary>
		public static void AddPoints(Character player, int points) {
			if (player.IsGM) {
				return;
			}
			CheckExpiration();
			int index = standings.FindIndex(e => e.player == player);
			PvpLeagueEntry entry;
			if (index < 0) {
				entry = new PvpLeagueEntry { player = player };
				standings.Add(entry);
				index = standings.Count - 1;
			} else {
				entry = standings[index];
			}
			entry.points += points;
			while ((index > 0) && (standings[index - 1].points < entry.points)) {
				standings[index] = standings[index - 1];
				standings[index - 1] = entry;
				index--;
			}
		}

		/// <summary>e_allplayers' @finalBlow (+1) and f_countfight (+2 each fighter), from Player.On_Death. A death within
		/// 5 minutes of the last counts for nothing (i_checkstats_timer).</summary>
		public static void OnDeath(Player victim, Character killer, bool recentlyDead) {
			if (recentlyDead) {
				return;
			}
			foreach (Character fighter in LevelingSystem.FindFighters(victim, killer, false)) {
				if ((fighter is Player player) && (player != victim) && PvpRules.IsEvaluable(victim, player)) {
					AddPoints(player, (player == killer) ? (FightPoints + KillingBlowPoints) : FightPoints);
					ConquestRewards.GainPoint(player, FightPoints); //#1116 - f_pvpLeague_increaseRank's conquest part
				}
			}
		}

		/// <summary>For a test: back to no season.</summary>
		internal static void Reset() {
			season = 0;
			seasonEnd = default(DateTime);
			standings.Clear();
		}

		/// <summary>For a test: the season ended yesterday.</summary>
		internal static void EndSeasonYesterday() {
			seasonEnd = DateTime.Now.Date.AddDays(-1);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class PvpLeagueEntry {
		[SaveableData, CopyableData]
		public Character player;

		[SaveableData, CopyableData]
		public int points;

		[LoadingInitializer, DeepCopyImplementation]
		public PvpLeagueEntry() {
		}
	}

	/// <summary>#1116 - Erebor's def_pvp_league defnames, on .settings as "PvP League".</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class PvpLeagueSettings : SettingsMetaCategory {
		[SavedMember]
		public static PvpLeagueSettings instance = new PvpLeagueSettings();

		[LoadingInitializer]
		public PvpLeagueSettings() {
		}

		/// <summary>def_pvpLeague_playersStored: places on the board (ties with the last one stay too).</summary>
		[SaveableData]
		public int playersStored = 30;

		/// <summary>def_pvpLeague_monthsInSeason.</summary>
		[SaveableData]
		public int monthsInSeason = 2;
	}
}
