using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - the training arena's pieces, which Erebor's GMs placed and the archive doesn't keep: the Umbar Company's
	/// agent in each realm's capital, and in each bojova arena its guard and the red exit gate. Spots chosen here, unreviewed.</summary>
	public sealed class ArenaSetup : WorldSetup {
		private const int ExitGateHue = 0x21; //"Tou cervenou branou."

		public override string Name {
			get {
				return "Arena";
			}
		}

		[SteamFunction]
		public static void SetupArena(Character self) {
			new ArenaSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Agent(pieces, gm, "a_Minas_Tirith", "gondor");
			Agent(pieces, gm, "a_Barad_dur", "mordor");
			InArena(pieces, gm, Arena.GondorArena, "gondor");
			InArena(pieces, gm, Arena.MordorArena, "mordor");
		}

		private static void Agent(WorldPieces pieces, Character gm, string city, string realmName) {
			FlaggedRegion area = StaticRegion.GetByDefname(city) as FlaggedRegion;
			int[] spot = (area == null) ? null : TownSetup.SpotIn(area, gm, 7);
			if (spot == null) {
				gm.SysMessage("No spot for the arena agent in " + city + ".");
				return;
			}
			pieces.Npc("arena/" + realmName + "/agent", "c_arena_obchodnik", spot, area.P.M, Realm.None, true, WorldPieces.StandsStill);
		}

		private static void InArena(WorldPieces pieces, Character gm, string arenaName, string realmName) {
			FlaggedRegion arena = StaticRegion.GetByDefname(arenaName) as FlaggedRegion;
			int[] gateSpot = (arena == null) ? null : TownSetup.SpotIn(arena, gm, 0);
			int[] guardSpot = (arena == null) ? null : TownSetup.SpotIn(arena, gm, 1);
			if ((gateSpot == null) || (guardSpot == null)) {
				gm.SysMessage("No spots in " + arenaName + ".");
				return;
			}
			pieces.Item("arena/" + realmName + "/exit", Arena.ExitGateDef, gateSpot, arena.P.M, item => item.Color = ExitGateHue);
			pieces.Npc("arena/" + realmName + "/guard", "c_arena_strazce", guardSpot, arena.P.M, Realm.None, true, WorldPieces.StandsStill);
		}
	}
}
