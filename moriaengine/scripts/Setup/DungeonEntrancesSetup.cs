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

	/// <summary>#850 - two-way gates into dungeons. Erebor's were placed by GMs and aren't in the archive. A gate stands a few
	/// steps from where it leads back to, so nobody arrives on one; t_telepad doesn't fire on arriving by teleport anyway.</summary>
	public sealed class DungeonEntrancesSetup : WorldSetup {
		public const string GateDef = "i_moongate_red";
		private const int StepsFromSpawnPoint = 2;

		public override string Name {
			get {
				return "Dungeon entrances";
			}
		}

		[SteamFunction]
		public static void SetupDungeonEntrances(Character self) {
			new DungeonEntrancesSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			PlaceEntrance(pieces, gm, "ered-mithrin", "a_Ered_Mithrin_vstup", "a_Ered_Mithrinska_Jeskyne");
		}

		private static void PlaceEntrance(WorldPieces pieces, Character gm, string name, string outsideRegion, string insideRegion) {
			Point4D outside = StaticRegion.GetByDefname(outsideRegion).P;
			Point4D inside = StaticRegion.GetByDefname(insideRegion).P;
			int[] outsideGate = GateSpot(outside, gm);
			int[] insideGate = GateSpot(inside, gm);
			if ((outsideGate == null) || (insideGate == null)) {
				gm.SysMessage("No way to walk from the spawn point of " + outsideRegion + " or " + insideRegion + ", no gates placed there.");
				return;
			}
			pieces.Item("entrance/" + name + "/in", GateDef, outsideGate, outside.M, gate => gate.MoreP = inside);
			pieces.Item("entrance/" + name + "/out", GateDef, insideGate, inside.M, gate => gate.MoreP = outside);
		}

		//null if the spawn point is a tile nobody can leave, which a walk just returns (#841)
		private static int[] GateSpot(Point4D spawnPoint, Character gm) {
			int[] spot = WorldSpots.WalkableRing(Map.GetMap(spawnPoint.M), spawnPoint, gm.MovementSettings, StepsFromSpawnPoint)[0];
			return ((spot[0] == spawnPoint.X) && (spot[1] == spawnPoint.Y)) ? null : spot;
		}

	}
}
