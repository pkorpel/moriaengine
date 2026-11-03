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

	/// <summary>#833 - the Master quest's pieces: Lenastir beside both of Erebor's Expert stones, the teleport between the
	/// serpent pillars of Beridor Level 3, and spawn gems for the snake queen and her serpents on the Snake Islands. The
	/// archive has none of these spots.</summary>
	public sealed class MasterQuestSetup : WorldSetup {
		private const string SilverSerpent = "c_serpent_silver_mistr", GoldSerpent = "c_serpent_gold_mistr", BlackSerpent = "c_serpent_black_mistr";

		public override string Name {
			get {
				return "Master quest";
			}
		}

		[SteamFunction]
		public static void SetupMasterQuest(Character self) {
			new MasterQuestSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Map map = Map.GetMap(0);
			PlaceLenastir(pieces, map, "gondor", 2773, 2763);
			PlaceLenastir(pieces, map, "mordor", 3725, 2955);

			int[] teleportAt = WorldSpots.NearestStandable(Map.GetMap(MasterQuest.TeleportPlane), 5331, 2745, 5);
			if (teleportAt != null) {
				pieces.Item("master/teleport", MasterQuest.TeleportDef, teleportAt, MasterQuest.TeleportPlane);
			}

			int[] queenAt = WorldSpots.NearestStandable(map, 1518, 3890, 20);
			if (queenAt == null) {
				return;
			}
			pieces.SpawnGem("master/queen", new SpawnInfo(1, (IThingFactory) ThingDef.GetByDefname(MasterQuest.QueenDef), 30, 60, 5), queenAt, 0);
			int[] serpentsAt = WorldSpots.NearestStandable(map, queenAt[0] + 4, queenAt[1], 10);
			if (serpentsAt != null) {
				SpawnInfo serpents = new SpawnInfo(6, 10, 20, 12);
				foreach (string defname in new[] { SilverSerpent, GoldSerpent, BlackSerpent }) {
					serpents.Entries.Add(new SpawnEntry((IThingFactory) ThingDef.GetByDefname(defname), 1));
				}
				pieces.SpawnGem("master/serpents", serpents, serpentsAt, 0);
			}
		}

		private static void PlaceLenastir(WorldPieces pieces, Map map, string name, int x, int y) {
			int[] at = WorldSpots.NearestStandable(map, x, y, 5);
			if (at != null) {
				pieces.Npc("master/lenastir/" + name, MasterQuest.LenastirDef, at, 0, Realm.None, true); //invulnerable: Erebor's @create
			}
		}
	}
}
