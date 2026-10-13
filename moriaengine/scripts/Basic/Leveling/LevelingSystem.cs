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

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Core leveling/experience system (#125). This is the one shared entry point other
	/// systems (monster kills, future quests/gates) should call to grant experience, rather
	/// than setting Player.Experience directly - that keeps the level-up check (threshold
	/// formula + overflow carry) in one place instead of duplicated at every call site.
	/// </summary>
	public static class LevelingSystem {

		/// <summary>
		/// Grants (or removes, if negative) experience and runs the level-up check - a while
		/// loop, not a single if, so one large gain can correctly cross several level
		/// thresholds in a row, carrying over the leftover experience each time rather than
		/// clamping it to 0 (e.g. level 1 with 600 exp, +100 -> level 2 with 40 exp left over).
		/// Level never decreases here even if amount is negative - that's intentional, this
		/// method is for gaining experience through normal gameplay. Death's exp loss (#128)
		/// is a separate, simpler operation (reset to 0, no level change) that doesn't go
		/// through this method.
		/// </summary>
		public static void AddExperience(Player player, int amount) {
			if (player == null || amount == 0) {
				return;
			}
			player.Experience += amount;
			int coefficient = LevelingSettings.instance.nextLevelCoefficient;
			while (player.Experience > (player.Level + 1) * coefficient) {
				player.Level += 1;
				player.Experience -= player.Level * coefficient;
				//Phase 4 (#129) will hook level-up point-pool rewards (skill/ability/attribute points) here.
			}
		}
	}
}
