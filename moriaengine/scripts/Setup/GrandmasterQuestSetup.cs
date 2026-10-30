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

using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833, #839 - the Grandmaster quest's pieces. The archive defines Rinald's cave, the regions of the story and, in the
	/// journal's texts, roughly where things are: Elvira in the forest east of the Ice Plains (a_Severni_Spoust), Elmira's
	/// cage by the rock face a little way off, with the cave's entrance, the root of Este at the far end of the dark cave
	/// (see GrandmasterQuest.RootCaveMouth). Exact tiles are proposals, as are Elqira beside both Expert stones and the
	/// envoys at the spawn points of Minas Tirith and Rhun.</summary>
	public sealed class GrandmasterQuestSetup : WorldSetup {
		public override string Name {
			get {
				return "Grandmaster quest";
			}
		}

		[SteamFunction]
		public static void SetupGrandmasterQuest(Character self) {
			new GrandmasterQuestSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Map world = Map.GetMap(0);
			IMovementSettings walker = gm.MovementSettings;

			PlaceStill(pieces, "elqira/gondor", GrandmasterQuest.ElqiraDef, WorldSpots.NearestStandable(world, 2769, 2763, 5), 0);
			PlaceStill(pieces, "elqira/mordor", GrandmasterQuest.ElqiraDef, WorldSpots.NearestStandable(world, 3721, 2955, 5), 0);
			PlaceStill(pieces, "elvira", GrandmasterQuest.ElviraDef, WorldSpots.NearestStandable(world, 4004, 512, 10), 0);

			int[] elmiraAt = WorldSpots.NearestStandable(world, 4104, 530, 6);
			PlaceStill(pieces, "elmira", GrandmasterQuest.ElmiraDef, elmiraAt, 0);
			List<int[]> aroundElmira = WorldSpots.WalkableRing(world, new Point3D(elmiraAt[0], elmiraAt[1], elmiraAt[2]), walker, 3);
			PlaceFixed(pieces, "cagedoor", GrandmasterQuest.CageDoorDef, aroundElmira[0], 0);
			Item entrance = PlaceFixed(pieces, "cave/entrance", GrandmasterQuest.CaveEntranceDef, aroundElmira[aroundElmira.Count / 2], 0);

			this.PlaceCave(pieces, entrance, walker);
			this.PlaceRoot(pieces, world, walker);

			Point4D tirith = StaticRegion.GetByDefname("a_Minas_Tirith").P;
			PlaceStill(pieces, "envoy/gondor", GrandmasterQuest.GondorEnvoyDef, WorldSpots.NearestStandable(Map.GetMap(tirith.M), tirith.X + 2, tirith.Y, 6), tirith.M);
			Point4D rhun = StaticRegion.GetByDefname("a_Rhun").P;
			PlaceStill(pieces, "envoy/mordor", GrandmasterQuest.MordorEnvoyDef, WorldSpots.NearestStandable(world, rhun.X + 2, rhun.Y, 6), 0);
		}

		private void PlaceCave(WorldPieces pieces, Item entrance, IMovementSettings walker) {
			Point4D cave = StaticRegion.GetByDefname(GrandmasterQuest.CaveRegion).P;
			Map caveMap = Map.GetMap(cave.M);
			int[] rinaldAt = WorldSpots.WalkableRing(caveMap, cave, walker, 30)[0];
			NPC rinald = pieces.Npc(GrandmasterQuest.RinaldPieceId, GrandmasterQuest.RinaldDef, rinaldAt, cave.M, GrandmasterQuest.PrepareRinald);
			GrandmasterQuest.LinkCave(entrance, pieces.Item(Id("cave/exit"), GrandmasterQuest.CaveExitDef, WorldSpots.WalkableRing(caveMap, cave, walker, 3)[0], cave.M, Fix), rinald, rinaldAt, cave.M);
			pieces.SpawnGem(Id("cave/hounds"), new SpawnInfo(4, (IThingFactory) ThingDef.GetByDefname("c_hellhound"), 20, 40, 8),
				WorldSpots.WalkableRing(caveMap, new Point3D(rinaldAt[0], rinaldAt[1], rinaldAt[2]), walker, 3)[0], cave.M);
		}

		private void PlaceRoot(WorldPieces pieces, Map world, IMovementSettings walker) {
			int[] rootAt = WorldSpots.WalkableRing(world, GrandmasterQuest.RootCaveMouth, walker, int.MaxValue, GrandmasterQuest.RootCave)[0]; //the end of the dark cave
			PlaceFixed(pieces, "root", GrandmasterQuest.RootDef, rootAt, 0);
			Point3D rootPoint = new Point3D(rootAt[0], rootAt[1], rootAt[2]);
			List<int[]> aroundRoot = WorldSpots.WalkableRing(world, rootPoint, walker, 6, GrandmasterQuest.RootCave);
			for (int i = 0; i < 4; i++) {
				PlaceFixed(pieces, "root/alarm" + i, GrandmasterQuest.AlarmDef, aroundRoot[i * aroundRoot.Count / 4], 0);
			}
			pieces.SpawnGem(Id("root/guards"), new SpawnInfo(3, (IThingFactory) ThingDef.GetByDefname("c_q000_phoenix"), 20, 40, 8),
				WorldSpots.WalkableRing(world, rootPoint, walker, 2, GrandmasterQuest.RootCave)[0], 0);
		}

		private static string Id(string name) {
			return "grandmaster/" + name;
		}

		private static void Fix(Item item) {
			item.SetAttribute(ItemAttributes.attr_move_never, true);
		}

		private static void PlaceStill(WorldPieces pieces, string name, string defname, int[] at, byte m) {
			pieces.Npc(Id(name), defname, at, m, npc => {
				npc.Flag_Invulnerable = true; //Erebor's @create
				NPCAiTimer.GetFor(npc).WanderLeash = 0;
			});
		}

		private static Item PlaceFixed(WorldPieces pieces, string name, string defname, int[] at, byte m) {
			return pieces.Item(Id(name), defname, at, m, Fix);
		}
	}
}
