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
	/// V-9 (verification pass, #77): Phase 17 content-restoration spot-check - server-side
	/// helpers for the movement-capability scenario driven by real movement (not teleport)
	/// packets/calls from Tests/Protocol/ContentRestorationTests.cs. #77's armor scenario is
	/// deliberately not covered here - see issue #96 and that test file's own doc comment.
	/// </summary>
	public static class ContentRestorationTests {
		/// <summary>Expanding-ring search for the nearest real water tile, then the nearest of
		/// its 4 cardinal non-water neighbors to approach it from - gives a test a deterministic,
		/// known (approach x/y, direction-into-the-water) pair without needing to know this test
		/// map's real layout ahead of time (same idea as V-7's TestGoToNearbyFoliage).</summary>
		[SteamFunction]
		public static void TestFindWaterCrossing(Character self) {
			Map map = self.GetMap();
			int cx = self.X;
			int cy = self.Y;
			for (int r = 1; r <= 1000; r++) { //#861 - the spawn moved since V-9: no water within 200 tiles of it any more
				for (int dx = -r; dx <= r; dx++) {
					for (int dy = -r; dy <= r; dy++) {
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) {
							continue; //only scan the new outer ring, inner ones already checked
						}
						int wx = cx + dx;
						int wy = cy + dy;
						if (!map.IsValidPos(wx, wy) || !map.IsMapTileWater(wx, wy)) {
							continue;
						}
						int ax, ay;
						Direction dir;
						if (TryFindApproach(map, wx, wy, out ax, out ay, out dir)) {
							Console.WriteLine("WATER_CROSSING_FOUND: wx=" + wx + " wy=" + wy +
								" ax=" + ax + " ay=" + ay + " dir=" + (int) dir + " from=" + cx + "," + cy + " r=" + r);
							return;
						}
					}
				}
			}
			Console.WriteLine("WATER_CROSSING_FOUND: none");
		}

		private static bool IsBare(Map map, int x, int y) {
			foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				return false;
			}
			return true;
		}

		private static bool TryFindApproach(Map map, int wx, int wy, out int ax, out int ay, out Direction dir) {
			//offset, then the direction that steps from the offset tile back onto (wx, wy)
			(int Dx, int Dy, Direction Dir)[] candidates = {
				(-1, 0, Direction.East),
				(1, 0, Direction.West),
				(0, -1, Direction.South),
				(0, 1, Direction.North),
			};
			foreach (var c in candidates) {
				int tx = wx + c.Dx;
				int ty = wy + c.Dy;
				if (map.IsValidPos(tx, ty) && !map.IsMapTileWater(tx, ty) && IsBare(map, tx, ty) && IsBare(map, wx, wy)
						&& (Math.Abs(map.GetTileZ(tx, ty) - map.GetTileZ(wx, wy)) <= 2)) { //#861 - a fair crossing: no statics, no cliff
					ax = tx;
					ay = ty;
					dir = c.Dir;
					return true;
				}
			}
			ax = 0;
			ay = 0;
			dir = Direction.North;
			return false;
		}

		/// <summary>Spawns a throwaway NPC at a real land position with an explicit
		/// DynamicMovementSettings - canFly is the only thing that varies between the two real
		/// scenario runs, everything else held identical, same "otherwise identical" principle
		/// the armor side of #77 asked for.</summary>
		[SteamFunction]
		public static void TestSpawnMovementTestNpc(Character self, ScriptArgs sa) {
			bool canFly = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture) != 0;
			int x = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[2], CultureInfo.InvariantCulture);

			Map map = self.GetMap();
			int z = map.GetTileZ(x, y);
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_bird_crow");
			NPC npc = (NPC) def.Create(x, y, z, self.M);
			npc.MovementSettings = new DynamicMovementSettings(
				canCrossLand: true, canSwim: false, canCrossLava: false, canFly: canFly,
				ignoreDoors: false, climbPower: 2);
			Console.WriteLine("MOVE_NPC_SETUP: npc=#" + npc.Uid);
		}

		/// <summary>Faces the NPC the requested way first (WalkRunOrFly only attempts a real step
		/// when already facing that direction - otherwise it just turns, per its own doc/code in
		/// AbstractCharacter.cs), then makes the one real step attempt under test, going through
		/// the genuine Map.CheckMovement(this.MovementSettings, ...) validation.</summary>
		[SteamFunction]
		public static void TestStepNpc(Character self, ScriptArgs sa) {
			int npcUid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int dirInt = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			Direction dir = (Direction) dirInt;

			NPC npc = (NPC) Thing.UidGetCharacter(npcUid);
			npc.Direction = dir;
			bool moved = npc.WalkRunOrFly(dir, false, true);
			Console.WriteLine("STEP_RESULT: moved=" + moved + " x=" + npc.X + " y=" + npc.Y);
		}
	}
}
