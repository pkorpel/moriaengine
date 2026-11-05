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
using SteamEngine.Common;

namespace SteamEngine.Regions {
	/// <summary>
	/// #1201 - which parts of a map plane have a player near, so NPCs can sleep where nobody is (Sphere's sector sleep).
	/// The plane is cut into areas of AreaSize tiles (4x4 sectors, Sphere's size). An area is awake while a player is in it
	/// or in one of its 8 neighbours, and for AwakeSeconds after the last of them left; at boot nothing is awake.
	/// Players are counted where a sector's list of players changes (Map.Sector's AddPlayer/RemovePlayer), so steps,
	/// teleports, logins, logouts and plane changes all count. Game thread only (under globalLock).
	/// </summary>
	public sealed class ActivityGrid {
		public const int AreaShift = 6;
		public const int AreaSize = 1 << AreaShift;

		/// <summary>steamengine.ini's npcAreaAwakeSeconds: how long an area stays awake after its last nearby player
		/// left. 0 or less turns sleeping off: every area is always awake.</summary>
		public static double AwakeSeconds { get; set; } = 300;

		/// <summary>Scripts' hook: an asleep area (map, area x, area y) just woke. Called in the middle of a sector
		/// change: it may resume timers, it must not move things. Forgotten at a recompile.</summary>
		public static Action<Map, int, int> AreaWoke { get; set; }

		/// <summary>Scripts' hook: a character that isn't a player came onto the map or into another area (a GM's
		/// summon, a teleport). Same rules as AreaWoke.</summary>
		public static Action<AbstractCharacter> CharacterChangedArea { get; set; }

		private readonly int width, height;
		private readonly int[] players;	//players in the area
		private readonly int[] nearby;	//players in the area and its 8 neighbours
		private readonly double[] emptySince;	//when nearby last fell to 0; asleep AwakeSeconds after (read then, so a new value applies at once)
		private readonly Action<int, int> woke;

		public ActivityGrid(int sizeX, int sizeY, Action<int, int> woke) {
			this.width = (sizeX + AreaSize - 1) >> AreaShift;
			this.height = (sizeY + AreaSize - 1) >> AreaShift;
			this.players = new int[this.width * this.height];
			this.nearby = new int[this.players.Length];
			this.emptySince = new double[this.players.Length];
			this.woke = woke;
			this.Clear();
		}

		public static bool SleepingEnabled {
			get {
				return AwakeSeconds > 0;
			}
		}

		public int Width {
			get {
				return this.width;
			}
		}

		public int Height {
			get {
				return this.height;
			}
		}

		/// <summary>Whether the area holding tile (x, y) is awake at game time `now` (Globals.TimeInSeconds).</summary>
		public bool IsAwake(int x, int y, double now) {
			return this.IsAreaAwake(x >> AreaShift, y >> AreaShift, now);
		}

		public bool IsAreaAwake(int ax, int ay, double now) {
			if (!SleepingEnabled || !this.IsValidArea(ax, ay)) {
				return true;
			}
			int i = (ay * this.width) + ax;
			return (this.nearby[i] > 0) || (now < this.emptySince[i] + AwakeSeconds);
		}

		public int PlayersIn(int ax, int ay) {
			return this.IsValidArea(ax, ay) ? this.players[(ay * this.width) + ax] : 0;
		}

		private bool IsValidArea(int ax, int ay) {
			return (ax >= 0) && (ay >= 0) && (ax < this.width) && (ay < this.height);
		}

		/// <summary>A player came into area (ax, ay). Each neighbouring area that was asleep wakes, after all the counts are in.</summary>
		internal void PlayerArrived(int ax, int ay, double now) {
			if (!this.IsValidArea(ax, ay)) {
				return;
			}
			this.players[(ay * this.width) + ax]++;
			int wokeMask = 0;
			for (int dy = -1; dy <= 1; dy++) {
				for (int dx = -1; dx <= 1; dx++) {
					int nx = ax + dx, ny = ay + dy;
					if (this.IsValidArea(nx, ny)) {
						int i = (ny * this.width) + nx;
						if ((this.nearby[i]++ == 0) && !(now < this.emptySince[i] + AwakeSeconds)) {
							wokeMask |= 1 << (((dy + 1) * 3) + dx + 1);
						}
					}
				}
			}
			if ((wokeMask != 0) && SleepingEnabled && (this.woke != null)) {
				for (int bit = 0; bit < 9; bit++) {
					if ((wokeMask & (1 << bit)) != 0) {
						this.woke(ax + (bit % 3) - 1, ay + (bit / 3) - 1);
					}
				}
			}
		}

		/// <summary>A player left area (ax, ay). An area left with nobody near starts its AwakeSeconds countdown.</summary>
		internal void PlayerLeft(int ax, int ay, double now) {
			if (!this.IsValidArea(ax, ay)) {
				return;
			}
			int self = (ay * this.width) + ax;
			Sanity.IfTrueThrow(this.players[self] <= 0, "ActivityGrid.PlayerLeft(" + ax + "," + ay + ") with no player counted there");
			this.players[self]--;
			for (int dy = -1; dy <= 1; dy++) {
				for (int dx = -1; dx <= 1; dx++) {
					int nx = ax + dx, ny = ay + dy;
					if (this.IsValidArea(nx, ny)) {
						int i = (ny * this.width) + nx;
						if (--this.nearby[i] == 0) {
							this.emptySince[i] = now;
						}
					}
				}
			}
		}

		/// <summary>The world was cleared: no players anywhere, everything asleep.</summary>
		internal void Clear() {
			Array.Clear(this.players, 0, this.players.Length);
			Array.Clear(this.nearby, 0, this.nearby.Length);
			Array.Fill(this.emptySince, double.NegativeInfinity); //asleep, whatever the clock says
		}

		internal static void ForgetScripts() {
			AreaWoke = null;
			CharacterChangedArea = null;
		}
	}
}
