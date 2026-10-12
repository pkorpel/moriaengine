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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// V-10 (verification pass, #78): server-side helper for the real-movement-packet scenario
	/// driven by Tests/Protocol/MovementSpeedTests.cs.
	/// </summary>
	public static class MovementSpeedTests {
		/// <summary>Real finding while building this: the test map's spawn tile is hemmed in on
		/// all 8 sides by impassable terrain (confirmed via real move-request packets - same
		/// category of finding as V-7/V-9's own "exact spawn tile is impassable real map data").
		/// Teleports (not a real step - same "move the resource to the character" idiom as other
		/// phases' setup helpers) to the nearest open, non-water, non-impassable land tile found
		/// via an expanding-ring search, so the protocol test's real move-request packets have
		/// somewhere open to actually go (the test itself probes for a real clear direction from
		/// there via real packets, not a server-side guess).</summary>
		[SteamFunction]
		public static void TestTeleportToOpenGround(Character self) {
			Map map = self.GetMap();
			int cx = self.X;
			int cy = self.Y;
			for (int r = 1; r <= 200; r++) {
				for (int dx = -r; dx <= r; dx++) {
					for (int dy = -r; dy <= r; dy++) {
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) {
							continue;
						}
						int x = cx + dx;
						int y = cy + dy;
						if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
							continue;
						}
						int tileId = map.GetTileId(x, y);
						if (TileData.HasFlag(TileData.GetTileFlags(tileId), TileFlag.Impassable)) {
							continue;
						}
						bool anyStaticNearby = false;
						foreach (AbstractInternalItem item in map.GetStaticsAndMultiComponentsInRange(x, y, 7)) {
							anyStaticNearby = true;
							break;
						}
						if (anyStaticNearby) {
							continue;
						}
						int z = map.GetTileZ(x, y);
						self.Go(x, y, z, self.M);
						Console.WriteLine("OPEN_GROUND_FOUND: x=" + x + " y=" + y + " z=" + z);
						return;
					}
				}
			}
			Console.WriteLine("OPEN_GROUND_FOUND: none");
		}

		/// <summary>Real finding while building this: neither raw land-tile flags nor a direct
		/// Map.CheckMovement call (nor even real move-request packets, from certain positions)
		/// reliably predicted whether AbstractCharacter.WalkRunOrFly would actually accept a step
		/// - there's more to real movement validity than flags alone (static placement, Z/climb
		/// limits between tiles, etc). Rather than keep re-deriving all of that by hand, this
		/// calls the same real WalkRunOrFly the production movement-packet handler itself calls
		/// (MovementState.Movement) directly - a genuine step attempt, not a guess. Faces `dir`
		/// first (WalkRunOrFly only attempts a real step when already facing that way - otherwise
		/// it just turns, per its own documented behavior), so the result is a real
		/// Map.CheckMovement-validated outcome, not a turn.</summary>
		[SteamFunction]
		public static void TestProbeDirection(Character self, ScriptArgs sa) {
			int dirInt = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Direction dir = (Direction) dirInt;
			self.Direction = dir;
			bool moved = self.WalkRunOrFly(dir, false, true);
			Console.WriteLine("PROBE_RESULT: dir=" + dirInt + " moved=" + moved + " x=" + self.X + " y=" + self.Y);
		}

		[SteamFunction]
		public static void TestReportPosition(Character self) {
			Console.WriteLine("POSITION_REPORT: x=" + self.X + " y=" + self.Y);
		}
	}
}
