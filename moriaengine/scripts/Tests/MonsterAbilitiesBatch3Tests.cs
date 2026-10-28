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
	/// Real-user-reported follow-up ("what are some other monster abilities we can
	/// implement?") - e_summoneater/e_parakrik/e_npc_curse/e_npc_summoning/e_npc_sniper/
	/// e_npc_infiniteMana given real behavior (MonsterSpecialAttacks.cs). Server-side setup
	/// helpers for Tests/Protocol/MonsterAbilitiesBatch3Tests.cs - same "deterministic direct
	/// call" precedent as the two earlier teventStubs batches.
	/// </summary>
	public static class MonsterAbilitiesBatch3Tests {
		[SteamFunction]
		public static void TestSummonEaterDeletesSummonButNotOrdinaryTarget(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_summoneater_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC summon = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			SummonSpellDef.MarkAsSummonedCreature(summon);
			NPC ordinary = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			bool consumedSummon = MonsterSpecialAttacks.TryApplySummonEater(attacker, summon);
			bool summonDeleted = summon.IsDeleted;
			bool consumedOrdinary = MonsterSpecialAttacks.TryApplySummonEater(attacker, ordinary);
			bool ordinaryDeleted = ordinary.IsDeleted;

			Console.WriteLine("SUMMONEATER_TEST: consumedSummon=" + consumedSummon + " summonDeleted=" + summonDeleted +
				" consumedOrdinary=" + consumedOrdinary + " ordinaryDeleted=" + ordinaryDeleted);
		}

		[SteamFunction]
		public static void TestParaKrikParalyzesNearbyPlayer(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			//real archive filter excludes GMs from ever being paralyzed - this test's own
			//connected character is otherwise the server's literal first-ever account (always
			//auto-elevated to GM, see the standing "account creation order" gotcha), so it must
			//explicitly demote itself first to actually exercise the paralyze path.
			PetCommandTests.TestSetGmOff(self);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_parakrik_attacker");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);

			MonsterSpecialAttacks.TryApplyParaKrik(attacker);

			bool paralyzed = self.GetPlugin(MonsterSpecialAttacks.paraKrikPluginKey) != null;
			Console.WriteLine("PARAKRIK_TEST: paralyzed=" + paralyzed);
		}

		[SteamFunction]
		public static void TestNpcCurseDamagesPlayerAndPetButNotWildMonster(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			//same GM-exclusion gotcha as TestParaKrikParalyzesNearbyPlayer above - IsValidNpcCurseTarget
			//also excludes GMs from the player-target branch.
			PetCommandTests.TestSetGmOff(self);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_npccurse_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			self.MaxHits = 1000;
			self.Hits = 1000;
			int selfHitsBefore = self.Hits;

			NPC pet = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			pet.MaxHits = 1000;
			pet.Hits = 1000;
			pet.ControlMaster = self;
			int petHitsBefore = pet.Hits;

			NPC wildMonster = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			wildMonster.MaxHits = 1000;
			wildMonster.Hits = 1000;
			int wildHitsBefore = wildMonster.Hits;

			MonsterSpecialAttacks.TryApplyNpcCurse(attacker);

			Console.WriteLine("NPCCURSE_TEST: playerDamaged=" + (self.Hits < selfHitsBefore) +
				" petDamaged=" + (pet.Hits < petHitsBefore) +
				" wildMonsterDamaged=" + (wildMonster.Hits < wildHitsBefore));
		}

		[SteamFunction]
		public static void TestSummonerSpawnsConfiguredReinforcements(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_summoner_attacker");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);

			int countBefore = CountNearbyJackrabbits(self);

			MonsterSpecialAttacks.TryApplySummoning(attacker);

			int countAfter = CountNearbyJackrabbits(self);
			bool foundHostileSummonedOne = false;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				NPC npc = ch as NPC;
				if ((npc != null) && !npc.IsDeleted && (npc.Def.Defname == "c_a_jackrabbit") &&
						(npc.AggroMode == AggroMode.Hostile) && SummonSpellDef.IsSummonedCreature(npc)) {
					foundHostileSummonedOne = true;
				}
			}

			Console.WriteLine("SUMMONER_TEST: countBefore=" + countBefore + " countAfter=" + countAfter +
				" foundHostileSummonedOne=" + foundHostileSummonedOne);
		}

		private static int CountNearbyJackrabbits(Character self) {
			int count = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				NPC npc = ch as NPC;
				if ((npc != null) && !npc.IsDeleted && (npc.Def.Defname == "c_a_jackrabbit")) {
					count++;
				}
			}
			return count;
		}

		[SteamFunction]
		public static void TestSniperAddsBonusDamageVsPlayerAtRangeOnly(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_sniper_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			WeaponSwingArgs vsPlayer = new WeaponSwingArgs(attacker, self, 100, 0, 0, 100);
			MonsterSpecialAttacks.TryApplySniper(attacker, vsPlayer);

			WeaponSwingArgs vsMonster = new WeaponSwingArgs(attacker, dummy, 100, 0, 0, 100);
			MonsterSpecialAttacks.TryApplySniper(attacker, vsMonster);

			Console.WriteLine("SNIPER_TEST: vsPlayerDamage=" + vsPlayer.DamageAfterAC +
				" vsMonsterDamage=" + vsMonster.DamageAfterAC);
		}

		[SteamFunction]
		public static void TestInfiniteManaKeepsManaFullOnlyWithAbility(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_infinitemana_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			attacker.MaxMana = 1000;
			attacker.Mana = 1;
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.MaxMana = 1000;
			dummy.Mana = 1;

			MonsterSpecialAttacks.TryApplyInfiniteMana(attacker);
			MonsterSpecialAttacks.TryApplyInfiniteMana(dummy);

			Console.WriteLine("INFINITEMANA_TEST: attackerMana=" + attacker.Mana + " dummyMana=" + dummy.Mana);
		}
	}
}
