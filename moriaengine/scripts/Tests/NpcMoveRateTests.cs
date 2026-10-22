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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side setup/report helpers for #590's protocol test
	/// (Tests/Protocol/NpcMoveRateTests.cs) - spawns two otherwise-identical NPCs (one with
	/// NPCDef.MoveRate=6, one left at the unconfigured 3.0 baseline), sends both toward the
	/// same distant goal point via the existing PetGoalPoint/"GoingToPoint" AI state, and
	/// reports each one's real displacement after a live wait - proving MoveRate actually
	/// drives NPCAiTimer's step-budget accumulator, not just that the data field parses.
	/// </summary>
	public static class NpcMoveRateTests {
		private static readonly TagKey startXTK = TagKey.Acquire("_moveRateTestStartX_");
		private static readonly TagKey startYTK = TagKey.Acquire("_moveRateTestStartY_");

		[SteamFunction]
		public static void TestSpawnMoveRateNpcs(Character self, ScriptArgs sa) {
			NPCDef fastDef = (NPCDef) ThingDef.GetByDefname("npc_test_moverate_fast");
			NPCDef baselineDef = (NPCDef) ThingDef.GetByDefname("npc_test_moverate_baseline");

			NPC fast = (NPC) fastDef.Create(self.X, self.Y, self.Z, self.M);
			NPC baseline = (NPC) baselineDef.Create(self.X, self.Y, self.Z, self.M);

			//Far enough along open, already-verified-clear ground (self was teleported there
			//first, see TestTeleportToOpenGround) that neither NPC can ever arrive within this
			//test's short wait window - Think()'s GoingToPoint branch just keeps stepping
			//toward it every tick regardless of whether it's ultimately reachable.
			Point3D goal = new Point3D(self.X + 40, self.Y, self.Z);

			fast.SetTag(startXTK, fast.X);
			fast.SetTag(startYTK, fast.Y);
			fast.PetGoalPoint = goal;

			baseline.SetTag(startXTK, baseline.X);
			baseline.SetTag(startYTK, baseline.Y);
			baseline.PetGoalPoint = goal;

			Console.WriteLine("MOVERATE_TEST_SPAWNED: fastUid=" + fast.Uid + " baselineUid=" + baseline.Uid);
		}

		[SteamFunction]
		public static void TestReportNpcDisplacement(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC npc = Thing.UidGetThing(uid) as NPC;
			int displacement = -1;
			if (npc != null) {
				object startXTag = npc.GetTag(startXTK);
				object startYTag = npc.GetTag(startYTK);
				if ((startXTag is int startX) && (startYTag is int startY)) {
					displacement = Point2D.GetSimpleDistance(npc.X, npc.Y, startX, startY);
				}
			}
			Console.WriteLine("MOVERATE_TEST_DISPLACEMENT: uid=" + uid + " displacement=" + displacement);
		}
	}
}
