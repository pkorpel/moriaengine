using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Real-user-reported: a ranged NPC fired a few shots then stopped attacking
	/// entirely, while a caster NPC in the same fight kept going. Root cause: a transient LOS
	/// loss during WeaponSkillDef.On_Stroke calls WeaponSkillTargetQueuePlugin.RemoveTarget,
	/// which - once this was the only queued target - deletes the whole plugin.
	/// NPCAiTimer.Fight's own "AddTarget exactly once per engagement" bookkeeping
	/// (lastEngagedTarget) had no way to learn this happened, so it never re-engaged on a later
	/// tick even once the obstruction causing the LOS loss was gone. Reproduces the exact
	/// sequence Fight()/On_Stroke actually drive - engage, simulate the abort via the same
	/// RemoveTarget call On_Stroke itself makes, then call Fight() again - rather than waiting
	/// out live AI ticks (documented elsewhere in this codebase as flaky in this sandbox).</summary>
	public static class NpcReengagementAfterAbortTests {
		[SteamFunction]
		public static void TestNpcReengagesAfterTargetQueueAbort(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			NPCDef rangerDef = ThingDef.GetByDefname("c_titan_ranger") as NPCDef;
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC ranger = (NPC) rangerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);

			NPCAiTimer rangerTimer = NPCAiTimer.GetFor(ranger);
			rangerTimer.Fight(ranger, titan);
			bool engagedBeforeAbort = WeaponSkillTargetQueuePlugin.IsActivelyEngaging(ranger, titan);

			//same call WeaponSkillDef.On_Stroke itself makes on a real LOS-loss abort - the
			//resulting empty queue deletes the whole plugin (FightCurrentTarget's this.Delete()
			//fallback), simulating the exact silent-stuck state the user hit.
			WeaponSkillTargetQueuePlugin.RemoveTarget(ranger, titan);
			bool engagedRightAfterAbort = WeaponSkillTargetQueuePlugin.IsActivelyEngaging(ranger, titan);

			rangerTimer.Fight(ranger, titan);
			bool engagedAfterNextFightTick = WeaponSkillTargetQueuePlugin.IsActivelyEngaging(ranger, titan);

			Console.WriteLine("REENGAGEMENT_TEST: engagedBeforeAbort=" + engagedBeforeAbort +
				" engagedRightAfterAbort=" + engagedRightAfterAbort +
				" engagedAfterNextFightTick=" + engagedAfterNextFightTick);
		}
	}
}
