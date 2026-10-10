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

using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Enhancements #23 - light switching reached the client via real packets already (region-
	/// flag-driven, underground vs surface), but nothing drove it by time of day (nightLight was
	/// explicitly marked "unused for now") and there was no weather/season system at all -
	/// SeasonalInformationOutPacket existed but every call site was commented out, and
	/// AbstractCharacter.Season/.Cursor (referenced by that dead code) didn't even exist as real
	/// properties.
	///
	/// WorldClockTimer (a single recurring BoundTimer on Globals.Instance, same idiom as
	/// NPCAiTimer/CriminalExpiryTimer elsewhere in this codebase) now drives both: flips day/night
	/// lighting based on elapsed real time modulo a configurable day length, and periodically
	/// rolls a new season to simulate weather changing over time - classic UO has no separate
	/// rain/snow packet, the client renders season-appropriate weather (snow in Winter, etc.)
	/// purely from the season byte, so cycling Season *is* the weather system here.
	/// </summary>
	[HasSavedMembers]
	public static class LightAndWeather {
		[SavedMember]
		private static int dayLight;
		[SavedMember]
		private static int nightLight = 12;
		[SavedMember]
		private static int undergroundLight = 26;

		[SavedMember]
		private static double dayLengthSeconds = 24 * 60; //24 real minutes = 1 game day, classic Sphere-ish default

		[SavedMember]
		private static Season currentSeason = Season.Spring;

		private static bool wasNight;

		public static double DayLengthSeconds {
			get {
				return dayLengthSeconds;
			}
			set {
				dayLengthSeconds = value;
			}
		}

		public static Season CurrentSeason {
			get {
				return currentSeason;
			}
			set {
				currentSeason = value;
				RefreshAllPlayers();
			}
		}

		/// <summary>First half of the day cycle is day, second half is night - simple and good enough without a real sunrise/sunset curve.</summary>
		public static bool IsNight {
			get {
				if (dayLengthSeconds <= 0) {
					return false;
				}
				double phase = (Globals.TimeInSeconds % dayLengthSeconds) / dayLengthSeconds;
				return phase >= 0.5;
			}
		}

		public static int GetLightAt(IPoint4D point) {
			EnsureClockRunning();
			point = point.TopPoint;
			FlaggedRegion asFlagged = point.GetMap().GetRegionFor(point) as FlaggedRegion;
			if (asFlagged != null) {
				return GetLightIn(asFlagged);
			}
			return IsNight ? nightLight : dayLight;
		}

		public static int GetLightIn(FlaggedRegion region)
		{
			EnsureClockRunning();
			if (region.Flag_Underground) {
				return undergroundLight;
			}
			return IsNight ? nightLight : dayLight;
		}

		private static readonly TimerKey worldClockTK = TimerKey.Acquire("_worldClock_");
		private const double ClockCheckIntervalSeconds = 60;
		private const double SeasonChangeChancePerCheck = 0.01; //~once every 100 checks on average, i.e. roughly once per 100 game-minutes at the default check interval

		/// <summary>Starts the recurring day/night + weather clock on first use - no reliable compiled-trigger hook for "@startup" on Globals.Instance was found (the LScript PeriodicSave.scp uses it, but that's a different, script-level attachment mechanism this class doesn't share), so this lazily self-installs instead, same as other on-demand timers in this codebase.</summary>
		private static void EnsureClockRunning() {
			if (!Globals.Instance.HasTimer(worldClockTK)) {
				WorldClockTimer timer = new WorldClockTimer();
				Globals.Instance.AddTimer(worldClockTK, timer);
				timer.DueInSeconds = ClockCheckIntervalSeconds;
				timer.PeriodInSeconds = ClockCheckIntervalSeconds;
			}
		}

		[SaveableClass]
		public class WorldClockTimer : BoundTimer {
			[LoadingInitializer]
			public WorldClockTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				bool nowNight = IsNight;
				if (nowNight != wasNight) {
					wasNight = nowNight;
					RefreshAllPlayers();
				}

				if (Globals.dice.NextDouble() < SeasonChangeChancePerCheck) {
					Season[] seasons = (Season[]) System.Enum.GetValues(typeof(Season));
					CurrentSeason = seasons[Globals.dice.Next(seasons.Length)];
				}
			}
		}

		public static int DayLight {
			get {
				return dayLight;
			}
			set { 
				dayLight = value;
				RefreshAllPlayers();
			}
		}

		public static int NightLight {
			get {
				return nightLight;
			}
			set { 
				nightLight = value;
				RefreshAllPlayers();
			}
		}

		public static int UndergroundLight {
			get {
				return undergroundLight;
			}
			set { 
				undergroundLight = value;
				RefreshAllPlayers();
			}
		}

		private static void RefreshAllPlayers() {
			foreach (Player p in GameServer.GetAllPlayers()) {
				p.SendGlobalLightLevel(GetLightAt(p));
				p.SendSeasonalInformation(currentSeason, CursorType.Normal);
			}
		}
	}
}
