using System;
using System.Collections.Generic;
using Xunit;
using SteamEngine.CompiledScripts;
using SteamEngine.Regions;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	/// <summary>
	/// #769 - the AStar search core on synthetic grids (no map data needed), plus NpcPathCache.
	/// A BFS over the same grid is the oracle for "shortest path".
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class AStarTests {
		/// <summary>Flat grid, every step costs 1, diagonals can't cut a blocked corner (as in UO).
		/// Optional per-tile Z for the Z-tolerance test.</summary>
		private sealed class Grid {
			internal readonly int width, height;
			internal readonly bool[,] blocked;
			internal readonly int[,] z;

			internal Grid(int width, int height) {
				this.width = width;
				this.height = height;
				this.blocked = new bool[width, height];
				this.z = new int[width, height];
			}

			internal bool Free(int x, int y) {
				return (x >= 0) && (y >= 0) && (x < this.width) && (y < this.height) && !this.blocked[x, y];
			}

			internal bool Step(int x, int y, int zz, Direction dir, out int newX, out int newY, out int newZ) {
				newX = x;
				newY = y;
				Map.Offset(dir, ref newX, ref newY);
				newZ = this.Free(newX, newY) ? this.z[newX, newY] : 0;
				if (!this.Free(newX, newY)) {
					return false;
				}
				if ((newX != x) && (newY != y)) {
					return this.Free(newX, y) && this.Free(x, newY);
				}
				return true;
			}

			internal AStar.Result Find(int sx, int sy, int tx, int ty, AStar.Options options) {
				return AStar.FindPath(sx, sy, this.z[sx, sy], tx, ty, this.z[tx, ty], this.Step, options ?? new AStar.Options());
			}

			/// <summary>Shortest step count, or -1 if unreachable.</summary>
			internal int Bfs(int sx, int sy, int tx, int ty) {
				int[,] dist = new int[this.width, this.height];
				for (int i = 0; i < this.width; i++) {
					for (int j = 0; j < this.height; j++) {
						dist[i, j] = -1;
					}
				}
				Queue<(int, int)> queue = new Queue<(int, int)>();
				dist[sx, sy] = 0;
				queue.Enqueue((sx, sy));
				while (queue.Count > 0) {
					(int x, int y) = queue.Dequeue();
					for (int d = 0; d < 8; d++) {
						int nx, ny, nz;
						if (this.Step(x, y, 0, (Direction) d, out nx, out ny, out nz) && (dist[nx, ny] < 0)) {
							dist[nx, ny] = dist[x, y] + 1;
							queue.Enqueue((nx, ny));
						}
					}
				}
				return dist[tx, ty];
			}

			/// <summary>Walks path from (sx, sy), asserting every step is legal; returns the end tile.</summary>
			internal (int, int) Walk(int sx, int sy, Direction[] path) {
				int x = sx, y = sy;
				foreach (Direction dir in path) {
					int nx, ny, nz;
					Assert.True(this.Step(x, y, 0, dir, out nx, out ny, out nz), "illegal step " + dir + " from " + x + "," + y);
					x = nx;
					y = ny;
				}
				return (x, y);
			}
		}

		[Fact]
		public void OpenGroundPathIsChebyshevLength() {
			Grid grid = new Grid(20, 20);
			AStar.Result result = grid.Find(2, 2, 9, 6, null);
			Assert.True(result.ReachedGoal);
			Assert.Equal(7, result.Path.Length);
			Assert.Equal((9, 6), grid.Walk(2, 2, result.Path));
		}

		[Fact]
		public void FindsShortestPathOutOfAConcaveTrap() {
			//a U-shaped wall opening west, start inside it, target straight east behind it -
			//the shape the old squared-distance greedy search flooded its whole budget on
			Grid grid = new Grid(40, 40);
			for (int y = 10; y <= 30; y++) {
				grid.blocked[25, y] = true;
			}
			for (int x = 10; x <= 25; x++) {
				grid.blocked[x, 10] = true;
				grid.blocked[x, 30] = true;
			}
			AStar.Result result = grid.Find(20, 20, 32, 20, null);
			Assert.True(result.ReachedGoal);
			Assert.Equal(grid.Bfs(20, 20, 32, 20), result.Path.Length);
			Assert.Equal((32, 20), grid.Walk(20, 20, result.Path));
		}

		[Fact]
		public void PathsMatchBfsOnRandomGrids() {
			Random rnd = new Random(769);
			int compared = 0;
			for (int round = 0; round < 60; round++) {
				Grid grid = new Grid(30, 30);
				for (int x = 0; x < 30; x++) {
					for (int y = 0; y < 30; y++) {
						grid.blocked[x, y] = rnd.Next(100) < 25;
					}
				}
				grid.blocked[1, 1] = false;
				grid.blocked[28, 27] = false;
				int expected = grid.Bfs(1, 1, 28, 27);
				AStar.Options options = new AStar.Options { MaxIterations = 100000, AllowPartial = false };
				AStar.Result result = grid.Find(1, 1, 28, 27, options);
				if (expected < 0) {
					Assert.Null(result.Path);
					continue;
				}
				compared++;
				Assert.NotNull(result.Path);
				Assert.Equal(expected, result.Path.Length);
				Assert.Equal((28, 27), grid.Walk(1, 1, result.Path));
			}
			Assert.True(compared >= 10, "too few reachable random grids to be a real check: " + compared);
		}

		[Fact]
		public void TargetZWithinToleranceCountsAsReached() {
			//the old goal test needed an exact Z match, so a target standing a few Z units off its
			//tile's walkable Z (on an item, a slope) was never "reached"
			Grid grid = new Grid(10, 10);
			AStar.Result result = AStar.FindPath(1, 1, 0, 6, 1, 5, grid.Step, new AStar.Options());
			Assert.True(result.ReachedGoal);
			Assert.Equal(5, result.Path.Length);

			AStar.Result tooFar = AStar.FindPath(1, 1, 0, 6, 1, 40, grid.Step, new AStar.Options { AllowPartial = false });
			Assert.Null(tooFar.Path);
		}

		[Fact]
		public void StopWithinEndsAdjacentToTheTarget() {
			Grid grid = new Grid(20, 20);
			AStar.Result result = grid.Find(2, 5, 10, 5, new AStar.Options { StopWithin = 1 });
			Assert.True(result.ReachedGoal);
			Assert.Equal(7, result.Path.Length);
			(int ex, int ey) = grid.Walk(2, 5, result.Path);
			Assert.Equal(1, Math.Max(Math.Abs(ex - 10), Math.Abs(ey - 5))); //(9,4)/(9,5)/(9,6) are all 7 steps away
		}

		[Fact]
		public void AlreadyAtTheGoalReturnsAnEmptyPath() {
			Grid grid = new Grid(10, 10);
			AStar.Result result = grid.Find(4, 4, 5, 5, new AStar.Options { StopWithin = 1 });
			Assert.True(result.ReachedGoal);
			Assert.Empty(result.Path);
		}

		[Fact]
		public void UnreachableTargetGivesAPartialPathTowardIt() {
			Grid grid = new Grid(30, 30);
			for (int i = 18; i <= 22; i++) { //target boxed in on all sides
				grid.blocked[i, 18] = true;
				grid.blocked[i, 22] = true;
				grid.blocked[18, i] = true;
				grid.blocked[22, i] = true;
			}
			AStar.Result partial = grid.Find(2, 20, 20, 20, null);
			Assert.False(partial.ReachedGoal);
			Assert.NotNull(partial.Path);
			(int ex, int ey) = grid.Walk(2, 20, partial.Path);
			Assert.True(Math.Max(Math.Abs(ex - 20), Math.Abs(ey - 20)) <= 3, "partial path should end against the box, ended at " + ex + "," + ey);
			Assert.True(partial.Iterations <= AStar.DefaultMaxIterations);

			AStar.Result none = grid.Find(2, 20, 20, 20, new AStar.Options { AllowPartial = false });
			Assert.Null(none.Path);
		}

		[Fact]
		public void BlockedFirstStepIsRoutedAround() {
			Grid grid = new Grid(20, 20);
			AStar.Options options = new AStar.Options { Blocked = (x, y, z) => (x == 6) && (y == 5) };
			AStar.Result result = grid.Find(5, 5, 10, 5, options);
			Assert.True(result.ReachedGoal);
			Assert.NotEqual(Direction.East, result.Path[0]);
			Assert.Equal(5, result.Path.Length); //a diagonal first step costs nothing extra
		}

		[Fact]
		public void BlockedFirstStepOntoTheTargetTileIsStillAllowed() {
			Grid grid = new Grid(10, 10);
			AStar.Options options = new AStar.Options { Blocked = (x, y, z) => true };
			AStar.Result result = grid.Find(4, 4, 5, 4, options);
			Assert.True(result.ReachedGoal);
			Assert.Equal(new[] { Direction.East }, result.Path);
		}

		/// <summary>A wall along x = wallX, open only at the given rows.</summary>
		private static Grid GridWithWall(int width, int height, int wallX, params int[] gaps) {
			Grid grid = new Grid(width, height);
			for (int y = 0; y < height; y++) {
				grid.blocked[wallX, y] = Array.IndexOf(gaps, y) < 0;
			}
			return grid;
		}

		[Fact]
		public void CharacterInAGapFurtherAlongSendsThePathThroughTheOtherGap() {
			Grid grid = GridWithWall(30, 30, 10, 15, 22);
			AStar.Options options = new AStar.Options {
				BlockedWithin = 8,
				Blocked = (x, y, z) => (x == 10) && (y == 15),
			};
			AStar.Result result = grid.Find(6, 15, 16, 15, options);
			Assert.True(result.ReachedGoal);
			Assert.True(result.HitBlocked);
			int cx = 6, cy = 15;
			foreach (Direction dir in result.Path) {
				Map.Offset(dir, ref cx, ref cy);
				Assert.False((cx == 10) && (cy == 15), "the path stepped onto the occupied gap");
			}
			Assert.Equal((16, 15), grid.Walk(6, 15, result.Path));
		}

		[Fact]
		public void NoDawdlingUntilABlockedTileCountsAsFarAway() {
			//right next to the only gap: with the rule on steps taken, not on distance, wandering
			//off and coming back gets the search through the occupied tile
			Grid grid = GridWithWall(20, 20, 10, 10);
			AStar.Options options = new AStar.Options {
				BlockedWithin = 3,
				AllowPartial = false,
				Blocked = (x, y, z) => (x == 10) && (y == 10),
			};
			AStar.Result result = grid.Find(9, 10, 14, 10, options);
			Assert.False(result.ReachedGoal);
			Assert.True(result.HitBlocked);
		}

		[Fact]
		public void CharacterBeyondTheRadiusIsIgnored() {
			Grid grid = GridWithWall(30, 30, 10, 15);
			AStar.Options options = new AStar.Options {
				BlockedWithin = 3,
				Blocked = (x, y, z) => (x == 10) && (y == 15),
			};
			AStar.Result result = grid.Find(2, 15, 16, 15, options);
			Assert.True(result.ReachedGoal);
			Assert.False(result.HitBlocked);
			Assert.Equal(14, result.Path.Length);
		}

		[Fact]
		public void ADetourOutsideTheNormalWindowNeedsTheWiderSearch() {
			Grid grid = GridWithWall(120, 120, 60, 60, 110);
			Func<int, int, int, bool> blocked = (x, y, z) => (x == 60) && (y == 60);

			AStar.Result normal = grid.Find(57, 60, 63, 60, new AStar.Options { BlockedWithin = 8, Blocked = blocked });
			Assert.False(normal.ReachedGoal);
			Assert.True(normal.HitBlocked);

			AStar.Result wide = grid.Find(57, 60, 63, 60, new AStar.Options {
				BlockedWithin = 8, Blocked = blocked, WindowMargin = 64, MaxIterations = 20000, AllowPartial = false,
			});
			Assert.True(wide.ReachedGoal);
			Assert.Equal((63, 60), grid.Walk(57, 60, wide.Path));
		}

		[Fact]
		public void PathCacheWalksADetourToItsEnd() {
			NpcPathCache cache = new NpcPathCache();
			Direction[] path = new Direction[12];
			cache.SetPath(path, 0, 0, 10, 0, true);
			Direction dir;
			for (int i = 0; i < 12; i++) {
				Assert.True(cache.TryGetNextStep(i, 0, 15, 0, out dir), "step " + i); //5 tiles of drift is fine
				cache.Advance(i + 1, 0);
			}
			Assert.False(cache.TryGetNextStep(12, 0, 15, 0, out dir));

			cache.SetPath(path, 0, 0, 10, 0, true);
			Assert.False(cache.TryGetNextStep(0, 0, 19, 0, out dir)); //the target went somewhere else entirely
		}

		[Fact]
		public void PathCacheHandsOutStepsWhileThePathStillApplies() {
			NpcPathCache cache = new NpcPathCache();
			cache.SetPath(new[] { Direction.East, Direction.East, Direction.SouthEast }, 0, 0, 10, 1);
			Direction dir;
			Assert.True(cache.TryGetNextStep(0, 0, 10, 1, out dir));
			Assert.Equal(Direction.East, dir);
			cache.Advance(1, 0);
			Assert.True(cache.TryGetNextStep(1, 0, 11, 2, out dir)); //target drifted by 1 - still fine
			Assert.Equal(Direction.East, dir);
			cache.Advance(2, 0);
			Assert.True(cache.TryGetNextStep(2, 0, 10, 1, out dir));
			Assert.Equal(Direction.SouthEast, dir);
			cache.Advance(3, 1);
			Assert.False(cache.TryGetNextStep(3, 1, 10, 1, out dir)); //path used up
		}

		[Fact]
		public void PathCacheReplansWhenTheTargetMovesOrTheMoverIsDisplaced() {
			NpcPathCache cache = new NpcPathCache();
			Direction dir;
			cache.SetPath(new[] { Direction.East, Direction.East }, 0, 0, 10, 0);
			Assert.False(cache.TryGetNextStep(0, 0, 13, 0, out dir)); //target moved 3 tiles
			Assert.False(cache.TryGetNextStep(0, 1, 10, 0, out dir)); //mover isn't where the path expects (knocked back, teleported)
			cache.Invalidate();
			Assert.False(cache.TryGetNextStep(0, 0, 10, 0, out dir));
		}

		[Fact]
		public void PathCacheReplansPeriodically() {
			NpcPathCache cache = new NpcPathCache();
			Direction[] longPath = new Direction[20];
			cache.SetPath(longPath, 0, 0, 50, 0);
			Direction dir;
			for (int i = 0; i < NpcPathCache.ReplanEvery; i++) {
				Assert.True(cache.TryGetNextStep(0, i, 50, 0, out dir));
				cache.Advance(0, i + 1);
			}
			Assert.False(cache.TryGetNextStep(0, NpcPathCache.ReplanEvery, 50, 0, out dir));
		}

		/// <summary>#1199 - a failed search holds off the next one from about the same spot toward about the same spot,
		/// for the back-off only; walked away from there, the NPC may search again.</summary>
		[Fact]
		public void PathCacheBacksOffAfterAFailedSearch() {
			NpcPathCache cache = new NpcPathCache();
			Assert.False(cache.IsBackingOff(0, 0, 10, 0, 100)); //nothing failed yet

			cache.RememberFailedSearch(0, 0, 10, 0, 100, 8);
			Assert.True(cache.IsBackingOff(0, 0, 10, 0, 100));
			Assert.True(cache.IsBackingOff(0, 0, 10, 0, 107.9));
			Assert.True(cache.IsBackingOff(0, 0, 10 + NpcPathCache.FailedTargetDrift, 0, 101)); //about the same target
			Assert.False(cache.IsBackingOff(0, 0, 10 + NpcPathCache.FailedTargetDrift + 1, 0, 101)); //another target
			Assert.True(cache.IsBackingOff(NpcPathCache.FailedTargetDrift, 0, 10, 0, 101)); //about the same place
			Assert.False(cache.IsBackingOff(-NpcPathCache.FailedTargetDrift - 1, 0, 10, 0, 101)); //walked away: search afresh
			Assert.False(cache.IsBackingOff(0, 0, 10, 0, 108)); //back-off over

			cache.RememberFailedSearch(0, 0, 10, 0, 200, 8);
			cache.Invalidate(); //a failed step forgets the path, not the failure
			Assert.True(cache.IsBackingOff(0, 0, 10, 0, 201));

			cache.RememberFailedSearch(0, 0, 10, 0, 300, 0); //back-off of 0 = no back-off
			Assert.False(cache.IsBackingOff(0, 0, 10, 0, 300));
		}

		/// <summary>#1199 - the walk home keeps to its path past ReplanEvery steps, but only a path to exactly that spot.</summary>
		[Fact]
		public void PathCacheTellsAWalkToItsExactTargetPastReplanEvery() {
			NpcPathCache cache = new NpcPathCache();
			Assert.False(cache.IsWalkingTo(0, 0, 20, 0));
			cache.SetPath(new Direction[12], 0, 0, 20, 0);
			Assert.False(cache.IsWalkingTo(0, 0, 21, 0)); //another target, even one TryGetNextStep would accept
			for (int i = 0; i < 11; i++) {
				Assert.True(cache.IsWalkingTo(i, 0, 20, 0));
				cache.Advance(i + 1, 0);
			}
			Assert.False(cache.IsWalkingTo(10, 0, 20, 0)); //not where the path expects it
			Assert.True(cache.IsWalkingTo(11, 0, 20, 0)); //a step left, past ReplanEvery
			cache.Advance(12, 0);
			Assert.False(cache.IsWalkingTo(12, 0, 20, 0)); //used up
		}
	}
}
