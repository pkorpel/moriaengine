using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1324 - Map.GetFixedZ under terrain: in one of Erebor's halls under the mountain the land tile lies far above the floor;
	/// it's a roof there, not where one stands. Tests/Protocol/FixedZTests.cs.</summary>
	public static class FixedZTests {
		private static void Report(string name, bool ok) {
			Console.WriteLine("FIXEDZ_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestFixedZ(Character self) {
			Point4D erebor = WorldEvents.CentreOf("Erebor");
			Map map = Map.GetMap(erebor.M);
			int land = map.GetTileZ(3158, 968);
			int hall = map.GetFixedZ(new Point3D(3158, 968, 40));
			Report("theHallUnderTheMountain", (land > 40 + 8) && (hall == 40) && (map.GetFixedZ(new Point3D(3158, 968, 50)) == 40) //only the floor gives 40 from 50
				&& (map.GetFixedZ(new Point3D(3158, 968, 30)) == 40)); //below the floor: up onto it, not the mountain
			self.Go(3158, 968, 40, erebor.M);
			Report("goKeepsOneInTheHall", self.Z == 40);
			//the old cases: #1148's house floor above the street, and the street below it
			Map main = Map.GetMap(0);
			int street = main.GetTileZ(2946, 1229);
			Report("aFloorAboveTheGround", (main.GetFixedZ(new Point3D(2946, 1229, 45)) == 45) && (main.GetFixedZ(new Point3D(2946, 1229, street)) == street)
				&& (main.GetFixedZ(new Point3D(2946, 1229, street - 20)) == street)); //below the ground: up onto it, as Sphere
			Console.WriteLine("FIXEDZ_DONE land=" + land + " hall=" + hall + " z=" + self.Z);
		}
	}
}
