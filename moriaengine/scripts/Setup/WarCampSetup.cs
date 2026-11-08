using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1280 - Barad-dur's rival captains, Gorbag and Shagrat, a few steps either side of the city's spawn point (Erebor
	/// had no such captains; the spots are mine, unreviewed). A GM can move them.</summary>
	public sealed class WarCampSetup : WorldSetup {
		public override string Name {
			get {
				return "War camp";
			}
		}

		[SteamFunction]
		public static void SetupWarCamp(Character self) {
			new WarCampSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			StaticRegion area = StaticRegion.GetByDefname("a_Barad_dur");
			if (area == null) {
				gm.SysMessage("No a_Barad_dur area, no captains placed.");
				return;
			}
			Point4D p = area.P;
			Map map = Map.GetMap(p.M);
			int[] offsets = { -6, 6 };
			for (int i = 0; i < RivalCaptains.All.Length; i++) {
				Captain captain = RivalCaptains.All[i];
				int[] at = WorldSpots.NearestStandable(map, p.X + offsets[i], p.Y + 4, 6);
				if (at == null) {
					gm.SysMessage("No spot for " + captain.Name + ".");
				} else {
					pieces.Npc("warcamp/" + captain.Key, captain.Def, at, p.M, Realm.Mordor, true, WorldPieces.StandsStill, false); //never a GM's spawn gem nearby
				}
			}
		}
	}
}
