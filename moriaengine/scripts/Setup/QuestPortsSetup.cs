/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1264 - the NPCs of the ported Erebor quests (#1237 phase 4), which Erebor's GMs placed and the archive doesn't keep.
	/// q006: Qworet, Unas and the innkeeper where Erebor's q006 GM page says they stood (2893,908; 3769,1490; 164,2150); Eriol had
	/// no spot written down, he stands 8 tiles west of Minas Tirith's spawn point, 10 from the
	/// Grandmaster envoy, so a greeting from close to one doesn't open both dialogs. All unreviewed.</summary>
	public sealed class QuestPortsSetup : WorldSetup {
		public override string Name {
			get {
				return "Quest ports";
			}
		}

		[SteamFunction]
		public static void SetupQuestPorts(Character self) {
			new QuestPortsSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Map world = Map.GetMap(0);
			Point4D tirith = StaticRegion.GetByDefname("a_Minas_Tirith").P;
			PlaceStill(pieces, gm, "q006/eriol", Q006.EriolDef, WorldSpots.NearestStandable(Map.GetMap(tirith.M), tirith.X - 8, tirith.Y + 4, 6), tirith.M);
			PlaceStill(pieces, gm, "q006/qworet", Q006.QworetDef, WorldSpots.NearestStandable(world, 2893, 908, 8), 0);
			PlaceStill(pieces, gm, "q006/unas", Q006.UnasDef, WorldSpots.NearestStandable(world, 3769, 1490, 8), 0);
			PlaceStill(pieces, gm, "q006/innkeeper", Q006.InnkeeperDef, WorldSpots.NearestStandable(world, 164, 2150, 8), 0);

			//q007: the regions its texts name; the innkeeper is q006's piece (the same NPC)
			PlaceNear(pieces, gm, "q007/haradir", Q007.HaradirDef, "a_Severni_Spoust", 3, 0);
			PlaceStill(pieces, gm, "q007/inscripter", Q007.InscripterDef, WorldSpots.NearestStandable(world, 1510, 340, 12), 0); //"in his cellar in the Northern Waste"
			PlaceNear(pieces, gm, "q007/rachel", Q007.RachelDef, "a_Slunecni_ostrov", -3, 0);
			PlaceNear(pieces, gm, "q007/smith", Q007.SmithDef, "a_Zelezny_pas", 2, 2);
			PlaceStill(pieces, gm, "q007/divous", Q007.DivousDef, WorldSpots.NearestStandable(world, 167, 2152, 8), 0); //drinking at the innkeeper's
			PlaceNear(pieces, gm, "q007/rodrigo", Q007.RodrigoDef, "a_Umbar", 4, 0);
			int[] dryad = WorldSpots.NearestStandable(world, 300, 2060, 20); //the innkeeper's island, away from his inn
			if (dryad != null) {
				pieces.Npc("quests/q007/dryad", Q007.DryadDef, dryad, 0, Realm.None, false, WorldPieces.WandersAbout);
			}
			FlaggedRegion pirates = StaticRegion.GetByDefname("a_Ostrov_Piratu") as FlaggedRegion;
			int[] chest = (pirates == null) ? null : WorldSpots.NearestStandable(world, pirates.P.X + 2, pirates.P.Y, 6);
			if (chest == null) {
				gm.SysMessage("No spot for the pirates' chest.");
				return;
			}
			pieces.Item("quests/q007/chest", Q007.ChestDef, chest, 0);
			for (int i = 0; i < 2; i++) {
				int[] pirate = WorldSpots.NearestStandable(world, chest[0] + 3 - (6 * i), chest[1] + 2, 4);
				if (pirate != null) {
					pieces.Npc("quests/q007/pirate" + i, "c_pirat", pirate, 0, Realm.None, false, WorldPieces.WandersAbout);
				}
			}
		}

		private static void PlaceNear(WorldPieces pieces, Character gm, string id, string defname, string region, int dx, int dy) {
			StaticRegion area = StaticRegion.GetByDefname(region);
			int[] at = (area == null) ? null : WorldSpots.NearestStandable(Map.GetMap(area.P.M), area.P.X + dx, area.P.Y + dy, 6);
			PlaceStill(pieces, gm, id, defname, at, (area == null) ? (byte) 0 : area.P.M);
		}

		private static void PlaceStill(WorldPieces pieces, Character gm, string id, string defname, int[] at, byte m) {
			if (at == null) {
				gm.SysMessage("No spot for " + id + ".");
				return;
			}
			pieces.Npc("quests/" + id, defname, at, m, Realm.None, true, WorldPieces.StandsStill); //invulnerable, as the quest NPCs are
		}
	}
}
