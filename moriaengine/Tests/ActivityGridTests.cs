using System.Collections.Generic;
using SteamEngine.Regions;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #1201 - which areas of a map plane are awake: a player's area and its 8 neighbours, for AwakeSeconds after the
	/// last one left; asleep from the start; a callback for each area that wakes.
	/// </summary>
	public class ActivityGridTests {
		private const double Awake = 300;

		private static ActivityGrid NewGrid(List<(int, int)> woke) {
			return new ActivityGrid(10 * ActivityGrid.AreaSize, 10 * ActivityGrid.AreaSize, (ax, ay) => woke.Add((ax, ay)));
		}

		private static void WithAwakeSeconds(double seconds, System.Action test) {
			double old = ActivityGrid.AwakeSeconds;
			ActivityGrid.AwakeSeconds = seconds;
			try {
				test();
			} finally {
				ActivityGrid.AwakeSeconds = old;
			}
		}

		[Fact]
		public void APlayerWakesItsAreaAndTheNeighboursOnly() {
			WithAwakeSeconds(Awake, () => {
				List<(int, int)> woke = new List<(int, int)>();
				ActivityGrid grid = NewGrid(woke);
				Assert.False(grid.IsAreaAwake(5, 5, 0)); //asleep from the start
				Assert.False(grid.IsAwake(5 * ActivityGrid.AreaSize, 5 * ActivityGrid.AreaSize, 0));

				grid.PlayerArrived(5, 5, 10);
				Assert.Equal(9, woke.Count);
				for (int dx = -1; dx <= 1; dx++) {
					for (int dy = -1; dy <= 1; dy++) {
						Assert.Contains((5 + dx, 5 + dy), woke);
						Assert.True(grid.IsAreaAwake(5 + dx, 5 + dy, 10));
					}
				}
				Assert.False(grid.IsAreaAwake(7, 5, 10)); //two areas away
				Assert.False(grid.IsAreaAwake(3, 3, 10));
				Assert.Equal(1, grid.PlayersIn(5, 5));
				Assert.True(grid.IsAwake((6 * ActivityGrid.AreaSize) + 63, (4 * ActivityGrid.AreaSize), 10)); //tile in a neighbour

				grid.PlayerArrived(0, 0, 10); //a corner has 4 areas around it
				Assert.Equal(13, woke.Count);
			});
		}

		[Fact]
		public void AnAreaStaysAwakeForAwakeSecondsAfterTheLastPlayerLeft() {
			WithAwakeSeconds(Awake, () => {
				List<(int, int)> woke = new List<(int, int)>();
				ActivityGrid grid = NewGrid(woke);
				grid.PlayerArrived(5, 5, 10);
				grid.PlayerArrived(5, 5, 20); //a second player, no new waking
				Assert.Equal(9, woke.Count);

				grid.PlayerLeft(5, 5, 100);
				Assert.True(grid.IsAreaAwake(6, 6, 100 + Awake + 50)); //one still there
				grid.PlayerLeft(5, 5, 200);
				Assert.True(grid.IsAreaAwake(6, 6, 200 + Awake - 1));
				Assert.False(grid.IsAreaAwake(6, 6, 200 + Awake));
				Assert.Equal(0, grid.PlayersIn(5, 5));

				woke.Clear();
				grid.PlayerArrived(5, 5, 300); //back during the countdown: still awake, nothing wakes
				Assert.Empty(woke);
				grid.PlayerLeft(5, 5, 310);
				grid.PlayerArrived(5, 5, 310 + Awake + 1); //back after it: every area wakes again
				Assert.Equal(9, woke.Count);
			});
		}

		[Fact]
		public void AStepIntoTheNextAreaWakesOnlyTheNewColumn() {
			WithAwakeSeconds(Awake, () => {
				List<(int, int)> woke = new List<(int, int)>();
				ActivityGrid grid = NewGrid(woke);
				grid.PlayerArrived(5, 5, 10);
				woke.Clear();
				grid.PlayerLeft(5, 5, 20); //a sector change removes, then adds
				grid.PlayerArrived(6, 5, 20);
				Assert.Equal(3, woke.Count);
				Assert.Contains((7, 4), woke);
				Assert.Contains((7, 5), woke);
				Assert.Contains((7, 6), woke);
				Assert.True(grid.IsAreaAwake(4, 5, 20 + Awake - 1)); //left behind, counting down
				Assert.False(grid.IsAreaAwake(4, 5, 20 + Awake));
			});
		}

		[Fact]
		public void ANewAwakeTimeAppliesAtOnce() {
			List<(int, int)> woke = new List<(int, int)>();
			ActivityGrid grid = null;
			WithAwakeSeconds(Awake, () => {
				grid = NewGrid(woke);
				grid.PlayerArrived(5, 5, 1000);
				grid.PlayerLeft(5, 5, 1000);
				Assert.True(grid.IsAreaAwake(5, 5, 1010));
			});
			WithAwakeSeconds(5, () => Assert.False(grid.IsAreaAwake(5, 5, 1010))); //emptied 10 s ago, 5 s to stay awake
		}

		[Fact]
		public void ZeroAwakeSecondsTurnsSleepingOff() {
			WithAwakeSeconds(0, () => {
				List<(int, int)> woke = new List<(int, int)>();
				ActivityGrid grid = NewGrid(woke);
				Assert.False(ActivityGrid.SleepingEnabled);
				Assert.True(grid.IsAreaAwake(5, 5, 0));
				grid.PlayerArrived(5, 5, 10);
				Assert.Empty(woke);
				grid.PlayerLeft(5, 5, 20);
				Assert.True(grid.IsAreaAwake(5, 5, 1000));
			});
		}

		[Fact]
		public void ClearForgetsEveryone() {
			WithAwakeSeconds(Awake, () => {
				List<(int, int)> woke = new List<(int, int)>();
				ActivityGrid grid = NewGrid(woke);
				grid.PlayerArrived(5, 5, 10);
				grid.Clear();
				Assert.Equal(0, grid.PlayersIn(5, 5));
				Assert.False(grid.IsAreaAwake(5, 5, 10));
				Assert.True(grid.IsAreaAwake(-1, 5, 10)); //off the plane: never holds anyone back
			});
		}
	}
}
