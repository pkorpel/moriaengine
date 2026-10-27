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
	/// mount/equipment, see ConvertedThingDef.ThirdStage's removed premature `break`), unmounted
	/// (tag.mount used to resolve to a raw MountItem number set directly on the GUARD's own
	/// NPCDef, which has zero visual effect - see NPC.cs's On_Create doc comment on
	/// NPCDef.MountCharDef for why), and with AggroMode.Calm (tag.guard was never derived into
	/// AggroMode/Realm at all) - and reports equipment count, whether it's actually riding a real
	/// mount (and that mount's own species/color), AggroMode and Realm so the protocol test can
	/// confirm all four are now real.
	/// </summary>
	public static class NpcGuardContentTests {
		[SteamFunction]
		public static void TestSpawnRealGuardAndReport(Character self, ScriptArgs sa) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_vguard_rohan_hala_jezdec");
			NPC guard = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);

			int equippedCount = guard.Count();
			AbstractCharacter mount = guard.Mount;
			string mountInfo = (mount == null)
				? "none"
				: (mount.Def.PrettyDefname + "/0x" + mount.MountItem.ToString("x") + "/0x" + mount.Color.ToString("x"));
			Console.WriteLine("NPC_GUARD_CONTENT_TEST_RESULT: equipped=" + equippedCount +
				" mount=" + mountInfo +
				" aggroMode=" + guard.AggroMode +
				" realm=" + guard.Realm);
		}
	}
}
