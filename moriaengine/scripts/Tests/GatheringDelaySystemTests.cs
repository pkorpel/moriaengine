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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>Server-side helpers for GatheringDelaySystem tests (#336). This repo's fast
	/// unit-test tier (Tests.csproj) doesn't reference the Scripts assembly at all - every
	/// scripts-level check in this whole implementation pass has gone through the protocol
	/// tier instead, same precedent continued here.</summary>
	public static class GatheringDelaySystemTests {

		[SteamFunction]
		public static void TestReportDelayForSkillPercent(Character self, ScriptArgs sa) {
			double skillPercent = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			double delay = GatheringDelaySystem.GetDelaySecondsForSkillPercent(skillPercent);
			Console.WriteLine("DELAY_FOR_PERCENT: skillPercent=" + skillPercent + " delay=" + delay);
		}

		/// <summary>Speeds up Mining's real anti-macro delay (#336's own 20-second base) for
		/// tests that need a stroke to actually complete quickly - same established
		/// SpeedUpSweepForTest-style test-hook precedent used elsewhere in this codebase for a
		/// real timer that's correctly slow in production but too slow for a test to wait out.</summary>
		[SteamFunction]
		public static void TestSpeedUpGatheringDelayForTest(Character self) {
			GatheringDelaySystem.BaseDelaySeconds = 0.1;
			Console.WriteLine("GATHERING_DELAY_SPED_UP: baseDelaySeconds=" + GatheringDelaySystem.BaseDelaySeconds);
		}

		[SteamFunction]
		public static void TestReportMiningDelayForChar(Character self) {
			double delay = GatheringDelaySystem.GetDelaySeconds(self, SkillName.Mining);
			Console.WriteLine("MINING_DELAY_FOR_CHAR: skillValue=" + self.GetRealSkillValue(SkillName.Mining) + " delay=" + delay);
		}

		/// <summary>#424 - same wrapper-extraction check as TestReportMiningDelayForChar, for
		/// Lumberjacking's own On_Start wiring.</summary>
		[SteamFunction]
		public static void TestReportLumberjackingDelayForChar(Character self) {
			double delay = GatheringDelaySystem.GetDelaySeconds(self, SkillName.Lumberjacking);
			Console.WriteLine("LUMBERJACKING_DELAY_FOR_CHAR: skillValue=" + self.GetRealSkillValue(SkillName.Lumberjacking) + " delay=" + delay);
		}
	}
}
