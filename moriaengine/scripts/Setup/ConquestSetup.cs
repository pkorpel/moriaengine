using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - the keep conquest's pieces, which Erebor's GMs placed and the archive doesn't keep: a war declarator in
	/// each realm's capital. Spots chosen here, unreviewed.</summary>
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
			pieces.Item("conquest/" + Conquest.RealmName(realm).ToLowerInvariant() + "/declarator", "i_war_declarator", spot, area.P.M, item => ((WarDeclarator) item).SetRealm(realm));
		}
	}
}
