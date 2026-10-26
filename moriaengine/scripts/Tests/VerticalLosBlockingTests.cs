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

namespace SteamEngine.CompiledScripts {

	/// <summary>Real-user-reported: an NPC on an upper floor directly above (same X/Y column,
	/// different Z) an enemy on the floor below could still cast/attack straight through the
	/// floor. Root cause: Map.CanSeeLosFromTo (GameCore/Regions/LineOfSightImpl.cs) has an early
	/// return "if ((from.X == to.X) && (from.Y == to.Y)) return true" with the reasoning
	/// "nothing horizontal to block" - true for a WALL (a vertical plane that needs horizontal
	/// distance to intersect a horizontal sightline) but wrong for a FLOOR/roof (a horizontal
	/// plane that blocks a perfectly VERTICAL sightline at that exact column). Reproduced here
	/// with a real i_wall_stone placed at the same column, between the two Z values - the same
	/// deterministic mechanism as LineOfSightBlockingTests.cs, just stacked instead of
	/// offset.</summary>
	public static class VerticalLosBlockingTests {
		[SteamFunction]
		public static void TestCheckLosThroughSameColumnFloor(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyItems(self, (ItemDef) ThingDef.GetByDefname("i_wall_stone"), 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC lower = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC upper = (NPC) npcDef.Create(self.X, self.Y, self.Z + 20, self.M);

			bool openBeforeFloor = lower.CanInteractWith(upper).Allow;

			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			wallDef.Create(self.X, self.Y, self.Z + 10, self.M);

			bool openAfterFloor = lower.CanInteractWith(upper).Allow;

			Console.WriteLine("VERTICAL_LOS_TEST: openBeforeFloor=" + openBeforeFloor + " openAfterFloor=" + openAfterFloor);
		}

		/// <summary>Real-user-reported (#692, observation 2): a ranged/caster NPC standing
		/// outside and below a slightly-elevated static house (attacker Z=0, target+wall Z=2)
		/// could attack straight through the wall. Root cause and fix: see
		/// IsLosBlockedAt's own doc comment in GameCore/Regions/LineOfSightImpl.cs - the wall's
		/// blocking Z-band now extends down to the real (flat, unmodified) terrain Z at its own
		/// column, closing the gap a ray starting below the wall's registered base used to slip
		/// through.</summary>
		[SteamFunction]
		public static void TestCheckLosThroughElevatedWall(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyItems(self, (ItemDef) ThingDef.GetByDefname("i_wall_stone"), 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC outside = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC inside = (NPC) npcDef.Create(self.X + 6, self.Y, self.Z + 2, self.M);

			bool openBeforeWall = outside.CanInteractWith(inside).Allow;

			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			wallDef.Create(self.X + 3, self.Y, self.Z + 2, self.M);

			bool openAfterWall = outside.CanInteractWith(inside).Allow;

			Console.WriteLine("ELEVATED_WALL_LOS_TEST: openBeforeWall=" + openBeforeWall + " openAfterWall=" + openAfterWall);
		}
	}
}
