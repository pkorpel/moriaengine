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
using System.Linq;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side setup helper for #700's protocol test (Tests/Protocol/NpcGuardContentTests.cs).
	/// Spawns the REAL converted-content guard c_vguard_rohan_hala_jezdec
	/// (scripts/converted/chars/guards.def) - previously spawned naked (converter dropped its
	/// mount/equipment, see ConvertedThingDef.ThirdStage's removed premature `break`) and with
	/// AggroMode.Calm (tag.guard was never derived into AggroMode/Realm at all) - and reports
	/// equipment count, MountItem, AggroMode and Realm so the protocol test can confirm all three
	/// are now real.
	/// </summary>
	public static class NpcGuardContentTests {
		[SteamFunction]
		public static void TestSpawnRealGuardAndReport(Character self, ScriptArgs sa) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_vguard_rohan_hala_jezdec");
			NPC guard = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);

			int equippedCount = guard.Count();
			Console.WriteLine("NPC_GUARD_CONTENT_TEST_RESULT: equipped=" + equippedCount +
				" mountItem=0x" + guard.MountItem.ToString("x") +
				" aggroMode=" + guard.AggroMode +
				" realm=" + guard.Realm);
		}
	}
}
