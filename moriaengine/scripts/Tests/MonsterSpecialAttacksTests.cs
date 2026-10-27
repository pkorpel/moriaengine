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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - server-side setup helpers
	/// for Tests/Protocol/MonsterSpecialAttacksTests.cs. Each test NPCDef (scripts/Tests/
	/// MonsterSpecialAttacksTest.def) is configured with a guaranteed (1000/1000) chance for
	/// its own ability, so these call MonsterSpecialAttacks' public methods directly rather
	/// than waiting out real combat timing/RNG - same "deterministic direct call" precedent
	/// as NPCAiTimer's own tests.
	/// </summary>
	public static class MonsterSpecialAttacksTests {
		[SteamFunction]
		public static void TestStunLandsOnHit(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_stun_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			MonsterSpecialAttacks.TryApplyStun(attacker, dummy);

			bool stunned = dummy.GetPlugin(MonsterSpecialAttacks.stunPluginKey) != null;
			Console.WriteLine("STUN_TEST: stunned=" + stunned);
		}

		[SteamFunction]
		public static void TestStunNeverAppliesToHighResistTarget(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_stun_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.ResistParalyse = 950; //above the real archive's own 900 "immune" threshold

			MonsterSpecialAttacks.TryApplyStun(attacker, dummy);

			bool stunned = dummy.GetPlugin(MonsterSpecialAttacks.stunPluginKey) != null;
			Console.WriteLine("STUN_RESIST_TEST: stunned=" + stunned);
		}

		[SteamFunction]
		public static void TestCriticAddsBonusDamageVsPlayerOnly(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_critic_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			WeaponSwingArgs vsPlayer = new WeaponSwingArgs(attacker, self, 100, 0, 0, 100);
			MonsterSpecialAttacks.TryApplyCritic(attacker, vsPlayer);

			WeaponSwingArgs vsMonster = new WeaponSwingArgs(attacker, dummy, 100, 0, 0, 100);
			MonsterSpecialAttacks.TryApplyCritic(attacker, vsMonster);

			Console.WriteLine("CRITIC_TEST: vsPlayerDamage=" + vsPlayer.DamageAfterAC +
				" vsMonsterDamage=" + vsMonster.DamageAfterAC);
		}

		[SteamFunction]
		public static void TestRoundattackHitsNearbyTargets(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_roundattack_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.MaxHits = 1000;
			dummy.Hits = 1000;
			int startingHits = dummy.Hits;

			MonsterSpecialAttacks.TryRoundattack(attacker);

			Console.WriteLine("ROUNDATTACK_TEST: damaged=" + (dummy.Hits < startingHits));
		}

		[SteamFunction]
		public static void TestRoundattackPullDragsTargetCloser(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_roundattack_pull_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 5, self.Y, self.Z, self.M);
			dummy.MaxHits = 1000;
			dummy.Hits = 1000;
			int startDistance = Point2D.GetSimpleDistance(attacker, dummy);

			MonsterSpecialAttacks.TryRoundattack(attacker);

			int endDistance = Point2D.GetSimpleDistance(attacker, dummy);
			Console.WriteLine("ROUNDATTACK_PULL_TEST: startDistance=" + startDistance +
				" endDistance=" + endDistance + " pulledCloser=" + (endDistance < startDistance));
		}
	}
}
