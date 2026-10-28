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
	/// #797 - monster abilities batch 4. Server-side helpers for
	/// Tests/Protocol/MonsterAbilitiesBatch4Tests.cs, same "deterministic direct call"
	/// precedent as batches 1-3. Several abilities per helper, so the protocol class stays
	/// within one account's 5 character slots.
	/// </summary>
	public static class MonsterAbilitiesBatch4Tests {
		private static NPC Spawn(Character self, string defname, int dx) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname(defname);
			return (NPC) def.Create(self.X + dx, self.Y, self.Z, self.M);
		}

		private static void FullHealth(Character ch, short hits) {
			ch.MaxHits = hits;
			ch.Hits = hits;
		}

		/// <summary>Reactive armor, stamina drain, mana gain, auto reflect, life leech, summon
		/// devourer - the swing-reaction abilities.</summary>
		[SteamFunction]
		public static void TestBatch4SwingReactions(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			PetCommandTests.TestSetGmOff(self);
			FullHealth(self, 1000);

			NPC defender = Spawn(self, "npc_test_b4_defender", 1);
			int selfHitsBefore = self.Hits;
			int reflected = MonsterSpecialAttacks.TryApplyReactiveArmor(self, defender, DamageType.PhysicalBlunt, 100);
			bool selfHurt = self.Hits < selfHitsBefore;

			self.Stam = 50;
			int drained = MonsterSpecialAttacks.TryApplyStamDmg(self, defender);
			int stamAfter = self.Stam;

			FullHealth(defender, 1000);
			defender.MaxMana = 400;
			defender.Mana = 0;
			defender.Hits = 970;
			int gain1 = MonsterSpecialAttacks.TryApplyManaGain(defender, 30); //lost 3% - not yet
			defender.Hits = 900;
			int gain2 = MonsterSpecialAttacks.TryApplyManaGain(defender, 70); //10% since the mark

			bool reflectArmed = MonsterSpecialAttacks.TryApplyAutoReflect(defender);
			bool hasReflection = MagicReflectionPluginDef.HasMagicReflection(defender);
			bool reflectArmedAgain = MonsterSpecialAttacks.TryApplyAutoReflect(defender);

			NPC leecher = Spawn(self, "npc_test_b4_leecher", 2);
			FullHealth(leecher, 1000);
			leecher.Hits = 500;
			int leeched = MonsterSpecialAttacks.TryApplyLifeLeech(leecher);

			NPC summon = Spawn(self, "c_a_jackrabbit", 3);
			SummonSpellDef.MarkAsSummonedCreature(summon);
			leecher.Str = 100;
			int expectedStr = 100 + summon.Str;
			bool eaten = MonsterSpecialAttacks.TryApplySummonEater(leecher, summon);

			Console.WriteLine("B4_SWING_TEST: reflected=" + (reflected > 0) + " selfHurt=" + selfHurt +
				" drained=" + drained + " stamAfter=" + stamAfter +
				" gain1=" + gain1 + " gain2=" + gain2 +
				" reflectArmed=" + reflectArmed + " hasReflection=" + hasReflection + " reflectArmedAgain=" + reflectArmedAgain +
				" leeched=" + leeched + " eaten=" + eaten + " summonDeleted=" + summon.IsDeleted +
				" strOk=" + (leecher.Str == expectedStr));
		}

		/// <summary>Hazard tiles (stander + step-on), lightnings, explode on death.</summary>
		[SteamFunction]
		public static void TestBatch4HazardsAndExplode(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			PetCommandTests.TestSetGmOff(self);
			FullHealth(self, 1000);
			NPC owner = Spawn(self, "npc_test_b4_pusher", 4);

			int before = self.Hits;
			MonsterSpecialAttacks.PlaceHazardTile(owner, self.X, self.Y, 30, DamageType.MagicFire, false);
			bool standerHit = self.Hits < before;

			int x = self.X, y = self.Y, z = self.Z;
			MonsterSpecialAttacks.PlaceHazardTile(owner, x + 1, y, 30, DamageType.MagicFire, false);
			int liveTiles = MonsterSpecialAttacks.CountLiveHazardTiles();
			before = self.Hits;
			self.P(x + 1, y, z);
			MonsterSpecialAttacks.OnStepTaken(self);
			bool stepperHit = self.Hits < before;
			int liveTilesAfterStep = MonsterSpecialAttacks.CountLiveHazardTiles();
			self.P(x, y, z);

			before = self.Hits;
			MonsterSpecialAttacks.ForceLightnings(owner, self.X, self.Y, true);
			bool lightningHit = self.Hits < before;

			FullHealth(self, 1000);
			NPC exploder = Spawn(self, "npc_test_b4_exploder", 2);
			FullHealth(exploder, 1000); //so the 1-damage poke below can't already kill it
			DamageManager.CauseDamage(self, exploder, DamageType.PhysicalBlunt, 1);
			int expected = 60 / Point2D.GetSimpleDistance(self, exploder);
			before = self.Hits;
			exploder.CauseDeath(self);
			int explodeDamage = before - self.Hits;

			Console.WriteLine("B4_HAZARD_TEST: standerHit=" + standerHit + " liveTiles=" + liveTiles +
				" stepperHit=" + stepperHit + " liveTilesAfterStep=" + liveTilesAfterStep +
				" lightningHit=" + lightningHit + " explodeOk=" + (explodeDamage == expected) +
				" explodeDamage=" + explodeDamage + " expected=" + expected);
		}

		/// <summary>Push all, the mana/stamina curses, and Dark Curse's "spare the current
		/// target" rule (on by default, switchable on .settings).</summary>
		[SteamFunction]
		public static void TestBatch4PushAndCurses(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			PetCommandTests.TestSetGmOff(self);
			FullHealth(self, 1000);

			NPC pusher = Spawn(self, "npc_test_b4_pusher", -1); //same geometry as the knockback tests (push east)
			int distBefore = Point2D.GetSimpleDistance(self, pusher);
			int pushed = MonsterSpecialAttacks.ForcePushall(pusher);
			int distAfter = Point2D.GetSimpleDistance(self, pusher);
			pusher.Delete();

			NPC manaCurser = Spawn(self, "npc_test_b4_cursemana", 2);
			NPC pet = Spawn(self, "c_a_jackrabbit", 1);
			pet.ControlMaster = self;
			pet.MaxMana = 500;
			pet.Mana = 500;
			self.Mana = self.MaxMana;
			int selfManaBefore = self.Mana;
			MonsterSpecialAttacks.TryApplyNpcCurseMana(manaCurser);
			int selfManaLost = selfManaBefore - self.Mana;
			int expectedSelfManaLost = Math.Min(30, selfManaBefore); //can't drain more than it has
			int petManaLost = 500 - pet.Mana;
			manaCurser.Delete();
			//#798 - deleting a pet while its owner is online hangs the server; release it first
			pet.ControlMaster = null;
			pet.Delete();

			self.Stam = self.MaxStam;
			int selfStamBefore = self.Stam;
			NPC stamCurser = Spawn(self, "npc_test_b4_cursestam", 2);
			MonsterSpecialAttacks.TryApplyNpcCurseStam(stamCurser);
			int selfStamLost = selfStamBefore - self.Stam;
			stamCurser.Delete();

			NPC darkCurser = Spawn(self, "npc_test_npccurse_attacker", 2);
			NPCAiTimer.EnsureInstalled(darkCurser);
			NPCAiTimer.GetFor(darkCurser).EngageTarget(self);
			int before = self.Hits;
			MonsterSpecialAttacks.TryApplyNpcCurse(darkCurser);
			bool targetSpared = self.Hits == before;

			bool oldRule = MonsterAbilitiesSettings.instance.NpcCurseSpareCurrentTarget;
			MonsterAbilitiesSettings.instance.NpcCurseSpareCurrentTarget = false;
			before = self.Hits;
			MonsterSpecialAttacks.TryApplyNpcCurse(darkCurser);
			bool targetHitWithRuleOff = self.Hits < before;
			MonsterAbilitiesSettings.instance.NpcCurseSpareCurrentTarget = oldRule;
			darkCurser.Delete();

			Console.WriteLine("B4_CURSE_TEST: pushed=" + pushed + " pushedAway=" + (distAfter > distBefore) +
				" selfManaOk=" + (selfManaLost == expectedSelfManaLost) + " selfManaLost=" + selfManaLost + " petManaLost=" + petManaLost + " selfStamLost=" + selfStamLost +
				" targetSpared=" + targetSpared + " targetHitWithRuleOff=" + targetHitWithRuleOff);
		}

		/// <summary>Phoenix rebirth (twice, then no more), dragon regeneration, inexhaustible.</summary>
		[SteamFunction]
		public static void TestBatch4PhoenixRegenInexhaustible(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC phoenix = Spawn(self, "npc_test_b4_phoenix", 1);
			FullHealth(phoenix, 1000);
			phoenix.Hits = 100;
			bool rose1 = MonsterSpecialAttacks.TryApplyPhoenixRebirth(phoenix);
			int max1 = phoenix.MaxHits, hits1 = phoenix.Hits;
			phoenix.Hits = 50;
			bool rose2 = MonsterSpecialAttacks.TryApplyPhoenixRebirth(phoenix);
			int max2 = phoenix.MaxHits;
			phoenix.Hits = 10;
			bool rose3 = MonsterSpecialAttacks.TryApplyPhoenixRebirth(phoenix);

			phoenix.Hits = 100;
			int regen1 = MonsterSpecialAttacks.TryApplyDragonRegeneration(phoenix); //schedules the first pulse
			int regen2 = MonsterSpecialAttacks.TryApplyDragonRegeneration(phoenix); //interval 0 - due now

			NPC walker = Spawn(self, "npc_test_b4_leecher", 2);
			walker.MaxStam = 100;
			walker.Stam = 10;
			int stamGained = MonsterSpecialAttacks.TryApplyInexhaustible(walker);

			Console.WriteLine("B4_PHOENIX_TEST: rose1=" + rose1 + " max1=" + max1 + " hits1=" + hits1 +
				" rose2=" + rose2 + " max2=" + max2 + " rose3=" + rose3 +
				" regen1=" + regen1 + " regen2=" + regen2 + " stamGained=" + stamGained);
		}
	}
}
