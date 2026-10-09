using System.Collections.Generic;
using System.Linq;
using SteamEngine.Regions;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Map.CreateLosPath (the Bresenham-style path tracer CanSeeLosFromTo now actually uses -
	/// see issue #24) had no automated coverage at all before this, despite already existing.
	/// This exercises the pure geometry only - it needs no real map/tile data, unlike the
	/// blocking check itself (Map.IsLosBlockedAt), which depends on real .mul data and is only
	/// practically verifiable via the full-boot tier or the existing in-game GM tool
	/// (scripts/Tests/CanSeeLOSTest.scp's test_los function).
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class LineOfSightTests {
		[Fact]
		public void CreateLosPath_StraightHorizontalLine_ReachesDestination() {
			var org = new Point3D(100, 100, 0);
			var dest = new Point3D(110, 100, 0);

			List<Point3D> path = ToListAndDispose(Map.CreateLosPath(org, dest));

			Assert.NotEmpty(path);
			Assert.Contains(path, p => p.X == dest.X);
			Assert.All(path, p => Assert.InRange(p.X, org.X, dest.X));
			// losPathWidth (0.2) means points can be 1 tile off the exact line on the Y axis.
			Assert.All(path, p => Assert.InRange(p.Y, org.Y - 1, org.Y + 1));
		}

		[Fact]
		public void CreateLosPath_StraightVerticalLine_ReachesDestination() {
			var org = new Point3D(100, 100, 0);
			var dest = new Point3D(100, 108, 0);

			List<Point3D> path = ToListAndDispose(Map.CreateLosPath(org, dest));

			Assert.NotEmpty(path);
			Assert.Contains(path, p => p.Y == dest.Y);
			Assert.All(path, p => Assert.InRange(p.Y, org.Y, dest.Y));
			Assert.All(path, p => Assert.InRange(p.X, org.X - 1, org.X + 1));
		}

		[Fact]
		public void CreateLosPath_Diagonal_ReachesDestinationWithoutThrowing() {
			var org = new Point3D(100, 100, 0);
			var dest = new Point3D(106, 106, 0);

			List<Point3D> path = ToListAndDispose(Map.CreateLosPath(org, dest));

			Assert.NotEmpty(path);
			Assert.Contains(path, p => (p.X == dest.X) && (p.Y == dest.Y));
		}

		// CreateLosPath returns a pooled PointsInLine under the IEnumerable<Point3D> interface -
		// it must be disposed (released back to its pool) once consumed.
		private static List<Point3D> ToListAndDispose(IEnumerable<Point3D> path) {
			try {
				return path.ToList();
			} finally {
				(path as System.IDisposable)?.Dispose();
			}
		}
	}
}
