using System;
using SteamEngine.CompiledScripts;
using SteamEngine.Regions;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #781 - knockback and roundattack's pull move their victim by forced steps
	/// (MonsterSpecialAttacks.WalkForced/ForceMove) under the same rules as walking, so a wall stops
	/// them. Before, only the terrain tile under each step was checked: house walls (statics) never
	/// stopped a knockback, and the pull teleported its victim straight next to the monster -
	/// through walls either way. Pure tests on a synthetic grid; ForceMove plugs Map.CheckMovement
	/// into the same step delegate.
	/// </summary>
	public class KnockbackPushDistanceTests {
		/// <summary>Walls block, diagonals can't cut a blocked corner, each tile has its own Z.</summary>
		private sealed class Grid {
			internal readonly bool[,] wall = new bool[30, 30];
			internal readonly int[,] z = new int[30, 30];

			internal bool Free(int x, int y) {
				return (x >= 0) && (y >= 0) && (x < 30) && (y < 30) && !this.wall[x, y];
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

			internal int Push(int x, int y, Direction dir, int distance, out int endX, out int endY, out int endZ) {
				return MonsterSpecialAttacks.WalkForced(x, y, this.z[x, y], distance, (px, py) => dir, (px, py) => false, this.Step, out endX, out endY, out endZ);
			}

			internal int Pull(int x, int y, int monsterX, int monsterY, int distance, out int endX, out int endY, out int endZ) {
				return MonsterSpecialAttacks.WalkForced(x, y, this.z[x, y], distance,
					(px, py) => Point2D.GetDirFromTo(new Point2D(px, py), new Point2D(monsterX, monsterY)),
					(px, py) => Math.Max(Math.Abs(px - monsterX), Math.Abs(py - monsterY)) <= 1,
					this.Step, out endX, out endY, out endZ);
			}
		}

		[Fact]
		public void StandingRightByAWallIsNotPushedAtAll() {
			Grid grid = new Grid();
			grid.wall[11, 10] = true;
			int ex, ey, ez;
			Assert.Equal(0, grid.Push(10, 10, Direction.East, 3, out ex, out ey, out ez));
			Assert.Equal((10, 10), (ex, ey));
		}

		[Fact]
		public void TwoTilesFromAWallIsPushedExactlyTwo() {
			Grid grid = new Grid();
			grid.wall[13, 10] = true;
			int ex, ey, ez;
			Assert.Equal(2, grid.Push(10, 10, Direction.East, 3, out ex, out ey, out ez));
			Assert.Equal((12, 10), (ex, ey));
		}

		[Fact]
		public void OpenGroundGetsTheFullDistance() {
			Grid grid = new Grid();
			int ex, ey, ez;
			Assert.Equal(3, grid.Push(10, 10, Direction.West, 3, out ex, out ey, out ez));
			Assert.Equal((7, 10), (ex, ey));
		}

		[Fact]
		public void NeverMovesMoreThanTheDistance() {
			Grid grid = new Grid();
			int ex, ey, ez;
			Assert.Equal(0, grid.Push(10, 10, Direction.East, 0, out ex, out ey, out ez));
			Assert.Equal((10, 10), (ex, ey));
			Assert.Equal(5, grid.Push(10, 10, Direction.South, 5, out ex, out ey, out ez));
			Assert.Equal((10, 15), (ex, ey));
		}

		[Fact]
		public void DiagonalPushCannotSlipThroughAWallCorner() {
			//the corner of a building south-east of the victim: both orthogonal neighbours are wall
			Grid grid = new Grid();
			grid.wall[11, 10] = true;
			grid.wall[10, 11] = true;
			int ex, ey, ez;
			Assert.Equal(0, grid.Push(10, 10, Direction.SouthEast, 3, out ex, out ey, out ez));
		}

		[Fact]
		public void LandsOnTheRealHeightOfTheLastTile() {
			Grid grid = new Grid();
			grid.z[11, 10] = 2;
			grid.z[12, 10] = 4;
			int ex, ey, ez;
			grid.Push(10, 10, Direction.East, 2, out ex, out ey, out ez);
			Assert.Equal(4, ez);
		}

		[Fact]
		public void PullStopsNextToTheMonster() {
			Grid grid = new Grid();
			int ex, ey, ez;
			int steps = grid.Pull(16, 10, 10, 10, 5, out ex, out ey, out ez);
			Assert.Equal(5, steps);
			Assert.Equal((11, 10), (ex, ey));
		}

		[Fact]
		public void PullStopsAtAWallBetweenTargetAndMonster() {
			//monster inside a house (x <= 12), wall along x = 13, target outside at x = 16
			Grid grid = new Grid();
			for (int y = 0; y < 30; y++) {
				grid.wall[13, y] = true;
			}
			int ex, ey, ez;
			grid.Pull(16, 10, 10, 10, 5, out ex, out ey, out ez);
			Assert.Equal(14, ex); //dragged up to the wall, not through it
		}
	}
}
