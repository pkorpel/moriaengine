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
	/// Server-side helpers for SkillGainSystem protocol tests (#140/#327/#328). Statistical
	/// sampling (not a single roll) is the right proof here, same precedent as
	/// SpawnCommandTests' own TestSampleRollSpawnDefDistribution for a probabilistic system.
	/// </summary>
	public static class SkillGainSystemTests {

		/// <summary>Reports a skill's real ADVRATE array, to directly confirm the real Erebor
		/// curve values (#328) actually loaded from .def content, not the old generic
		/// placeholders.</summary>
		[SteamFunction]
		public static void TestReportAdvRate(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			double[] rate = SkillDef.GetBySkillName(skill).AdvRate;
			Console.WriteLine("ADVRATE_REPORT: skill=" + skill + " values=" + string.Join(",", rate));
		}

		/// <summary>Sets self's RealValue for the given skill directly (bypassing any skill-gain
		/// roll) - needed to put a test character at a known, controlled starting skill value
		/// before sampling gain chance there.</summary>
		[SteamFunction]
		public static void TestSetSkillValue(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			int value = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			self.SetRealSkillValue((int) skill, value);
			Console.WriteLine("SKILL_VALUE_SET: skill=" + skill + " value=" + value);
		}

		/// <summary>Calls SkillGainSystem.TryGainSkill `attempts` times in a row and reports how
		/// many actually raised the skill, plus the skill's value before/after. The real
		/// per-attempt chance at a given skill value is 1/(AdvRateOfChar+1) - sampling many
		/// attempts (not asserting an exact count, inherently random) is the right proof,
		/// matching this repo's own established pattern for probabilistic systems.</summary>
		[SteamFunction]
		public static void TestSampleSkillGain(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			int attempts = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);

			int before = self.GetRealSkillValue(skill);
			int gains = 0;
			for (int i = 0; i < attempts; i++) {
				if (SkillGainSystem.TryGainSkill(self, skill)) {
					gains++;
				}
			}
			int after = self.GetRealSkillValue(skill);
			Console.WriteLine("SKILL_GAIN_SAMPLE: skill=" + skill + " before=" + before + " after=" + after +
				" gains=" + gains + " attempts=" + attempts);
		}

		/// <summary>Confirms NPCs never organically gain skill through use (#328's own explicit
		/// gate) - spawns a throwaway NPC, samples many attempts, confirms zero gains.</summary>
		[SteamFunction]
		public static void TestSampleSkillGainOnNpc(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			int attempts = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);

			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC npc = (NPC) npcDef.Create(self.P());
			npc.SetRealSkillValue((int) skill, 50);

			int gains = 0;
			for (int i = 0; i < attempts; i++) {
				if (SkillGainSystem.TryGainSkill(npc, skill)) {
					gains++;
				}
			}
			Console.WriteLine("SKILL_GAIN_NPC_SAMPLE: skill=" + skill + " gains=" + gains + " attempts=" + attempts);
		}

		/// <summary>Confirms a character already at their profession's skill cap never gains
		/// further (#328's own explicit gate, reusing the pre-existing Skill.Cap mechanism).</summary>
		[SteamFunction]
		public static void TestSampleSkillGainAtCap(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			int attempts = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);

			Player player = (Player) self;
			int cap = (player.Profession != null) ? player.Profession.GetSkillCap((int) skill) : 0;
			self.SetRealSkillValue((int) skill, cap);

			int gains = 0;
			for (int i = 0; i < attempts; i++) {
				if (SkillGainSystem.TryGainSkill(self, skill)) {
					gains++;
				}
			}
			Console.WriteLine("SKILL_GAIN_CAP_SAMPLE: skill=" + skill + " cap=" + cap +
				" valueAfter=" + self.GetRealSkillValue(skill) + " gains=" + gains + " attempts=" + attempts);
		}

		/// <summary>Lockpicking/Taming's own custom-formula gain path (#329) - same sampling
		/// approach as TestSampleSkillGain above, just calling TryGainSkillCustomFormula
		/// instead.</summary>
		[SteamFunction]
		public static void TestSampleSkillGainCustomFormula(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			double advRate = Convert.ToDouble(sa.Argv[1], CultureInfo.InvariantCulture);
			int attempts = Convert.ToInt32(sa.Argv[2], CultureInfo.InvariantCulture);

			int before = self.GetRealSkillValue(skill);
			int gains = 0;
			for (int i = 0; i < attempts; i++) {
				if (SkillGainSystem.TryGainSkillCustomFormula(self, skill, advRate)) {
					gains++;
				}
			}
			int after = self.GetRealSkillValue(skill);
			Console.WriteLine("SKILL_GAIN_CUSTOM_SAMPLE: skill=" + skill + " before=" + before + " after=" + after +
				" gains=" + gains + " attempts=" + attempts);
		}

		/// <summary>Confirms NPCs never organically gain skill through the custom-formula path
		/// either (#329, same gate as #328's native-curve path).</summary>
		[SteamFunction]
		public static void TestSampleSkillGainCustomFormulaOnNpc(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			double advRate = Convert.ToDouble(sa.Argv[1], CultureInfo.InvariantCulture);
			int attempts = Convert.ToInt32(sa.Argv[2], CultureInfo.InvariantCulture);

			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC npc = (NPC) npcDef.Create(self.P());
			npc.SetRealSkillValue((int) skill, 200);

			int gains = 0;
			for (int i = 0; i < attempts; i++) {
				if (SkillGainSystem.TryGainSkillCustomFormula(npc, skill, advRate)) {
					gains++;
				}
			}
			Console.WriteLine("SKILL_GAIN_CUSTOM_NPC_SAMPLE: skill=" + skill + " gains=" + gains + " attempts=" + attempts);
		}

		/// <summary>Confirms a character already at their profession's cap never gains further
		/// through the custom-formula path either (#329, same gate as #328's native-curve
		/// path).</summary>
		[SteamFunction]
		public static void TestSampleSkillGainCustomFormulaAtCap(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture), true);
			double advRate = Convert.ToDouble(sa.Argv[1], CultureInfo.InvariantCulture);
			int attempts = Convert.ToInt32(sa.Argv[2], CultureInfo.InvariantCulture);

			Player player = (Player) self;
			int cap = (player.Profession != null) ? player.Profession.GetSkillCap((int) skill) : 0;
			self.SetRealSkillValue((int) skill, cap);

			int gains = 0;
			for (int i = 0; i < attempts; i++) {
				if (SkillGainSystem.TryGainSkillCustomFormula(self, skill, advRate)) {
					gains++;
				}
			}
			Console.WriteLine("SKILL_GAIN_CUSTOM_CAP_SAMPLE: skill=" + skill + " cap=" + cap +
				" valueAfter=" + self.GetRealSkillValue(skill) + " gains=" + gains + " attempts=" + attempts);
		}
	}
}
