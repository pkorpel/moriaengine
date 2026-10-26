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

	/// <summary>
	/// Server-side helpers for the real user-reported bug: an NPC spellcaster walled in with
	/// i_wall_stone could still see through the walls and cast at/damage a target outside them.
	/// Root cause: Map.IsLosBlockedAt (GameCore/Regions/LineOfSightImpl.cs) deliberately checked
	/// only terrain and pre-baked map statics, never real dynamically-placed Item instances -
	/// i_wall_stone IS real, already-placeable content, just never wired into the LOS blocking
	/// check (movement already handled it correctly - only sight through one was broken).
	///
	/// Tests the exact mechanism TryCastSpell/Fight actually call (Character.CanInteractWith ->
	/// CanSeeLOS -> Map.CanSeeLosFromTo) directly against two throwaway NPCs with a wall placed
	/// between them, rather than driving it through the full NPC AI tick loop - CombatDepthTests
	/// (the file that already covers NPC-spellcaster-AI scenarios) is already known-flaky under
	/// real-time AI ticking in this sandbox; this is deterministic and isolates the actual fixed
	/// code path.
	/// </summary>
	public static class LineOfSightBlockingTests {
		[SteamFunction]
		public static void TestCheckLosThroughPlacedWall(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyItems(self, (ItemDef) ThingDef.GetByDefname("i_wall_stone"), 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC caster = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC target = (NPC) npcDef.Create(self.X + 6, self.Y, self.Z, self.M);

			bool openBeforeWall = caster.CanInteractWith(target).Allow;

			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			wallDef.Create(self.X + 3, self.Y, self.Z, self.M);

			bool openAfterWall = caster.CanInteractWith(target).Allow;

			Console.WriteLine("LOS_WALL_TEST: openBeforeWall=" + openBeforeWall + " openAfterWall=" + openAfterWall);
		}
	}
}
