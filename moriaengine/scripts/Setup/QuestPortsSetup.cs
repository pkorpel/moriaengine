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

using System;
using System.Collections.Generic;
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
			int[] inn = WorldSpots.NearestStandable(world, 164, 2150, 8);
			PlaceStill(pieces, gm, "q006/innkeeper", Q006.InnkeeperDef, inn, 0);

			//q007: the regions its texts name; the innkeeper is q006's piece (the same NPC)
			PlaceNear(pieces, gm, "q007/haradir", Q007.HaradirDef, "a_Severni_Spoust", 3, 0);
			PlaceStill(pieces, gm, "q007/inscripter", Q007.InscripterDef, WorldSpots.NearestStandable(world, 1510, 340, 12), 0); //"in his cellar in the Northern Waste"
			PlaceNear(pieces, gm, "q007/rachel", Q007.RachelDef, "a_Slunecni_ostrov", -3, 0);
			PlaceNear(pieces, gm, "q007/smith", Q007.SmithDef, "a_Zelezny_pas", 2, 2);
			//drinking by the innkeeper's, 4 tiles or more from him: both answer "ahoj" from 3 tiles
			PlaceStill(pieces, gm, "q007/divous", Q007.DivousDef, (inn == null) ? null : WorldSpots.NearestStandable(world, inn[0] + 7, inn[1], 3), 0);
			PlaceNear(pieces, gm, "q007/rodrigo", Q007.RodrigoDef, "a_Umbar", 4, 0);
			int[] dryad = WorldSpots.NearestStandable(world, 300, 2060, 20); //the innkeeper's island, away from his inn
			if (dryad != null) {
				pieces.Npc("quests/q007/dryad", Q007.DryadDef, dryad, 0, Realm.None, false, WorldPieces.WandersAbout);
			}
			FlaggedRegion pirates = StaticRegion.GetByDefname("a_Ostrov_Piratu") as FlaggedRegion;
			int[] chest = (pirates == null) ? null : WorldSpots.NearestStandable(world, pirates.P.X + 2, pirates.P.Y, 6);
			if (chest == null) {
				gm.SysMessage("No spot for the pirates' chest.");
			} else {
				pieces.Item("quests/q007/chest", Q007.ChestDef, chest, 0);
				for (int i = 0; i < 2; i++) {
					int[] pirate = WorldSpots.NearestStandable(world, chest[0] + 3 - (6 * i), chest[1] + 2, 4);
					if (pirate != null) {
						pieces.Npc("quests/q007/pirate" + i, "c_pirat", pirate, 0, Realm.None, false, WorldPieces.WandersAbout);
					}
				}
			}
			PlaceTerlak(pieces, gm);
			PlaceTailorAndSalmon(pieces, gm);
			PlaceTombPoison(pieces, gm);
		}

		/// <summary>#1272 - q026: Andor and Roxana where Erebor's chars files say (3200,2971; 2913,1988), the Temple of Knowledge's
		/// tapestry at 3941,1503 (the journal's note), its door and book pile beside it, Dol Guldur's tile, secret wall and box in
		/// the Dol Guldur bonus area at 5396,3384 on plane 10 (the journal's note). Erebor's GMs built the temple and the room; the
		/// archive keeps neither, so these spots are only near the right places.</summary>
		internal static void PlaceTombPoison(WorldPieces pieces, Character gm) {
			Map world = Map.GetMap(0);
			PlaceStill(pieces, gm, "q026/andor", Q026.AndorDef, WorldSpots.NearestStandable(world, 3200, 2971, 8), 0);
			PlaceStill(pieces, gm, "q026/roxana", Q026.RoxanaDef, WorldSpots.NearestStandable(world, 2913, 1988, 8), 0);
			PlaceItem(pieces, gm, "q026/tapestry", Q026.TapestryDef, new Point4D(3941, 1503, 5, 0), false);
			PlaceItem(pieces, gm, "q026/door", Q026.DoorDef, new Point4D(3944, 1503, 5, 0), false);
			PlaceItem(pieces, gm, "q026/books", Q026.BookPileDef, new Point4D(3944, 1499, 5, 0), false);
			PlaceItem(pieces, gm, "q026/alarm", Q026.AlarmDef, new Point4D(5396, 3384, 0, 10), false);
			PlaceItem(pieces, gm, "q026/wall", Q026.WallDef, new Point4D(5398, 3384, 0, 10), false);
			PlaceItem(pieces, gm, "q026/box", Q026.BoxDef, new Point4D(5396, 3381, 0, 10), false);
		}

		/// <summary>#1270 - q009's tailor in Edoras (Erebor's spot: none written down), q018's three NPCs where Erebor's script header
		/// says they stood, the golden rune in Osgiliath's eastern ruins and the wheel in the Pits of Death, at the spots its items
		/// teleport to.</summary>
		internal static void PlaceTailorAndSalmon(WorldPieces pieces, Character gm) {
			Map world = Map.GetMap(0);
			PlaceNear(pieces, gm, "q009/talathwen", Q009.TailorDef, "a_Edoras", 5, 3);
			PlaceStill(pieces, gm, "q018/fisherman", Q018.FishermanDef, WorldSpots.NearestStandable(world, 3916, 3609, 8), 0);
			PlaceStill(pieces, gm, "q018/mirrana", Q018.MirranaDef, WorldSpots.NearestStandable(world, 3014, 2467, 8), 0);
			PlaceStill(pieces, gm, "q018/elnassir", Q018.ElnassirDef, WorldSpots.NearestStandable(world, 2462, 3324, 8), 0);
			PlaceItem(pieces, gm, "q018/rune", Q018.RuneDef, Q018.RuneSpot, true); //Erebor's height: Osgiliath's ruins lie under other floors
			PlaceItem(pieces, gm, "q018/wheel", Q018.WheelDef, Q018.WheelSpot, false);
		}

		private static void PlaceItem(WorldPieces pieces, Character gm, string id, string defname, Point4D near, bool withZ) {
			Map map = Map.GetMap(near.M);
			int[] at = (withZ && map.CanFit(near.X, near.Y, near.Z, 16, true, false, true)) ? new int[] { near.X, near.Y, near.Z }
				: WorldSpots.NearestStandable(map, near.X, near.Y, 4);
			if (at == null) {
				gm.SysMessage("No spot for " + id + ".");
				return;
			}
			pieces.Item("quests/" + id, defname, at, near.M);
		}

		/// <summary>#1268 - q017: the entry gate at the first of its four dungeon spots (it moves after each entry), the exit gate at the
		/// far end of Gargamel's dungeon, Gargamel a few steps in, the collector by Minas Tirith's spawn point (Erebor's spots: none).</summary>
		private const int MinDungeonLength = 10;

		internal static void PlaceTerlak(WorldPieces pieces, Character gm) {
			Point4D gateAt = Q017.GateSpots[0];
			int[] gate = WorldSpots.NearestStandable(Map.GetMap(gateAt.M), gateAt.X, gateAt.Y, 4);
			if (gate != null) {
				pieces.Item("quests/q017/entrygate", Q017.EntryGateDef, gate, gateAt.M);
			}
			Map dungeon = Map.GetMap(Q017.DungeonStart.M);
			ImmutableRectangle kobka = new ImmutableRectangle(5695, 101, 5722, 122);
			List<int[]> far = WorldSpots.WalkableRing(dungeon, Q017.DungeonStart, gm.MovementSettings, int.MaxValue, kobka);
			int length = Math.Max(Math.Abs(far[0][0] - Q017.DungeonStart.X), Math.Abs(far[0][1] - Q017.DungeonStart.Y));
			if (length >= MinDungeonLength) { //a ring never comes back empty: it holds the start itself when nothing can be walked
				List<int[]> inside = WorldSpots.WalkableRing(dungeon, Q017.DungeonStart, gm.MovementSettings, length / 2, kobka);
				pieces.Item("quests/q017/exitgate", Q017.ExitGateDef, far[0], Q017.DungeonStart.M);
				pieces.Npc("quests/q017/gargamel", Q017.GargamelDef, inside[0], Q017.DungeonStart.M, Realm.None, false, WorldPieces.WandersAbout);
			} else {
				gm.SysMessage("Gargamel's dungeon can't be walked from its start: no exit gate, no Gargamel.");
			}
			Point4D tirith = StaticRegion.GetByDefname("a_Minas_Tirith").P;
			PlaceStill(pieces, gm, "q017/collector", Q017.CollectorNpcDef, WorldSpots.NearestStandable(Map.GetMap(tirith.M), tirith.X - 3, tirith.Y - 8, 6), tirith.M);
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
