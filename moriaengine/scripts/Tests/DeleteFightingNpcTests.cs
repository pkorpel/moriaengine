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
	/// #786 - server side of Tests/Protocol/DeleteFightingNpcTests.cs: deleting an NPC with a swing
	/// queued (WeaponSkillTargetQueuePlugin installed) used to overflow the stack and kill GameCore.
	/// </summary>
	public static class DeleteFightingNpcTests {
		[SteamFunction]
		public static void TestDeleteFightingNpc(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			npc.Flag_WarMode = true;
			WeaponSkillTargetQueuePlugin.AddTarget(npc, self); //starts the swing, installs the plugin
			Console.WriteLine("DELETE_FIGHTING_NPC: swinging=" + (npc.CurrentSkill != null));

			npc.Delete();
			Console.WriteLine("DELETE_FIGHTING_NPC: deleted=" + npc.IsDeleted);
		}
	}
}
