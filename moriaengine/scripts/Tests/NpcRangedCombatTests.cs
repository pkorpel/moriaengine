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
	/// Server-side setup/report helpers for #600's protocol test
	/// (Tests/Protocol/NpcRangedCombatTests.cs) - proves a real NPC equipped with a real bow
	/// (c_titan_ranger, real archive content - Equip = i_bow) actually gets a live ranged
	/// WeaponRangeVsM/VsP and a resolved WeaponProjectile from CombatCalculator's NPC branch,
	/// instead of being stuck at the unconfigured NPCDef.WeaponRange default of 1 (melee).
	/// </summary>
	public static class NpcRangedCombatTests {
		[SteamFunction]
		public static void TestSpawnRangerAndReportCombatValues(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_titan_ranger");
			NPC ranger = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("RANGED_NPC_TEST_SPAWNED: uid=" + ranger.Uid +
				" rangeVsM=" + ranger.WeaponRangeVsM + " rangeVsP=" + ranger.WeaponRangeVsP +
				" projectileType=" + ranger.WeaponProjectileType +
				" hasProjectile=" + (ranger.WeaponProjectile != null));
		}
	}
}
