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
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_knockback/e_selfreser/
	/// e_npc_detect/e_npc_leaper/e_looter given real behavior (MonsterSpecialAttacks.cs,
	/// Character.CauseDeath, NPCAiTimer.Fight, Corpse.On_Step). Server-side setup helpers for
	/// Tests/Protocol/MonsterAbilitiesBatchTests.cs - same "deterministic direct call" precedent
	/// as scripts/Tests/MonsterSpecialAttacksTests.cs. In a separate file/collection/account from
	/// that one since this batch's own 5 Facts would otherwise push a shared account over the
	/// 5-character-slot cap.
	/// </summary>
	public static class MonsterAbilitiesBatchTests {
		[SteamFunction]
		public static void TestKnockbackPushesTargetAway(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_knockback_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			int startDistance = Point2D.GetSimpleDistance(attacker, dummy);

			MonsterSpecialAttacks.TryApplyKnockback(attacker, dummy);

			int endDistance = Point2D.GetSimpleDistance(attacker, dummy);
			Console.WriteLine("KNOCKBACK_TEST: startDistance=" + startDistance +
				" endDistance=" + endDistance + " pushedAway=" + (endDistance > startDistance));
		}

		[SteamFunction]
		public static void TestKillSelfResMonster(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef monsterDef = (NPCDef) ThingDef.GetByDefname("npc_test_selfres_attacker");
			NPC monster = (NPC) monsterDef.Create(self.X + 1, self.Y, self.Z, self.M);
			monster.CauseDeath(null);
			Console.WriteLine("SELFRES_KILL_TEST: killed=True");
		}

		[SteamFunction]
		public static void TestReportSelfResMonsterCount(Character self) {
			int count = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				NPC npc = ch as NPC;
				if ((npc != null) && !npc.IsDeleted && (npc.Def.Defname == "npc_test_selfres_attacker")) {
					count++;
				}
			}
			Console.WriteLine("SELFRES_COUNT_TEST: count=" + count);
		}

		[SteamFunction]
		public static void TestDetectRevealsHiddenTarget(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_detect_attacker");
			NPC attacker = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			self.Flag_Hidden = true;

			MonsterSpecialAttacks.TryApplyDetect(attacker);

			Console.WriteLine("DETECT_TEST: stillHidden=" + self.Flag_Hidden);
		}

		[SteamFunction]
		public static void TestLeaperExtraSteps(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_leaper_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC leaper = (NPC) attackerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC nonLeaper = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);

			int leaperSteps = MonsterSpecialAttacks.GetLeaperExtraSteps(leaper);
			int nonLeaperSteps = MonsterSpecialAttacks.GetLeaperExtraSteps(nonLeaper);
			Console.WriteLine("LEAPER_TEST: leaperSteps=" + leaperSteps + " nonLeaperSteps=" + nonLeaperSteps);
		}

		[SteamFunction]
		public static void TestLooterMovesItemFromCorpse(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyCorpses(self, 20);
			NPCDef looterDef = (NPCDef) ThingDef.GetByDefname("npc_test_looter_attacker");
			NPC looter = (NPC) looterDef.Create(self.X, self.Y, self.Z, self.M);

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			ItemDef daggerDef = (ItemDef) ThingDef.GetByDefname("i_dagger_copper");
			Item loot = (Item) daggerDef.Create(dummy.Backpack);
			//capture position BEFORE death - CauseDeath deletes a non-player NPC outright
			//(see Character.CauseDeath), and a deleted Thing can't have its own X/Y read.
			int dummyX = dummy.X;
			int dummyY = dummy.Y;
			dummy.CauseDeath(null);

			Corpse corpse = null;
			foreach (Thing nearbyThing in self.GetMap().GetThingsInRange(dummyX, dummyY, 0)) {
				Corpse c = nearbyThing as Corpse;
				if (c != null) {
					corpse = c;
					break;
				}
			}

			int itemsBefore = 0;
			if (corpse != null) {
				foreach (Item i in corpse.EnumShallow()) {
					itemsBefore++;
				}
			}

			MonsterSpecialAttacks.TryApplyLoot(looter, corpse);

			bool lootedItem = false;
			foreach (Item i in looter.Backpack.EnumShallow()) {
				if (i == loot) {
					lootedItem = true;
				}
			}

			Console.WriteLine("LOOTER_TEST: itemsBefore=" + itemsBefore + " lootedItem=" + lootedItem);
		}
	}
}
