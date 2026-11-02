using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1031 - a stone wall with one gap, the player standing in the gap and an NPC ordered to the
	/// far side. The wall reaches past the normal search window, so only the long search finds
	/// the way round its end. The AI walks in real time, so the test starts it and
	/// asks how it went later.
	/// </summary>
	public static class NpcDetourTests {
		private const int WallHalfLength = 20;
		private const int WallOffset = 4;
		private static NPC walker;
		private static int wallX, gapY;

		[SteamFunction]
		public static void TestNpcDetourStart(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 24);
			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			VerificationTestUtils.CleanupNearbyItems(self, wallDef, 30);

			Point3D open = FindOpenGround(self);
			Console.WriteLine("NPC_DETOUR_GROUND: " + open);
			self.Go(open.X, open.Y, open.Z, self.M);

			self.Account.PLevel = 1;
			int x0 = self.X, y0 = self.Y, z0 = self.Z;
			byte m = self.M;
			wallX = x0 + WallOffset;
			gapY = y0;
			Map map = self.GetMap();
			for (int y = y0 - WallHalfLength; y <= y0 + WallHalfLength; y++) {
				if (y != y0) {
					wallDef.Create(wallX, y, map.GetTileZ(wallX, y), m);
				}
			}

			int gapZ = map.GetTileZ(wallX, y0);
			self.Go(wallX, y0, gapZ, m);

			walker = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(x0, y0, z0, m);
			walker.PetGoalPoint = new Point4D(x0 + (WallOffset * 2), y0, map.GetTileZ(x0 + (WallOffset * 2), y0), m);
			CharCollisionSettings settings = CharCollisionSettings.instance;
			AStar.Options options = new AStar.Options {
				BlockedWithin = settings.npcPathCharacterRadius, WindowMargin = settings.npcDetourSearchMargin,
				MaxIterations = settings.npcDetourMaxIterations, AllowPartial = false,
				Blocked = (x, y, z) => CharacterCollision.IsTileBlockedFor(walker, map, x, y, z),
			};
			DateTime before = DateTime.UtcNow;
			AStar.Result result = AStar.FindPathOnMap(walker, walker.PetGoalPoint, map, walker.MovementSettings, options);
			Console.WriteLine("NPC_DETOUR_SEARCH: reached=" + result.ReachedGoal + " steps=" + (result.Path == null ? -1 : result.Path.Length) + " iterations=" + result.Iterations + " ms=" + (DateTime.UtcNow - before).TotalMilliseconds);
			NPCAiTimer.EnsureInstalled(walker);
			Console.WriteLine("NPC_DETOUR_STARTED: player=" + self.X + "," + self.Y + "," + self.Z + " walker=" + walker.X + "," + walker.Y + "," + walker.Z);
		}

		/// <summary>The spawn's surroundings are small enclosures, and a wall across one just seals
		/// it. This finds a straight row with a way round the wall line that the wider search can
		/// walk, asking AStar itself with the line (gap included) closed.</summary>
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
						for (int i = 0; (i < WallOffset * 2) && (end != null); i++) {
							end = Walk(map, settings, end, Direction.East);
						}
						if ((end != null) && HasWayRoundTheLine(map, settings, start, end)) {
							return start;
						}
					}
				}
			}
			throw new SEException("No ground found for the detour test");
		}

		private static bool HasWayRoundTheLine(Map map, IMovementSettings settings, Point3D start, Point3D end) {
			CharCollisionSettings tuning = CharCollisionSettings.instance;
			int lineX = start.X + WallOffset;
			AStar.Options options = new AStar.Options {
				BlockedWithin = int.MaxValue, WindowMargin = tuning.npcDetourSearchMargin,
				MaxIterations = tuning.npcDetourMaxIterations, AllowPartial = false,
				Blocked = (x, y, z) => (x == lineX) && (Math.Abs(y - start.Y) <= WallHalfLength),
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

		[SteamFunction]
		public static void TestNpcDetourReport(Character self) {
			bool passed = (walker != null) && !walker.IsDeleted && (walker.X > wallX);
			Console.WriteLine("NPC_DETOUR_REPORT: walker=" + walker.X + "," + walker.Y + "," + walker.Z + " wallX=" + wallX + " goal=" + walker.PetGoalPoint + " passed=" + passed);
		}
	}
}
