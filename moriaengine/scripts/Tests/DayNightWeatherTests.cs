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

	/// <summary>
	/// V-8 (verification pass, #76): server-side helpers for day/night + weather scenarios
	/// driven by real protocol reads from Tests/Protocol/DayNightWeatherTests.cs. The real
	/// autonomous world clock only checks for a day/night flip every 60 real seconds and only
	/// rolls a new season with 1% probability per check - both too slow/non-deterministic to
	/// wait out directly, so these helpers shorten/re-anchor the clock (day/night scenario) or
	/// invoke the same real setter the clock itself uses (season scenario) instead.
	/// </summary>
	public static class DayNightWeatherTests {
		[SteamFunction]
		public static void TestSetDayLengthSeconds(Character self, ScriptArgs sa) {
			double seconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			LightAndWeather.DayLengthSeconds = seconds;
			Console.WriteLine("DAY_LENGTH_TEST: SET seconds=" + seconds);
		}

		[SteamFunction]
		public static void TestResetDayCycle(Character self) {
			LightAndWeather.ResetDayCycleForTest();
			Console.WriteLine("DAY_CYCLE_RESET_TEST: DONE");
		}

		[SteamFunction]
		public static void TestSpeedUpWorldClock(Character self, ScriptArgs sa) {
			double dueInSeconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			double periodInSeconds = Convert.ToDouble(sa.Argv[1], CultureInfo.InvariantCulture);
			LightAndWeather.SpeedUpClockForTest(dueInSeconds, periodInSeconds);
			Console.WriteLine("CLOCK_SPEEDUP_TEST: due=" + dueInSeconds + " period=" + periodInSeconds);
		}

		/// <summary>Takes the season as its real underlying int value (not the enum name) -
		/// GatheringTests already found that identifier-shaped speech-command arguments can get
		/// auto-resolved by LScript's argument binding, so plain numbers are the safe choice for
		/// every other test helper's arguments in this codebase too.</summary>
		[SteamFunction]
		public static void TestSetSeason(Character self, ScriptArgs sa) {
			int season = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			LightAndWeather.CurrentSeason = (Season) season;
			Console.WriteLine("SEASON_TEST: SET season=" + season);
		}
	}
}
