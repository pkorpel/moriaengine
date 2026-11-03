/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#839 - server-side helpers for Tests/Protocol/QuestJournalTests.cs.</summary>
	public static class QuestJournalTests {
		/// <summary>Args (one string): "rank stage" - my rank and Grandmaster stage, without any scroll.</summary>
		[SteamFunction]
		public static void TestJournalSet(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] args = sa.Args.Trim().Trim('"').Split(' ');
			player.SkillRank = (SkillRank) int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
			GrandmasterQuest.SetStage(player, int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture));
			Console.WriteLine("JOURNAL_SET: done");
		}

		/// <summary>The Grandmaster quest moves on to the given stage, the way the quest does it.</summary>
		[SteamFunction]
		public static void TestJournalAdvance(Character self, ScriptArgs sa) {
			GrandmasterQuest.Advance((Player) self, int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture));
			Console.WriteLine("JOURNAL_ADVANCED: done");
		}

		/// <summary>#1090 - two +25%/1h boosters and one +50%/1h, in the order a player would have got them.</summary>
		[SteamFunction]
		public static void TestBoostersGive(Character self) {
			Player player = (Player) self;
			player.DeleteTimer(BoosterTimer.Key);
			player.RemoveTag(TagKey.Acquire("_boosters_"));
			Boosters.Add(player, 0);
			Boosters.Add(player, 3);
			Boosters.Add(player, 0);
			Console.WriteLine("BOOSTERS_GIVEN: rows=" + Boosters.Stored(player).Count);
		}

		/// <summary>After the journal started the first row: the bonus on a gain and not on a loss, no second one while it
		/// runs, the rows left, and that a logout pauses it with its time and a login resumes it.</summary>
		[SteamFunction]
		public static void TestBoostersReport(Character self) {
			Player player = (Player) self;
			BoosterTimer active = Boosters.Active(player);
			long before = player.Experience;
			int level = player.Level;
			LevelingSystem.AddExperience(player, 4);
			bool bonus = (player.Level == level) && (player.Experience == before + 5); //4 + 25% of 4, rounded down: 5
			bool noSecond = !Boosters.Use(player, 0);
			List<KeyValuePair<int, int>> left = Boosters.Stored(player);
			double due = (active == null) ? 0 : active.DueInSeconds;
			Boosters.Pause(player);
			bool paused = Boosters.Active(player) == null;
			Boosters.Resume(player);
			BoosterTimer resumed = Boosters.Active(player);
			bool sameTime = (resumed != null) && (Math.Abs(resumed.DueInSeconds - due) < 5);
			Console.WriteLine("BOOSTERS_REPORT: kind=" + ((active == null) ? -1 : active.kind) + " due=" + ((due > 3500) && (due <= 3600)) + " bonus=" + bonus +
				" noSecond=" + noSecond + " left=" + string.Join(",", left.ConvertAll(row => row.Key + "x" + row.Value)) +
				" paused=" + paused + " resumed=" + sameTime + " end");
			player.DeleteTimer(BoosterTimer.Key);
		}
	}
}
