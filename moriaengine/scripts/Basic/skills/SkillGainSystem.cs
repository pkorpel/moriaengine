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

using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// The generic skill-gain-through-use mechanic (#140/#327/#328) - real Erebor Sphere source
	/// (spheretables.scp) uses a native `ADVRATE` field per skill: a 3-point curve (successes
	/// needed to gain 0.1 skill, at 0%/50%/100% of the skill's own current value relative to its
	/// cap). `SkillDef.AdvRateOfChar` already correctly interpolates this curve
	/// (ScriptUtil.EvalRangePermille) - this class is just the missing roll-and-increment step,
	/// plus the gates real Erebor's own `funkce.scp` `[function skillgain]` applies (profession
	/// cap, NPC exclusion, safe-region anti-macro exclusion).
	/// </summary>
	public static class SkillGainSystem {

		/// <summary>Attempts a skill gain for `self` using the native ADVRATE curve - call this
		/// from a skill's On_Success/On_Fail (real Erebor gains on both outcomes, not just
		/// success). Returns true if the skill actually went up. No-op (returns false) for NPCs,
		/// characters with no profession (Skill.Cap is 0 for those - see Skill.cs), characters
		/// already at their profession's cap, or anyone standing in a safe-flagged region
		/// (anti-macro, matches `funkce.scp`'s own `region.flags & region_flag_safe` check).</summary>
		public static bool TryGainSkill(Character self, SkillName skillName) {
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

			double advRate = def.AdvRateOfChar(self);
			if (advRate <= 0) {
				return false; //malformed/unset ADVRATE data - fail safe rather than divide by ~0
			}

			// Real Erebor formula shape (funkce.scp's [function skillgain]): roll {0, N}, gain
			// happens on hitting exactly 0 - i.e. a 1-in-(N+1) chance, where N is the interpolated
			// "successes needed" value. Matches the documented semantics directly: a value of 10
			// at this skill level means roughly 1-in-10 odds per attempt.
			int roll = Globals.dice.Next((int) advRate + 1);
			if (roll != 0) {
				return false;
			}

			self.SetRealSkillValue(def, currentValue + 1); //+0.1 displayed - skills are stored x10 internally
			return true;
		}

		/// <summary>Lockpicking's custom-formula advrate (#329) - the real Erebor flat
		/// `advrate_lockpicking` constant. Public/mutable so #330's .settings category can
		/// write through to it, same pattern as every other GM-tunable value in this system.</summary>
		public static double LockpickingAdvRate = 600;

		/// <summary>Taming's custom-formula advrate (#329) - the real Erebor flat
		/// `advrate_taming` constant. Public/mutable so #330's .settings category can write
		/// through to it.</summary>
		public static double TamingAdvRate = 600;

		/// <summary>Lockpicking/Taming's own gain path (#329) - the 2 of #140's 13 skills whose
		/// real Erebor .def disables the native ADVRATE curve (`ADVRATE=0fffffff`) in favor of
		/// this LScript-side `[function skillgain]`: chance = skillValue² × advrate / (10000 ×
		/// chanceModifier), same {0,N}-roll/gain-on-zero shape as TryGainSkill above, but fed by
		/// a flat per-skill advrate constant instead of a 3-point curve. chanceModifier matches
		/// every real content call site (`skillgain(skill_X, 100)` - always 100, "without
		/// change") - not exposed as a per-call argument since nothing in this engine's content
		/// needs a different value.</summary>
		public static bool TryGainSkillCustomFormula(Character self, SkillName skillName, double advRate) {
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

			const double chanceModifier = 100;
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
