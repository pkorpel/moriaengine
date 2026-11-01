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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#843 - server-side helpers for Tests/Protocol/WorldSetupTests.cs.</summary>
	public static class WorldSetupTests {
		private static readonly ImmutableRectangle eastPlains = new ImmutableRectangle(3970, 495, 4125, 545);
		private static int movedX, movedY, strayUid;

		private static int Count(string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			int count = 0;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing.Def == def) && !thing.IsDeleted) {
					count++;
				}
			}
			return count;
		}

		private static Thing FindOne(string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			foreach (Thing thing in Thing.AllThings) {
				if ((thing.Def == def) && !thing.IsDeleted) {
					return thing;
				}
			}
			return null;
		}

		/// <summary>What a GM might do between two runs: moves Elvira, deletes the cage door, and leaves an unrelated item
		/// where a piece stands. A Lenastir also loses its id, like a piece placed before there were ids.</summary>
		[SteamFunction]
		public static void TestWorldSetupEdit(Character self) {
			Thing elvira = FindOne(GrandmasterQuest.ElviraDef);
			int[] to = WorldSpots.NearestStandable(elvira.GetMap(), 3950, 505, 8); //far from her spot: she walks about while the setup runs
			movedX = to[0];
			movedY = to[1];
			((Character) elvira).Go(to[0], to[1], to[2], elvira.M);
			Thing door = FindOne(GrandmasterQuest.CageDoorDef);
			Thing stray = ThingDef.GetByDefname("i_q000_caveExit").Create(door.X, door.Y, door.Z, door.M);
			strayUid = stray.Uid;
			door.Delete();
			FindOne(MasterQuest.LenastirDef).RemoveTag(TagKey.Acquire("_worldPieceId_"));
			((Item) FindOne(ExpertQuest.SignDef)).SetAttribute(ItemAttributes.attr_move_never, false); //as placed before item pieces were fixed
			Console.WriteLine("WORLD_EDIT: moved=" + to[0] + "," + to[1]);
		}

		/// <summary>Takes me beside a quest sign, and reports it.</summary>
		[SteamFunction]
		public static void TestWorldSetupGoToSign(Character self) {
			Thing sign = FindOne(ExpertQuest.SignDef);
			int[] at = WorldSpots.NearestStandable(sign.GetMap(), sign.X + 1, sign.Y, 3);
			self.Go(at[0], at[1], at[2], sign.M);
			ReportSign(self, sign);
		}

		[SteamFunction]
		public static void TestWorldSetupReportSign(Character self, ScriptArgs sa) {
			ReportSign(self, Thing.UidGetThing(int.Parse(sa.Args.Trim().Trim('"'))));
		}

		private static void ReportSign(Character self, Thing sign) {
			Item item = (Item) sign;
			Console.WriteLine("WORLD_SIGN: uid=" + sign.Uid + " at=" + sign.X + "," + sign.Y + " me=" + self.X + " " + self.Y + " " + self.Z + " onGround=" + (item.Cont == null) +
				" fixed=" + item.Flag_NonMovable + " decaying=" + ItemDecaySystem.HasActiveTimer(item));
		}

		//item pieces that a player could pick up, or that would decay away
		private static int LoosePieces() {
			int loose = 0;
			TagKey idTK = TagKey.Acquire("_worldPieceId_");
			foreach (Thing thing in Thing.AllThings) {
				Item item = thing as Item;
				if ((item != null) && !item.IsDeleted && (item.GetTag(idTK) != null) && (!item.Flag_NonMovable || ItemDecaySystem.HasActiveTimer(item))) {
					loose++;
				}
			}
			return loose;
		}

		[SteamFunction]
		public static void TestWorldSetupCheck(Character self) {
			Thing elvira = FindOne(GrandmasterQuest.ElviraDef);
			Thing stray = Thing.UidGetThing(strayUid);
			Console.WriteLine("WORLD_CHECK: elviras=" + Count(GrandmasterQuest.ElviraDef) + " elviraMoved=" + (Point2D.GetSimpleDistance(elvira.X, elvira.Y, movedX, movedY) < 15) + " elviraAt=" + elvira.X + "," + elvira.Y +
				" loose=" + LoosePieces() + " doors=" + Count(GrandmasterQuest.CageDoorDef) + " strayThere=" + (stray != null) +
				" stones=" + Count(ExpertQuest.StartStoneDef) + " lenastirs=" + Count(MasterQuest.LenastirDef) + " rinalds=" + Count(GrandmasterQuest.RinaldDef) + " elqiras=" + Count(GrandmasterQuest.ElqiraDef));
		}
	}
}
