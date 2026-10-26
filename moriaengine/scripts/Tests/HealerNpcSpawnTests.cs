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

	/// <summary>Real-user-reported: creating c_f_HEALER logged a false "random_female_hair is
	/// not a known ItemDef" warning (a native Sphere random-hair-pick keyword, not a real
	/// ItemDef); creating c_m_HEALER crashed entirely, an uncaught exception resolving
	/// "Color = match_hair" (another native keyword, meaning "same color as the hair item
	/// equipped just before this one") inside Thing's own constructor. Both are now handled by
	/// NPC.On_Create's hair-randomizer/EquipColor logic. Spawns directly via ThingDef.Create so
	/// a real exception during creation surfaces as a genuine test failure, then reports the
	/// resulting hair/beard items so the match_hair color-copy can be checked for real.</summary>
	public static class HealerNpcSpawnTests {
		[SteamFunction]
		public static void TestSpawnHealer(Character self, ScriptArgs sa) {
			string defname = Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture);
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname(defname);
			NPC npc = (NPC) npcDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Item hair = npc.Hair;
			Item beard = npc.Beard;
			Console.WriteLine("HEALER_SPAWNED: uid=" + npc.Uid
				+ " hair=" + (hair != null ? "Color" + hair.Color : "none")
				+ " beard=" + (beard != null ? "Color" + beard.Color : "none"));
		}
	}
}
