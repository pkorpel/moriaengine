namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's stopscriptedskills: a skill run by a timer of its own ends when another starts, on a hit
	/// and on a spell. A skill that acts in On_Select never reaches On_SkillStart, so it calls this itself.</summary>
	public static class ScriptedSkills {
		public static void Stop(Character self) {
			HidingSkillDef.Spoil(self);
			TamingSkillDef.Spoil(self);
			ProvocationSkillDef.Spoil(self);
			DiscordanceSkillDef.Spoil(self);
			PeacemakingSkillDef.Spoil(self);
			PoisoningSkillDef.Spoil(self);
		}
	}
}
