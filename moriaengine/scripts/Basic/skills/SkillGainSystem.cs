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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Gaining a skill by using it (#140/#328), Sphere's Skill_Experience. Only the skills with a real ADVRATE on Erebor
	/// gain this way: crafting, gathering and Cartography. The rest are 0fffffff there, Lockpicking and Taming have
	/// their own formula below and most others grow by level points or teachers.
	/// </summary>
	public static class SkillGainSystem {
		private const int LearnsFromFailureBelow = 300, MaxDifficulty = 1000, TooEasy = 500, RatherEasy = 400;


		/// <summary>difficulty in tenths, as a skill value, and negative for a failed attempt. Failing teaches only under
		/// 30.0. An easy job teaches half as often from 40.0 below the skill and nothing from 50.0 below. Returns true if
		/// the skill went up.</summary>
		public static bool TryGainSkill(Character self, SkillName skillName, int difficulty) {
			if (!(self is Player)) {
				return false; //NPCs never organically gain skill through use
			}
			if ((self.Region as FlaggedRegion)?.Flag_Safe == true) {
				return false; //anti-macro - matches the real Erebor safe-region exclusion
			}

			SkillDef def = SkillDef.GetBySkillName(skillName);

			// Deliberately avoid Character.GetSkillObject/Skill.Cap here - GetSkillObject
			// returns null for any skill the character has never had explicitly set (the common
			// case: a Skill instance is only ever materialized on first write, not eagerly for
			// every skill a character could theoretically have), which would NullReferenceException
			// the very first time a player tries a skill they've never touched. GetRealSkillValue/
			// SetRealSkillValue are the null-safe equivalents; Skill.Cap's own profession-cap logic
			// is reproduced inline since it doesn't actually need an existing Skill instance.
			ProfessionDef profession = ((Player) self).Profession;
			int cap = (profession != null) ? profession.GetSkillCap(def.Id) : 0;
			int currentValue = self.GetRealSkillValue(def);
			if (cap <= 0 || currentValue >= cap) {
				return false;
			}

			int effort = EffortOf(difficulty, currentValue);
			if (effort == 0) {
				return false;
			}
			int chance = GainChance(def.AdvRate, currentValue, effort);
			if ((chance <= 0) || (Globals.dice.Next(1000) > chance)) {
				return false;
			}

			self.SetRealSkillValue(def, currentValue + 1); //+0.1 displayed - skills are stored x10 internally
			return true;
		}

		public static int EffortOf(int difficulty, int skillValue) {
			if (difficulty < 0) {
				difficulty = (skillValue < LearnsFromFailureBelow) ? ((Math.Min(-difficulty, skillValue) / 2) - 8) : 0;
			}
			return Math.Min(difficulty, MaxDifficulty);
		}

		public static int GainChance(double[] advRate, int skillValue, int effort) {
			int chance = SkillUtils.GetAdvanceChance(advRate, skillValue + (skillValue - effort));
			if (effort + TooEasy < skillValue) {
				return 0;
			}
			return (effort + RatherEasy < skillValue) ? (chance / 2) : chance;
		}

		/// <summary>Lockpicking/Taming's own gain path (#329) - the 2 of #140's 13 skills whose
		/// real Erebor .def disables the native ADVRATE curve (`ADVRATE=0fffffff`) in favor of
		/// this LScript-side `[function skillgain]`: chance = skillValue² × advrate / (10000 ×
		/// chanceModifier), same {0,N}-roll/gain-on-zero shape as TryGainSkill above, but fed by
		/// a flat per-skill advrate constant instead of a 3-point curve. chanceModifier matches
		/// skillgain's argv(1): 100 is "without change", more makes a gain likelier, as a spawned mount does.</summary>
		public static bool TryGainSkillCustomFormula(Character self, SkillName skillName, double advRate, double chanceModifier = 100) {
			if (!(self is Player)) {
				return false;
			}
			if ((self.Region as FlaggedRegion)?.Flag_Safe == true) {
				return false;
			}

			SkillDef def = SkillDef.GetBySkillName(skillName);
			ProfessionDef profession = ((Player) self).Profession;
			int cap = (profession != null) ? profession.GetSkillCap(def.Id) : 0;
			int currentValue = self.GetRealSkillValue(def);
			if (cap <= 0 || currentValue >= cap) {
				return false;
			}

			double chance = (currentValue * currentValue * advRate) / (10000 * chanceModifier);
			int roll = Globals.dice.Next((int) chance + 1);
			if (roll != 0) {
				return false;
			}

			self.SetRealSkillValue(def, currentValue + 1);
			return true;
		}
	}
}
