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
	/// #799 - monster abilities batch 5. Server-side helpers for
	/// Tests/Protocol/MonsterAbilitiesBatch5Tests.cs, same "deterministic direct call"
	/// precedent as the earlier batches. Timed abilities get their delays set to 0 through
	/// .settings for the test and restored afterwards.
	/// </summary>
	public static class MonsterAbilitiesBatch5Tests {
		private static NPC Spawn(Character self, string defname, int dx) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname(defname);
			return (NPC) def.Create(self.X + dx, self.Y, self.Z, self.M);
		}

		private static void FullHealth(Character ch, short hits) {
			ch.MaxHits = hits;
			ch.Hits = hits;
		}

		/// <summary>Eye, golem, craft monster, duplicator, multishot.</summary>
		[SteamFunction]
		public static void TestBatch5SwingAbilities(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			PetCommandTests.TestSetGmOff(self);
			FullHealth(self, 1000);
			MonsterAbilitiesSettings settings = MonsterAbilitiesSettings.instance;

			NPC eye = Spawn(self, "npc_test_b5_eye", 1);
			FullHealth(eye, 1000);
			eye.Hits = 500;
			self.Stam = self.MaxStam;
			self.Mana = self.MaxMana;
			int stamBefore = self.Stam, manaBefore = self.Mana;
			int leeched = MonsterSpecialAttacks.TryApplyEyeDrain(eye, self, 10);
			bool stamSapped = self.Stam < stamBefore;
			bool manaPaid = self.Mana == manaBefore - 4;
			eye.Delete();

			NPC golem = Spawn(self, "npc_test_b5_golem", 1);
			FullHealth(golem, 1000);
			double oldGolemChance = settings.GolemChanceAbove75;
			settings.GolemChanceAbove75 = 1000;
			bool golemTurned = MonsterSpecialAttacks.TryApplyGolemRetarget(self, golem);
			NPCAiTimer ai = NPCAiTimer.GetFor(golem);
			bool golemTargetsMe = (ai != null) && (ai.Target == self);
			settings.GolemChanceAbove75 = oldGolemChance;

			int bonusAsOther = MonsterSpecialAttacks.TryApplyCraftMonster(self, golem);
			Player player = (Player) self;
			ProfessionDef oldProfession = player.Profession;
			ProfessionDef.SetProfessionOfChar(player, Professions.Craftsman);
			int bonusAsCraftsman = MonsterSpecialAttacks.TryApplyCraftMonster(self, golem);
			ProfessionDef.SetProfessionOfChar(player, oldProfession);
			golem.Delete();

			NPC duplicator = Spawn(self, "npc_test_b5_duplicator", 2);
			FullHealth(duplicator, 1000);
			NPC copy = MonsterSpecialAttacks.TryApplyDuplicator(self, duplicator);
			NPC second = MonsterSpecialAttacks.TryApplyDuplicator(self, duplicator);
			NPC copyOfCopy = (copy == null) ? null : MonsterSpecialAttacks.TryApplyDuplicator(self, copy);
			bool sameKind = (copy != null) && (copy.Def == duplicator.Def);
			duplicator.Delete();
			if (copy != null) {
				copy.Delete();
			}

			FullHealth(self, 1000);
			NPC archer = Spawn(self, "npc_test_b5_multishot", 2);
			int before = self.Hits;
			bool shot = MonsterSpecialAttacks.TryApplyMultishot(archer);
			bool shotHurt = self.Hits < before;
			archer.Delete();

			Console.WriteLine("B5_SWING_TEST: leeched=" + leeched + " stamSapped=" + stamSapped + " manaPaid=" + manaPaid +
				" golemTurned=" + golemTurned + " golemTargetsMe=" + golemTargetsMe +
				" bonusAsOther=" + bonusAsOther + " craftsmanBonusOk=" + (bonusAsCraftsman >= 10) +
				" copied=" + (copy != null) + " sameKind=" + sameKind + " secondCopy=" + (second != null) +
				" copyOfCopy=" + (copyOfCopy != null) + " shot=" + shot + " shotHurt=" + shotHurt);
		}

		/// <summary>Backstab, mass leap, dispel bomb, warcry.</summary>
		[SteamFunction]
		public static void TestBatch5TimedAbilities(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			PetCommandTests.TestSetGmOff(self);
			FullHealth(self, 1000);
			MonsterAbilitiesSettings settings = MonsterAbilitiesSettings.instance;

			double oldHideDelay = settings.BackstabHideDelay;
			settings.BackstabHideDelay = 0;
			NPC stabber = Spawn(self, "npc_test_b5_backstab", 1);
			bool hiddenAtOnce = MonsterSpecialAttacks.TryBackstabHide(stabber); //notices me, starts hiding
			bool hidden = MonsterSpecialAttacks.TryBackstabHide(stabber); //0 s later: hidden
			int ambush = MonsterSpecialAttacks.TryApplyBackstabAmbush(stabber, self, 100);
			bool revealed = !stabber.Flag_Hidden;
			NPC stabber2 = Spawn(self, "npc_test_b5_backstab", -1);
			settings.BackstabHideDelay = 60;
			MonsterSpecialAttacks.TryBackstabHide(stabber2);
			bool interrupted = MonsterSpecialAttacks.TryInterruptBackstabHide(stabber2);
			settings.BackstabHideDelay = 0;
			bool hidesRightAfterInterrupt = MonsterSpecialAttacks.TryBackstabHide(stabber2) || MonsterSpecialAttacks.TryBackstabHide(stabber2);
			settings.BackstabHideDelay = oldHideDelay;
			stabber.Delete();
			stabber2.Delete();

			double oldFirstDelay = settings.MassLeapFirstDelay;
			settings.MassLeapFirstDelay = 0;
			NPC leaper = Spawn(self, "npc_test_b5_leaper", 5);
			int marked = MonsterSpecialAttacks.ForceMassLeap(leaper);
			int hops = MonsterSpecialAttacks.TryMassLeapTick(leaper);
			int leaperDistance = Point2D.GetSimpleDistance(self, leaper);
			settings.MassLeapFirstDelay = oldFirstDelay;

			int shocked = MonsterSpecialAttacks.TryApplyWarcry(leaper);
			int shockedAgain = MonsterSpecialAttacks.TryApplyWarcry(leaper);
			leaper.Delete();

			double oldFuse = settings.DispelBombFuse;
			settings.DispelBombFuse = 0;
			NPC bomber = Spawn(self, "npc_test_b5_bomber", 3);
			NPC summon = Spawn(self, "c_a_jackrabbit", 1); //no owner - an owned one would hit #798
			SummonSpellDef.MarkAsSummonedCreature(summon);
			MonsterSpecialAttacks.ThrowDispelBomb(bomber, self.X, self.Y);
			int dispelled = MonsterSpecialAttacks.TryDispelBombTick(bomber);
			bool summonGone = summon.IsDeleted;
			settings.DispelBombFuse = oldFuse;
			bomber.Delete();

			Console.WriteLine("B5_TIMED_TEST: hiddenAtOnce=" + hiddenAtOnce + " hidden=" + hidden + " ambush=" + ambush +
				" revealed=" + revealed + " interrupted=" + interrupted + " hidesRightAfterInterrupt=" + hidesRightAfterInterrupt +
				" marked=" + (marked >= 1) + " hops=" + hops + " leaperNear=" + (leaperDistance <= 1) +
				" shocked=" + (shocked >= 1) + " shockedAgain=" + shockedAgain +
				" dispelled=" + (dispelled >= 2) + " summonGone=" + summonGone);
		}
	}
}
