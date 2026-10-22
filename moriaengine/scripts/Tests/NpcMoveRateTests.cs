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

		/// <summary>#599 - simulates a GM editing MoveRate on one specific already-spawned
		/// NPC via .info (which reflects over the live NPC instance, not its NPCDef) - sets
		/// the per-instance override directly and resets the displacement baseline so the
		/// test can measure movement under the NEW rate cleanly, not polluted by whatever
		/// ground was already covered under the old one.</summary>
		[SteamFunction]
		public static void TestOverrideNpcMoveRate(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			uint uid = Convert.ToUInt32(parts[0], CultureInfo.InvariantCulture);
			double newRate = Convert.ToDouble(parts[1], CultureInfo.InvariantCulture);
			NPC npc = Thing.UidGetThing(uid) as NPC;
			npc.MoveRate = newRate;
			npc.SetTag(startXTK, npc.X);
			npc.SetTag(startYTK, npc.Y);
			Console.WriteLine("MOVERATE_TEST_OVERRIDDEN: uid=" + uid + " newInstanceRate=" + npc.MoveRate);
		}

		//Same dual-shape ScriptArgs problem documented elsewhere in this codebase - a
		//genuinely single numeric argument reaching an elevated account via the full LScript
		//parser is a boxed value in Argv[0], not a pre-split string; only a multi-token
		//un-split string needs splitting here.
		private static string[] SplitArgs(ScriptArgs sa) {
			if ((sa.Argv.Length == 1) && (sa.Argv[0] is string single)) {
				return single.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			}
			string[] result = new string[sa.Argv.Length];
			for (int i = 0; i < sa.Argv.Length; i++) {
				result[i] = Convert.ToString(sa.Argv[i], CultureInfo.InvariantCulture);
			}
			return result;
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
