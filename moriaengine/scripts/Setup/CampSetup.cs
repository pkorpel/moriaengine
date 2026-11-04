using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - the labour camp's guard (c_h_strazce_lagru, spk_strazcelagru). Erebor's GMs placed it and the archive
	/// has no spot, so it stands at the camp area's own point, on plane 1. A GM can move it.</summary>
	public sealed class CampSetup : WorldSetup {
		public override string Name {
			get {
				return "Labour camp";
			}
		}

		[SteamFunction]
		public static void SetupCamp(Character self) {
			new CampSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Point4D spot = Camp.GuardSpot;
			if (spot == null) {
				gm.SysMessage("No " + Camp.Area + " area, no camp guard placed.");
				return;
			}
			pieces.Npc("camp/guard", "c_h_strazce_lagru", new[] { (int) spot.X, (int) spot.Y, (int) spot.Z }, spot.M, Realm.None, true, WorldPieces.StandsStill);
		}
	}
}
