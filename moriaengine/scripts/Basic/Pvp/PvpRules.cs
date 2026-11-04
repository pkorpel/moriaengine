using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's isPvPEvaluable (sphereevents.scp): whether a player's death at `attacker`'s hands counts for
	/// PvP - for the Nazgul rings' kill count, the PvP league and the keep conquest. Different realms, the victim not in an
	/// arena, not a craftsman, and of skill rank 3 or more. Dying again within LevelingSystem's 5 minutes (i_checkstats_timer)
	/// is the caller's check, as it applies to everyone around the death at once.</summary>
	public static class PvpRules {
		public static bool IsEvaluable(Player victim, Character attacker) {
			FlaggedRegion area = RegionEventSystem.AreaOf(victim.Region);
			return (victim.Realm != attacker.Realm) && ((area == null) || !area.Flag_Arena) &&
				!Professions.HasProfession(victim, Professions.Craftsman) && ((int) victim.SkillRank >= 3);
		}
	}
}
