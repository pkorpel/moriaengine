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

	/// <summary>Real-user-reported: c_titan_ranger (a non-caster, ranged-weapon NPC) sent to
	/// attack c_titan just stood there instead of repositioning, even though c_titan_mage (a
	/// caster) correctly walked to get within range against the same kind of obstruction.
	/// Root cause: NPCAiTimer.Fight's non-caster branch hardcoded hasLineOfSight=true, so a
	/// target nominally within flat weapon range (Point2D.GetSimpleDistance has no concept of
	/// height/walls) but with no real line of sight (e.g. blocked by a wall, or - the user's own
	/// case - standing on a different floor/elevation) never triggered approach/repositioning at
	/// all. Reproduces with a real wall placed between the two (same deterministic mechanism as
	/// LineOfSightBlockingTests.cs - i_wall_stone blocks Character.CanInteractWith/CanSeeLOS),
	/// rather than a real multi-floor map area, which this sandbox's own terrain is too
	/// inconsistent to set up reliably (see this codebase's other movement tests' own notes on
	/// that).</summary>
	public static class NpcRangedApproachTests {
		[SteamFunction]
		public static void TestRangedNpcRepositionsWhenLosBlocked(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			NPCDef rangerDef = ThingDef.GetByDefname("c_titan_ranger") as NPCDef;
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC ranger = (NPC) rangerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC titan = (NPC) titanDef.Create(self.X + 6, self.Y, self.Z, self.M);

			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			wallDef.Create(self.X + 3, self.Y, self.Z, self.M);

			bool hasLineOfSight = ranger.CanInteractWith(titan).Allow;
			int startX = ranger.X;
			int startY = ranger.Y;

			NPCAiTimer rangerTimer = NPCAiTimer.GetFor(ranger);
			rangerTimer.Fight(ranger, titan);

			bool moved = (ranger.X != startX) || (ranger.Y != startY);
			Console.WriteLine("RANGED_APPROACH_TEST: hasLineOfSight=" + hasLineOfSight + " moved=" + moved);
		}
	}
}
