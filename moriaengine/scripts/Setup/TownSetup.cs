using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#892 - the stones of the Gondor cities. Erebor's were placed by GMs and aren't in the
	/// archive, so these spots are proposals: a few steps from each city's spawn point, inside its map
	/// area. The tax chest comes with the stone's first use. A GM moves a stone like any other piece.</summary>
	public sealed class TownSetup : WorldSetup {
		public const string StoneDef = "i_townstone";
		private const int StepsFromSpawnPoint = 3;

		/// <summary>The cities of map areas with tag.realm=1 and tag.jmeno_mesta, by tag.jmeno_mesta. The test
		/// town London and the abandoned Dainovo kralovstvi aren't among them.</summary>
		public static readonly string[] GondorCities = {
			"Minas_Tirith", "Edoras", "Dol_Amroth", "Fornost", "Hobitin", "Erebor", "Dol", "Sede_pristavy", "Kralovstvi_lesnich_elfu"
		};

		/// <summary>The realm's stones stand in its capital, as far on as a stone's own spot isn't taken.</summary>
		public const string GeneralCity = "Minas_Tirith";
		public const string GeneralStoneDef = "i_generalstone";

		public const string ShrineDef = "i_shrine_power";
		public const string MordorCapital = "Barad-dur";

		/// <summary>Cities of the map areas with tag.realm=2. Nothing sets how many Rings there are but this.</summary>
		public static readonly string[] MordorCities = { "Barad-dur", "Nurn", "Rhun", "Harad", "Umbar" };

		public override string Name {
			get {
				return "Town stones";
			}
		}

		[SteamFunction]
		public static void SetupTowns(Character self) {
			new TownSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (string city in GondorCities) {
				FlaggedRegion area = CityArea(city);
				int[] spot = (area != null) ? SpotIn(area, gm) : null;
				if (spot == null) {
					gm.SysMessage("No spot for the stone of " + city + ", walk from its spawn point failed.");
				} else {
					pieces.Item(StonePieceId(city), StoneDef, spot, area.P.M);
				}
			}
			FlaggedRegion capital = CityArea(GeneralCity);
			int[] generalSpot = SpotIn(capital, gm, 1);
			if (generalSpot != null) {
				pieces.Item("realm/gondor/stone", GeneralStoneDef, generalSpot, capital.P.M);
			}
			PlaceShop(pieces, gm, capital, "gondor", Realm.Gondor, "alchemist", AlchemistShop.NpcDef, 5);
			PlaceShop(pieces, gm, capital, "gondor", Realm.Gondor, "prodejce", ProdejceShop.NpcDef, 6);
			this.PlaceMordor(pieces, gm);
		}

		private void PlaceMordor(WorldPieces pieces, Character gm) {
			foreach (string city in MordorCities) {
				FlaggedRegion area = CityArea(city);
				int[] spot = (area != null) ? SpotIn(area, gm) : null;
				if (spot == null) {
					gm.SysMessage("No spot for the shrine of " + city + ", walk from its spawn point failed.");
				} else {
					pieces.Item("town/" + city + "/shrine", ShrineDef, spot, area.P.M);
				}
			}
			FlaggedRegion capital = CityArea(MordorCapital);
			PlaceShop(pieces, gm, capital, "mordor", Realm.Mordor, "alchemist", AlchemistShop.NpcDef, 5);
			PlaceShop(pieces, gm, capital, "mordor", Realm.Mordor, "prodejce", ProdejceShop.NpcDef, 6);
			FlaggedRegion trial = (FlaggedRegion) StaticRegion.GetByDefname("a_Fela_Nazgul");
			int[] stoneSpot = SpotIn(capital, gm, 1);
			int[] boardSpot = SpotIn(capital, gm, 2);
			int[] gateSpot = SpotIn(capital, gm, 3);
			int[] outSpot = SpotIn(trial, gm);
			int[] leadingSpot = SpotIn(trial, gm, 2);
			if ((stoneSpot == null) || (boardSpot == null) || (gateSpot == null) || (outSpot == null) || (leadingSpot == null)) {
				gm.SysMessage("No spots for the First Nazgul's pieces.");
				return;
			}
			pieces.Item("realm/mordor/stone", "i_lordnazgulsstone", stoneSpot, capital.P.M);
			pieces.Item("realm/mordor/board", "i_lordnazgulsboard", boardSpot, capital.P.M);
			//each leads to the spawn point: arriving on the other gate would send someone straight back
			Item gate = pieces.Item("realm/mordor/gate", "i_lordnazguls_gate", gateSpot, capital.P.M, item => item.MoreP = trial.P);
			pieces.Item("realm/mordor/gate-out", "i_lordnazguls_gate_out", outSpot, trial.P.M, item => item.MoreP = capital.P);
			pieces.Item("realm/mordor/leading-shrine", "i_leading_shrine_power", leadingSpot, trial.P.M);
			((NazgulGate) gate).Refresh();
		}

		/// <summary>#1017, #1070 - the user's choice: an alchemist (c_alchymista) and a Prodejce (c_drazitel) in each realm's
		/// capital, selling to that realm.</summary>
		private static void PlaceShop(WorldPieces pieces, Character gm, FlaggedRegion capital, string realmName, Realm realm, string piece, string npcDef, int nth) {
			int[] spot = (capital == null) ? null : SpotIn(capital, gm, nth);
			if (spot == null) {
				gm.SysMessage("No spot for the " + piece + " of " + realmName + ".");
				return;
			}
			pieces.Npc("realm/" + realmName + "/" + piece, npcDef, spot, capital.P.M, shop => {
				shop.Realm = realm;
				shop.Flag_Invulnerable = true;
			});
		}

		public static string StonePieceId(string city) {
			return "town/" + city + "/stone";
		}

		/// <summary>The map area named after the city; other areas that answer to it have other names.</summary>
		public static FlaggedRegion CityArea(string city) {
			return StaticRegion.GetByDefname("a_" + city.Replace('-', '_')) as FlaggedRegion;
		}

		/// <summary>A tile `StepsFromSpawnPoint` from the area's spawn point, the `nth` of them, inside the area.</summary>
		private static int[] SpotIn(FlaggedRegion area, Character gm, int nth = 0) {
			Point4D spawnPoint = area.P;
			Map map = Map.GetMap(spawnPoint.M);
			foreach (int[] spot in WorldSpots.WalkableRing(map, spawnPoint, gm.MovementSettings, StepsFromSpawnPoint + (2 * nth))) {
				bool moved = (spot[0] != spawnPoint.X) || (spot[1] != spawnPoint.Y);
				if (moved && (RegionEventSystem.AreaOf(map.GetRegionFor(spot[0], spot[1])) == area)) {
					return spot;
				}
			}
			return null;
		}
	}
}
