using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - the keep conquest's pieces, which Erebor's GMs placed and the archive doesn't keep: a war declarator in
	/// each realm's capital, and the realm's war lieutenant beside it; a palantir, its cash box and a captain stone in each keep. Spots chosen
	/// here, unreviewed.</summary>
	public sealed class ConquestSetup : WorldSetup {
		public override string Name {
			get {
				return "Conquest";
			}
		}

		[SteamFunction]
		public static void SetupConquest(Character self) {
			new ConquestSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Declarator(pieces, gm, "a_Minas_Tirith", Realm.Gondor);
			Declarator(pieces, gm, "a_Barad_dur", Realm.Mordor);
			Palantir(pieces, gm, "Gondor_1", "a_Osgiliath_zapadni_breh");
			Palantir(pieces, gm, "Gondor_2", "a_Vychodni_Marka");
			Palantir(pieces, gm, "Mordor_1", "a_Osgiliath_vychodni_breh");
			Palantir(pieces, gm, "Mordor_2", "a_Minas_Morgul");
		}

		/// <summary>TownSetup.SpotIn, or from the middle of the area's first rectangle: Minas Morgul's spawn point lies
		/// outside its own rectangle.</summary>
		internal static int[] SpotIn(FlaggedRegion area, Character gm, int nth) {
			int[] spot = TownSetup.SpotIn(area, gm, nth);
			if ((spot != null) || (area.Rectangles.Count == 0)) {
				return spot;
			}
			ImmutableRectangle rect = area.Rectangles[0];
			Map map = Map.GetMap(area.P.M);
			int x = (rect.MinX + rect.MaxX) / 2, y = (rect.MinY + rect.MaxY) / 2;
			Point4D middle = new Point4D(x, y, map.GetTileZ(x, y), area.P.M);
			int seen = 0;
			foreach (int[] candidate in WorldSpots.WalkableRing(map, middle, gm.MovementSettings, 3 + (2 * nth), rect)) {
				if ((RegionEventSystem.AreaOf(map.GetRegionFor(candidate[0], candidate[1])) == area) && (seen++ >= nth)) {
					return candidate;
				}
			}
			return null;
		}

		/// <summary>In the keep's first area (Erebor's d_area_&lt;fort&gt;_1), the cash box beside it.</summary>
		private static void Palantir(WorldPieces pieces, Character gm, string fort, string areaName) {
			FlaggedRegion area = StaticRegion.GetByDefname(areaName) as FlaggedRegion;
			int[] spot = (area == null) ? null : SpotIn(area, gm, 0);
			int[] boxSpot = (area == null) ? null : SpotIn(area, gm, 1);
			if ((spot == null) || (boxSpot == null)) {
				gm.SysMessage("No spot for the palantir of " + fort + " in " + areaName + ".");
				SteamEngine.Common.Logger.WriteWarning("ConquestSetup: no spot for the palantir of " + fort + " in " + areaName);
				return;
			}
			string id = "conquest/" + fort.ToLowerInvariant();
			pieces.Item(id + "/palantir", "i_fortress_palantir", spot, area.P.M, item => ((FortressPalantir) item).SetFort(fort));
			pieces.Item(id + "/cashbox", "i_fort_cashbox", boxSpot, area.P.M, item => ((FortCashBox) item).Fort = fort);
			int[] stoneSpot = SpotIn(area, gm, 2);
			if (stoneSpot == null) {
				gm.SysMessage("No spot for the captain stone of " + fort + " in " + areaName + ".");
				return;
			}
			string stoneDef = fort.StartsWith("Mordor", System.StringComparison.Ordinal) ? "i_mordorcaptainstone" : "i_captainstone";
			pieces.Item(id + "/captainstone", stoneDef, stoneSpot, area.P.M, item => ((CaptainStone) item).SetFort(fort));
		}

		private static void Declarator(WorldPieces pieces, Character gm, string city, Realm realm) {
			FlaggedRegion area = StaticRegion.GetByDefname(city) as FlaggedRegion;
			int[] spot = (area == null) ? null : TownSetup.SpotIn(area, gm, 8);
			if (spot == null) {
				gm.SysMessage("No spot for the war declarator in " + city + ".");
				return;
			}
			string realmName = Conquest.RealmName(realm).ToLowerInvariant();
			pieces.Item("conquest/" + realmName + "/declarator", "i_war_declarator", spot, area.P.M, item => ((WarDeclarator) item).SetRealm(realm));
			int[] lieutenantSpot = TownSetup.SpotIn(area, gm, 9);
			if (lieutenantSpot == null) {
				gm.SysMessage("No spot for the war lieutenant in " + city + ".");
				return;
			}
			pieces.Npc("conquest/" + realmName + "/lieutenant", (realm == Realm.Gondor) ? WarLieutenant.GondorDef : WarLieutenant.MordorDef, lieutenantSpot, area.P.M, realm, true, WorldPieces.StandsStill);
		}
	}
}
