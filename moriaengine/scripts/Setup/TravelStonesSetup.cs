using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1035 - a coach stone at each of Erebor's 30 stops. Its stones were placed by GMs and aren't
	/// in the archive, so each stands a few steps from where the coach sets people down. A GM can move them.</summary>
	public sealed class TravelStonesSetup : WorldSetup {
		public const string StoneDef = "i_stone_travel";
		private const int StepsFromArrival = 2;

		public override string Name {
			get {
				return "Travel stones";
			}
		}

		[SteamFunction]
		public static void SetupTravelStones(Character self) {
			new TravelStonesSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			for (int i = 0; i < TravelStops.All.Length; i++) {
				TravelStop stop = TravelStops.All[i];
				int[] spot = StoneSpot(stop, gm);
				if (spot == null) {
					gm.SysMessage("No way to walk from the stop " + stop.Name + ", no stone placed there.");
				} else {
					pieces.Item("travelstone/" + TravelStops.Count(i + 1), StoneDef, spot, stop.Destination.M);
				}
			}
		}

		//null if the stop is a tile nobody can leave, which a walk just returns (#841)
		internal static int[] StoneSpot(TravelStop stop, Character gm) {
			int[] arrival = stop.Arrival;
			if (arrival == null) {
				return null;
			}
			Point3D from = new Point3D(arrival[0], arrival[1], arrival[2]);
			List<int[]> ring = WorldSpots.WalkableRing(Map.GetMap(stop.Destination.M), from, gm.MovementSettings, StepsFromArrival);
			int[] spot = ring[0];
			return ((spot[0] == arrival[0]) && (spot[1] == arrival[1])) ? null : spot;
		}
	}
}
