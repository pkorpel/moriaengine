using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1199 - an NPC past its leash walks home: straight steps on open ground, no A* search; round a wall
	/// with one; out of a dead-end tube pointing home without stepping back into it; and with no way home
	/// at all, one search per back-off, not one per step. The NPC's own AI is paused and its walk home is
	/// driven step by step, all inside one command.
	/// </summary>
	public static class NpcWalkHomeTests {
		private const int HomeDistance = 8;
		private const int WallOffset = 4;
		private const int WallHalfLength = 3;
		private const int MaxSteps = 40;
		private const int TubeLength = 11; //longer than NpcPathCache.ReplanEvery, so the way out takes a replan

		[SteamFunction]
		public static void TestNpcWalkHome(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 24);
			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			VerificationTestUtils.CleanupNearbyItems(self, wallDef, 30);

			Point3D start = FindOpenGround(self);
			self.Go(start.X, start.Y, start.Z, self.M);
			Map map = self.GetMap();
			Point3D home = start;
			for (int i = 0; i < HomeDistance; i++) {
				home = Walk(map, self.MovementSettings, home, Direction.East);
			}
			Console.WriteLine("NPC_WALK_HOME_GROUND: start=" + start + " home=" + home);

			Report("open", WalkHomeFrom(start, home, self.M, MaxSteps), home);

			int wallX = start.X + WallOffset;
			for (int y = start.Y - WallHalfLength; y <= start.Y + WallHalfLength; y++) {
				wallDef.Create(wallX, y, map.GetTileZ(wallX, y), self.M);
			}
			Report("wall", WalkHomeFrom(start, home, self.M, MaxSteps), home);
			VerificationTestUtils.CleanupNearbyItems(self, wallDef, 30);

			for (int x = start.X - TubeLength + 1; x <= start.X + 1; x++) {
				for (int y = start.Y - 1; y <= start.Y + 1; y++) {
					if (IsTubeWall(start, x, y)) {
						wallDef.Create(x, y, map.GetTileZ(x, y), self.M);
					}
				}
			}
			Report("pocket", WalkHomeFrom(start, home, self.M, 80), home);
			VerificationTestUtils.CleanupNearbyItems(self, wallDef, 30);

			for (int dx = -1; dx <= 1; dx++) {
				for (int dy = -1; dy <= 1; dy++) {
					if ((dx != 0) || (dy != 0)) {
						wallDef.Create(start.X + dx, start.Y + dy, map.GetTileZ(start.X + dx, start.Y + dy), self.M);
					}
				}
			}
			Report("enclosed", WalkHomeFrom(start, home, self.M, 20), home);
			CharCollisionSettings settings = CharCollisionSettings.instance;
			double backOff = settings.npcFailedPathRetrySeconds;
			try {
				settings.npcFailedPathRetrySeconds = 0; //control: without the back-off every step searches
				Report("enclosedNoBackOff", WalkHomeFrom(start, home, self.M, 20), home);
			} finally {
				settings.npcFailedPathRetrySeconds = backOff;
			}
			VerificationTestUtils.CleanupNearbyItems(self, wallDef, 30);
			Console.WriteLine("NPC_WALK_HOME_DONE");
		}

		private static NPC WalkHomeFrom(Point3D start, Point3D home, byte m, int steps) {
			NPC walker = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(start.X, start.Y, start.Z, m);
			NPCAiTimer timer = NPCAiTimer.GetFor(walker);
			timer.DueInSeconds = -1; //its own thinking would add random steps
			for (int i = 0; (i < steps) && ((walker.X != home.X) || (walker.Y != home.Y)); i++) {
				timer.WalkHome(walker, home);
			}
			return walker;
		}

		private static void Report(string name, NPC walker, Point3D home) {
			bool reached = (walker.X == home.X) && (walker.Y == home.Y);
			Console.WriteLine("NPC_WALK_HOME: case=" + name + " reached=" + reached + " searches=" + NPCAiTimer.GetFor(walker).PathSearches +
				" at=" + walker.X + "," + walker.Y + "," + walker.Z);
			walker.Delete();
		}

		/// <summary>Walls along both sides of the row west of start, and across its east end: open only to the west.</summary>
		private static bool IsTubeWall(Point3D start, int x, int y) {
			if ((x < start.X - TubeLength + 1) || (x > start.X + 1) || (Math.Abs(y - start.Y) > 1)) {
				return false;
			}
			return (y != start.Y) || (x == start.X + 1);
		}

		/// <summary>A straight row of HomeDistance steps east and TubeLength + 1 west, with a way round the
		/// wall line and out of the tube within the normal search window (asked of AStar itself with them closed).</summary>
		private static Point3D FindOpenGround(Character self) {
			Map map = self.GetMap();
			IMovementSettings settings = self.MovementSettings;
			for (int radius = 0; radius <= 600; radius += 8) {
				for (int dx = -radius; dx <= radius; dx += 8) {
					for (int dy = -radius; dy <= radius; dy += 8) {
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) {
							continue;
						}
						int x = self.X + dx, y = self.Y + dy;
						if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
							continue;
						}
						Point3D start = new Point3D(x, y, map.GetTileZ(x, y));
						Point3D end = start;
						for (int i = 0; (i < HomeDistance) && (end != null); i++) {
							end = Walk(map, settings, end, Direction.East);
						}
						Point3D west = start;
						for (int i = 0; (i <= TubeLength) && (west != null); i++) {
							west = Walk(map, settings, west, Direction.West);
						}
						if ((end != null) && (west != null) && HasWayRound(map, settings, start, end, false) && HasWayRound(map, settings, start, end, true)) {
							return start;
						}
					}
				}
			}
			throw new SEException("No ground found for the walk home test");
		}

		private static bool HasWayRound(Map map, IMovementSettings settings, Point3D start, Point3D end, bool tube) {
			int lineX = start.X + WallOffset;
			AStar.Options options = new AStar.Options {
				BlockedWithin = int.MaxValue, AllowPartial = false,
				Blocked = tube ? (Func<int, int, int, bool>) ((x, y, z) => IsTubeWall(start, x, y)) :
					((x, y, z) => (x == lineX) && (Math.Abs(y - start.Y) <= WallHalfLength)),
			};
			return AStar.FindPathOnMap(start, end, map, settings, options).ReachedGoal;
		}

		private static Point3D Walk(Map map, IMovementSettings settings, Point3D p, Direction dir) {
			int x, y, z;
			if (!map.CheckMovement(p, settings, dir, false, out x, out y, out z)) {
				return null;
			}
			return new Point3D(x, y, z);
		}
	}
}
