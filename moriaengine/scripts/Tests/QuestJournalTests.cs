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
	}
}
