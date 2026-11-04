using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - the keep conquest's pieces, which Erebor's GMs placed and the archive doesn't keep: a war declarator in
	/// each realm's capital, and the realm's war lieutenant beside it. Spots chosen here, unreviewed.</summary>
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
