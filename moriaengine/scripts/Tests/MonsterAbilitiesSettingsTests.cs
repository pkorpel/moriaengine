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
	/// User-requested follow-up (#758) to the teventStubs batches - a new "Monster Abilities"
	/// .settings page, plus a configurable multi-tile knockback push. Server-side setup
	/// helpers for Tests/Protocol/MonsterAbilitiesSettingsTests.cs - own dedicated account/
	/// collection since every other monster-abilities test file is already at its own
	/// 5-character-slot cap.
	/// </summary>
	public static class MonsterAbilitiesSettingsTests {
		/// <summary>MonsterAbilitiesSettings.KnockbackDistance actually drives the real push
		/// distance, not just a cosmetic settings-dialog field. Reuses one attacker/dummy pair
		/// across both trials (resetting the dummy's position via a direct P() between them)
		/// rather than two pairs, to keep this inside whatever open-ground radius the caller
		/// already confirmed via .TestTeleportToOpenGround.</summary>
		[SteamFunction]
		public static void TestKnockbackDistanceSettingScalesPush(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_knockback_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);

			double originalDistance = MonsterAbilitiesSettings.instance.KnockbackDistance;

			MonsterAbilitiesSettings.instance.KnockbackDistance = 1;
			MonsterSpecialAttacks.TryApplyKnockback(attacker, dummy);
			int pushedAt1 = Point2D.GetSimpleDistance(attacker, dummy);

			dummy.P(self.X + 1, self.Y, self.Z, self.M); //back right next to the attacker

			MonsterAbilitiesSettings.instance.KnockbackDistance = 5;
			MonsterSpecialAttacks.TryApplyKnockback(attacker, dummy);
			int pushedAt5 = Point2D.GetSimpleDistance(attacker, dummy);

			MonsterAbilitiesSettings.instance.KnockbackDistance = originalDistance;

			Console.WriteLine("KNOCKBACK_DISTANCE_SETTING_TEST: pushedAt1=" + pushedAt1 +
				" pushedAt5=" + pushedAt5);
		}

		/// <summary>Proves MonsterAbilitiesSettings' pass-through properties really do write to
		/// the SAME live AbilityDef instance MonsterSpecialAttacks' Try* methods read from
		/// (MonsterSpecialAttacks.StunDef), not a separate copy - and that reading it back
		/// through the settings property reflects the change too (a real FieldValue round
		/// trip, not a cache). Picks Stun as one representative example; every other property
		/// in the new class follows the exact same shape.</summary>
		[SteamFunction]
		public static void TestMonsterAbilitiesSettingsWritesThroughToLiveAbility(Character self) {
			double[] original = MonsterAbilitiesSettings.instance.StunDuration;

			MonsterAbilitiesSettings.instance.StunDuration = new double[] { 42, 42 };
			double[] liveValue = MonsterSpecialAttacks.StunDef.Duration;
			double[] readBack = MonsterAbilitiesSettings.instance.StunDuration;

			MonsterAbilitiesSettings.instance.StunDuration = original;

			bool liveMatches = (liveValue[0] == 42) && (liveValue[1] == 42);
			bool readBackMatches = (readBack[0] == 42) && (readBack[1] == 42);
			Console.WriteLine("SETTINGS_WRITETHROUGH_TEST: liveMatches=" + liveMatches +
				" readBackMatches=" + readBackMatches);
		}

		/// <summary>User-raised concern: "knockback only works on melee, should work for
		/// ranger (bow/crossbow) monsters too." Confirmed via code reading this was already
		/// true - MonsterSpecialAttacks.TryApplyKnockback has no weapon-type check at all, and
		/// it's rolled inside DamageManager.ProcessSwing, which WeaponSkillDef.On_Success calls
		/// identically for a landed ranged OR melee hit (same single call site, no branching).
		/// c_troll_strelec (scripts/converted/chars/ogres.def) already proves this combination
		/// exists in real content today - a bow-equipped archer granted BOTH e_npc_sniper AND
		/// e_npc_knockback. This test exercises that exact real NPCDef, at a non-adjacent
		/// distance (mimicking a real ranged shot, not a melee swing) - a regression guard so
		/// this stays weapon-agnostic going forward, not a fix for a real gap.</summary>
		[SteamFunction]
		public static void TestKnockbackWorksOnARangedBowWieldingAttacker(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("c_troll_strelec");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 5, self.Y, self.Z, self.M); //non-adjacent - a real ranged shot's own shape, not a melee swing

			int[] originalChance = attackerDef.KnockbackChance; //c_troll_strelec has none set - null, falls back to the AbilityDef's own ~33% default
			attackerDef.KnockbackChance = new int[] { 1000, 1000 }; //guaranteed roll, for a deterministic test

			bool isBow = attacker.WeaponType == WeaponType.Bow;
			int startDistance = Point2D.GetSimpleDistance(attacker, dummy);
			MonsterSpecialAttacks.TryApplyKnockback(attacker, dummy);
			int endDistance = Point2D.GetSimpleDistance(attacker, dummy);

			attackerDef.KnockbackChance = originalChance;

			Console.WriteLine("KNOCKBACK_RANGED_TEST: isBow=" + isBow + " startDistance=" + startDistance +
				" endDistance=" + endDistance + " pushedAway=" + (endDistance > startDistance));
		}
	}
}
