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
		private static readonly TagKey lastDeathTimeTK = TagKey.Acquire("_levelingLastDeathTime_");

		/// <summary>How long after a death a player is immune to losing MORE experience from a
		/// subsequent death - mirrors Erebor's own i_checkstats_timer (5 real minutes,
		/// experiences.scp), there to stop death-chaining (e.g. corpse-camping) from draining a
		/// player repeatedly. Not a GM setting (unlike the two percentages below) - the user's
		/// design only called out those two as needing in-game tuning.</summary>
		private static readonly System.TimeSpan PostDeathExperienceLossImmunity = System.TimeSpan.FromSeconds(300);

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

		/// <summary>
		/// Death's experience-loss/pay-the-killer mechanic (#128) - the user's own stated
		/// design, deliberately diverging from Erebor's actual partial-drain formula: at the
		/// default settings (experienceLossPercentage=100, pkGainPercentage=10) the dying
		/// player loses everything and killer(s) collectively get 10% of what was lost (not
		/// 10% of the original total). Level never changes here - only Experience.
		///
		/// Exemptions (lose nothing, pay nothing): standing in a safe zone/arena
		/// (Notoriety.IsExemptFromExperienceLoss), or within 5 minutes of a previous death
		/// (PostDeathExperienceLossImmunity, mirrors Erebor's i_checkstats_timer).
		///
		/// Multi-killer credit splitting is explicitly OUT of scope for this phase - CauseDeath
		/// only ever identifies a single `killedBy` Character (there is no damage-contribution
		/// list anywhere in this engine to split credit from), so only that one killer (if a
		/// Player) is paid. The user's "default even split" note applies if/when a real
		/// multi-killer attribution mechanism gets built - not invented here speculatively.
		/// </summary>
		public static void ApplyDeathExperienceLoss(Player victim, Character killedBy) {
			if (victim == null) {
				return;
			}

			int lost = 0;
			if (!IsWithinPostDeathImmunity(victim) && !Notoriety.IsExemptFromExperienceLoss(victim)) {
				int lossPercentage = LevelingSettings.instance.experienceLossPercentage;
				lost = (int) ((long) victim.Experience * lossPercentage / 100);
				if (lost > 0) {
					victim.Experience -= lost;
				}
			}
			victim.SetTag(lastDeathTimeTK, Globals.TimeAsSpan);

			if (lost > 0) {
				Player killerPlayer = killedBy as Player;
				if ((killerPlayer != null) && (killerPlayer != victim)) {
					int gainPercentage = LevelingSettings.instance.pkGainPercentage;
					int gained = (int) ((long) lost * gainPercentage / 100);
					AddExperience(killerPlayer, gained);
				}
			}
		}

		private static bool IsWithinPostDeathImmunity(Player victim) {
			object tag = victim.GetTag(lastDeathTimeTK);
			if (tag is System.TimeSpan lastDeath) {
				return (Globals.TimeAsSpan - lastDeath) < PostDeathExperienceLossImmunity;
			}
			return false;
		}
	}
}
