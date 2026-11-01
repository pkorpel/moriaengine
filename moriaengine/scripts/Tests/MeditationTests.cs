using System;

namespace SteamEngine.CompiledScripts {
	public static class MeditationTests {
		[SteamFunction]
		public static void TestMedReady(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Meditation, 1100);
			self.ManaRegenSpeed = 0;
			self.Mana = (short) (self.MaxMana - int.Parse(sa.Args.Trim('"')));
			self.SelectSkill(SkillName.Meditation);
			Console.WriteLine("MED_READY:");
		}

		[SteamFunction]
		public static void TestMedState(Character self) {
			Console.WriteLine("MED_STATE: missing=" + (self.MaxMana - self.Mana) + " skill=" + self.CurrentSkillName);
		}

		[SteamFunction]
		public static void TestMedStep(Character self) {
			MeditationSkillDef.OnStep(self);
			Console.WriteLine("MED_STEPPED:");
		}
	}
}
