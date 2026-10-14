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
	/// <summary>Which attribute a call to LevelingSystem.TrySpendAttributePoints targets (#129).</summary>
	public enum AttributeStat {
		Str,
		Dex,
		Int,
		Vit
	}

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
				//Skill/ability points are flat per-level increments (#129) - attribute points are
				//NOT granted here, that budget is computed on demand from Level (see
				//Player.AttributePointBudget/LevelStatBonus below), since it's a tiered formula
				//of the absolute level, not a simple running total.
				player.UnspentSkillPoints += 5;
				player.UnspentAbilityPoints += 2;
			}
		}

		/// <summary>The tiered attribute-point-budget bonus (#129), verified against Erebor's own
		/// f_levelstatbonus: level*4 through level 9, 40+(level-10)*3 through level 29,
		/// 100+(level-30)*2 through level 39, then flat 120 forever from level 40 on (level 40 is
		/// the last level that grants any further attribute points). Added to a flat base of 300
		/// in Player.AttributePointBudget.</summary>
		public static int LevelStatBonus(int level) {
			if (level <= 9) {
				return level * 4;
			}
			if (level <= 29) {
				return 40 + (level - 10) * 3;
			}
			if (level <= 39) {
				return 100 + (level - 30) * 2;
			}
			return 120;
		}

		/// <summary>Spends `pointsToSpend` unspent skill points (#129) into `skill`, validated
		/// against the player's profession skill cap - mirrors Skill.Cap's own precedent of
		/// returning 0 when no profession is assigned, so a player with no profession can never
		/// spend a skill point at all (not an oversight - this is the user's own stated design,
		/// "no-profession-assigned = can't spend attribute points", applied the same way here).
		/// Returns false (no partial spend) if the player lacks the points or the cap would be
		/// exceeded.</summary>
		public static bool TrySpendSkillPoints(Player player, SkillName skill, int pointsToSpend) {
			if ((player == null) || (pointsToSpend <= 0) || (player.UnspentSkillPoints < pointsToSpend)) {
				return false;
			}
			ProfessionDef profession = player.Profession;
			int cap = (profession != null) ? profession.GetSkillCap(skill) : 0;
			int current = player.GetRealSkillValue(skill);
			if ((current + pointsToSpend) > cap) {
				return false;
			}
			player.ModifySkillValue((int) skill, pointsToSpend);
			player.UnspentSkillPoints -= pointsToSpend;
			return true;
		}

		/// <summary>Spends `pointsToSpend` unspent ability points (#129) into `ability`, via the
		/// existing (previously unused) Character.ModifyAbilityPoints mechanism. Does NOT
		/// validate against any profession-specific ability allowlist - that depends on the
		/// Abilities/talent-tree redesign the user explicitly deferred (see CLAUDE.md's
		/// Professions section); this just spends the pool, same way the engine already lets any
		/// ability points be granted today.</summary>
		public static bool TrySpendAbilityPoints(Player player, AbilityDef ability, int pointsToSpend) {
			if ((player == null) || (ability == null) || (pointsToSpend <= 0) ||
				(player.UnspentAbilityPoints < pointsToSpend)) {
				return false;
			}
			player.ModifyAbilityPoints(ability, pointsToSpend);
			player.UnspentAbilityPoints -= pointsToSpend;
			return true;
		}

		/// <summary>Spends `pointsToSpend` attribute points (#129) into one of STR/DEX/INT/VIT,
		/// validated against Player.AttributePointBudget. Requires a profession to be assigned at
		/// all - mirrors Skill.Cap's own "no profession = cap 0" precedent, the user's own stated
		/// design for this phase. Per-profession attribute RANGES (#135's planned strRange/
		/// dexRange/intRange/vitRange ProfessionDef fields) aren't implemented yet - once they
		/// are, this is the place to also validate the resulting stat value against them.</summary>
		public static bool TrySpendAttributePoints(Player player, AttributeStat stat, int pointsToSpend) {
			if ((player == null) || (pointsToSpend <= 0) || (player.Profession == null) ||
				(player.AttributePointBudget < pointsToSpend)) {
				return false;
			}
			switch (stat) {
				case AttributeStat.Str:
					player.Str += (short) pointsToSpend;
					player.StrPointsAllocated += pointsToSpend;
					break;
				case AttributeStat.Dex:
					player.Dex += (short) pointsToSpend;
					player.DexPointsAllocated += pointsToSpend;
					break;
				case AttributeStat.Int:
					player.Int += (short) pointsToSpend;
					player.IntPointsAllocated += pointsToSpend;
					break;
				case AttributeStat.Vit:
					player.Vit += (short) pointsToSpend;
					player.VitPointsAllocated += pointsToSpend;
					break;
			}
			return true;
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
