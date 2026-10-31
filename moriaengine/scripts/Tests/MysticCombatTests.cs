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
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side helpers for Mystic's mind-power attack (#447/#448/#449). Uses
	/// DamageManager.GetWeaponSwingArgs/ProcessSwing directly rather than waiting on a real
	/// WeaponSkillDef stroke loop - the formula itself is pure arithmetic (no dice roll involved
	/// the way a melee CheckSuccess is), so a direct call is fully deterministic and avoids
	/// real-swing timing entirely, same spirit as #96's own investigation.
	///
	/// EvalInt.effect/SpiritSpeak.effect/MagicResist.effect are all currently 0 (#309 - no
	/// content anywhere ever sets effect= for these skills), so these tests deliberately don't
	/// rely on skill values to move the numbers - mind power is driven via Int directly (the one
	/// real contributing term today), and mind defense is driven via the dummy's
	/// MindDefenseModifier (a flat, always-live override independent of the #309 gap, not the
	/// skill-based term).
	/// </summary>
	public static class MysticCombatTests {

		/// <summary>Confirms mindPowerVsM actually reaches DamageManager's computed damage and
		/// scales with Int (the formula's one real contributing term while #309 is open) -
		/// guards specifically against the #96/#309-style failure mode where a computed combat
		/// value never reaches the final result.</summary>
		[SteamFunction]
		public static void TestMysticMindPowerScalesWithInt(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm(); // bare-hands mind power fallback (MagerySettings.bareHandsMindPowerVsM) - not weapon-content-dependent for this check

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);

			self.Int = 50;
			double mindPowerLow = self.MindPowerVsM;
			double damageLow = DamageManager.GetWeaponSwingArgs(self, dummy).DamageAfterAC;

			self.Int = 300;
			double mindPowerHigh = self.MindPowerVsM;
			double damageHigh = DamageManager.GetWeaponSwingArgs(self, dummy).DamageAfterAC;

			Console.WriteLine("MYSTIC_MINDPOWER_SCALES_TEST: isMystic=" + Professions.HasProfession(self, Professions.Mystic) +
				" mindPowerLow=" + mindPowerLow + " damageLow=" + damageLow +
				" mindPowerHigh=" + mindPowerHigh + " damageHigh=" + damageHigh);
		}

		/// <summary>Confirms mindDefenseVsM (defender side) actually reduces the Mystic's
		/// damage, via MindDefenseModifier rather than the MagicResist skill (same #309 reason
		/// as above) - proves the subtractive mindPower-minus-mindDefense formula (#449) rather
		/// than just that SOME number comes out non-zero.</summary>
		[SteamFunction]
		public static void TestMysticMindDefenseReducesDamage(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 300;

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummyLowDefense = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummyHighDefense = (NPC) dummyDef.Create(self.X + 1, self.Y + 1, self.Z, self.M);
			dummyHighDefense.MindDefenseModifier = 100;

			double damageAgainstLowDefense = DamageManager.GetWeaponSwingArgs(self, dummyLowDefense).DamageAfterAC;
			double damageAgainstHighDefense = DamageManager.GetWeaponSwingArgs(self, dummyHighDefense).DamageAfterAC;

			Console.WriteLine("MYSTIC_MINDDEFENSE_TEST: mindPowerVsM=" + self.MindPowerVsM +
				" mindDefenseLow=" + dummyLowDefense.MindDefenseVsM + " damageAgainstLowDefense=" + damageAgainstLowDefense +
				" mindDefenseHigh=" + dummyHighDefense.MindDefenseVsM + " damageAgainstHighDefense=" + damageAgainstHighDefense);
		}

		/// <summary>Confirms a Mystic's swing is actually resisted as magic/Mystical damage
		/// (DamageType.MagicMystical), not physical - a defender with maxed physical resist but
		/// zero magic/mystical resist should take full damage, and the reverse should take
		/// (near) none. Goes through the full ProcessSwing/CauseDamage pipeline (not just
		/// GetWeaponSwingArgs) since GetResistModifier - the thing actually being checked here -
		/// only runs inside CauseDamage. Uses c_titan (not the sheep dummy above) purely for its
		/// much larger hit pool, so neither measurement risks the target dying mid-test.</summary>
		[SteamFunction]
		public static void TestMysticDamageIsMagicalNotPhysical(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 300;

			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC physicalResistDummy = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			physicalResistDummy.ResistPhysical = 1000;
			physicalResistDummy.ResistMagic = 0;
			physicalResistDummy.ResistMystical = 0;

			NPC magicResistDummy = (NPC) titanDef.Create(self.X + 1, self.Y + 1, self.Z, self.M);
			magicResistDummy.ResistPhysical = 0;
			magicResistDummy.ResistMagic = 1000;
			magicResistDummy.ResistMystical = 1000;

			int physicalResistHitsBefore = physicalResistDummy.Hits;
			DamageManager.ProcessSwing(self, physicalResistDummy);
			int physicalResistDamage = physicalResistHitsBefore - physicalResistDummy.Hits;

			int magicResistHitsBefore = magicResistDummy.Hits;
			DamageManager.ProcessSwing(self, magicResistDummy);
			int magicResistDamage = magicResistHitsBefore - magicResistDummy.Hits;

			Console.WriteLine("MYSTIC_DAMAGETYPE_TEST: physicalResistDamage=" + physicalResistDamage +
				" magicResistDamage=" + magicResistDamage);
		}

		/// <summary>Regression test for #511/#514's latent 3rd instance - CalculateMindPowerEffect
		/// has the identical unclamped `(mindPower - mindDefense) * damageMod` shape as the
		/// physical path, so a Mystic attacking a defender with very high MindDefenseModifier
		/// could also "heal" the target before this fix. Note the pre-existing
		/// TestMysticDamageIsMagicalNotPhysical above (ResistMagic/ResistMystical=1000) never
		/// actually caught this - it only asserts magicResistDamage &lt; physicalResistDamage,
		/// which is trivially true whether magicResistDamage is 0 or very negative.</summary>
		[SteamFunction]
		public static void TestMysticNeverHealsDefender(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 10; // weak mind power

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.MindDefenseModifier = 1000000; // dwarfs any possible mindPower value
			dummy.Hits = dummy.MaxHits;

			int before = dummy.Hits;
			for (int i = 0; i < 20; i++) {
				DamageManager.ProcessSwing(self, dummy);
			}
			int after = dummy.Hits;
			dummy.MindDefenseModifier = 0;

			Console.WriteLine("MYSTIC_NEVERHEALS_TEST: before=" + before + " after=" + after);
		}

		/// <summary>#868 - Astral Power and the attack switch: the strike's damage range and type.</summary>
		[SteamFunction]
		public static void TestMysticAbilitiesReport(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player player = (Player) self;
			player.Profession = Professions.Mystic;
			player.Level = 60;
			self.DisArm();
			self.Int = 300;
			self.Stam = 1000;
			NPC dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_resist")).Create(self.X + 1, self.Y, self.Z, self.M);

			double plain = DamageManager.GetWeaponSwingArgs(self, dummy).DamageAfterAC;
			bool inactive = MysticAbilities.IsAstralPowerActive(self);
			MysticAbilities.AstralPower(self);
			bool active = MysticAbilities.IsAstralPowerActive(self);
			double smallest = double.MaxValue;
			for (int i = 0; i < 50; i++) {
				smallest = Math.Min(smallest, DamageManager.GetWeaponSwingArgs(self, dummy).DamageAfterAC);
			}
			MysticAbilities.SwitchMagic(self);
			bool lightning = MysticAbilities.StrikesWithLightning(self);
			MysticAbilities.SwitchMagic(self);
			MysticAbilities.AstralPower(self);
			bool off = MysticAbilities.IsAstralPowerActive(self);
			dummy.Delete();
			Console.WriteLine("MYSTIC_ABILITIES: inactive=" + inactive + " active=" + active + " off=" + off + " lightning=" + lightning +
				" plain=" + ((int) plain) + " special=" + ((int) smallest));
		}

		/// <summary>#450 - confirms WeaponDelay is mana-driven for a Mystic (shorter delay =
		/// faster swings at higher mana) rather than the normal Dex-based formula every other
		/// profession uses - and that a non-Mystic's delay is completely unaffected by mana,
		/// as a regression guard on the branch added to CalculateCombatWeaponValues.</summary>
		[SteamFunction]
		public static void TestMysticSwingSpeedScalesWithMana(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 300; // MaxMana == Int for players (Player.cs) - gives Mana plenty of headroom

			self.Mana = 10;
			double delayLowManaMs = self.WeaponDelay.TotalMilliseconds;

			self.Mana = 200;
			double delayHighManaMs = self.WeaponDelay.TotalMilliseconds;

			((Player) self).Profession = null;
			self.Mana = 10;
			double nonMysticDelayLowManaMs = self.WeaponDelay.TotalMilliseconds;
			self.Mana = 200;
			double nonMysticDelayHighManaMs = self.WeaponDelay.TotalMilliseconds;

			Console.WriteLine("MYSTIC_SWINGSPEED_TEST: delayLowMana=" + delayLowManaMs +
				" delayHighMana=" + delayHighManaMs +
				" nonMysticDelayLowMana=" + nonMysticDelayLowManaMs +
				" nonMysticDelayHighMana=" + nonMysticDelayHighManaMs);
		}

		/// <summary>#450 - confirms a landed Mystic swing actually drains mana (the natural
		/// throttle - no hard mana gate on this attack per #447's research).</summary>
		[SteamFunction]
		public static void TestMysticSwingDrainsMana(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 300;
			self.Mana = 200;

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);

			int manaBefore = self.Mana;
			DamageManager.ProcessSwing(self, dummy);
			int manaAfter = self.Mana;

			Console.WriteLine("MYSTIC_MANADRAIN_TEST: manaBefore=" + manaBefore + " manaAfter=" + manaAfter);
		}

		/// <summary>#451 - confirms WeaponSkillDef's existing Drawing->Striking loop (already
		/// generic, no code change needed - see #451's own research) works end-to-end for a
		/// REAL player attack (Trigger_PlayerAttackRequest, the same path a live client's
		/// "attack" uses), not just a direct formula call like the other tests in this file.
		/// Uses CombatSettings.Ranges.mystic (CombatCalculator's own profession-level range
		/// override, not a per-weapon-model one - a Mystic's real reach is their mind power, not
		/// whatever staff they happen to hold) - mutates it directly for this one test and
		/// restores it in the check timer, same spirit as TestGuardsRealmAggro's direct NPCDef
		/// mutation elsewhere in this test suite.</summary>
		[SteamFunction]
		public static void TestMysticStandsOffAtRange(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 300;
			self.Mana = 200;

			// Real mechanism now (CombatSettings.Ranges.mystic, CombatCalculator's own
			// profession-level range override) rather than the old bareHandsRange workaround -
			// explicitly set here (even though the default is already 8) so this test doesn't
			// depend on whatever a GM happens to have it configured to at the time.
			int originalRange = RangesSettings.instance.mystic;
			RangesSettings.instance.mystic = 8;

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 5, self.Y, self.Z, self.M); // beyond melee (1), within the Mystic range setting (8)

			int startX = self.X;
			int startY = self.Y;
			int startingHits = dummy.Hits;

			self.Trigger_PlayerAttackRequest(dummy);

			self.AddTimer(MysticStandoffCheckTimer.TimerKey,
				new MysticStandoffCheckTimer(self, dummy, startX, startY, startingHits, originalRange))
				.DueInSeconds = 15;
			Console.WriteLine("MYSTIC_STANDOFF_TEST: STARTED");
		}

		/// <summary>Testing combat messages (CombatSettings.showCombatMessages) - the feature
		/// itself is profession-agnostic, Mystic is just reused test setup here since it's
		/// already wired up and conveniently also exercises the mana-drain message. Sets up a
		/// real adjacent attack (Trigger_PlayerAttackRequest) and lets it run - the xUnit test
		/// reads the resulting system-message packets directly off the wire rather than relying
		/// on server-console markers, since SysMessage has no console-log equivalent.</summary>
		[SteamFunction]
		public static void TestCombatMessagesSetup(Character self, ScriptArgs sa) {
			bool enabled = bool.Parse(Convert.ToString(sa.Argv[0]));
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			CombatSettings.instance.showCombatMessages = enabled;
			((Player) self).Profession = Professions.Mystic;
			self.DisArm();
			self.Int = 300;
			self.Mana = 200;

			// Calm c_sheep_woolly, not a Hostile monster - this runs a real sustained multi-
			// second fight (Trigger_PlayerAttackRequest), unlike the other direct ProcessSwing-
			// call tests in this file, so the dummy must not be able to aggro/retaliate.
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);

			self.Trigger_PlayerAttackRequest(dummy);
			Console.WriteLine("COMBAT_MESSAGES_TEST: STARTED");
		}

		/// <summary>Same feature, the "too far" branch specifically - target placed far beyond
		/// bareHandsStrikeStartRange (5) and never moved, so the Drawing-phase retry loop keeps
		/// finding it out of range (players don't auto-path toward a target the way NPCs do).</summary>
		[SteamFunction]
		public static void TestCombatMessagesTooFarSetup(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			CombatSettings.instance.showCombatMessages = true;
			self.DisArm();

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 20, self.Y, self.Z, self.M); // well beyond bareHandsStrikeStartRange (5)

			self.Trigger_PlayerAttackRequest(dummy);
			Console.WriteLine("COMBAT_MESSAGES_TOOFAR_TEST: STARTED");
		}
	}

	/// <summary>Mirrors ArcherStandoffCheckTimer's pattern (CombatDepthTests.cs) for a player
	/// attacker instead of an NPC, and restores the RangesSettings.mystic override
	/// TestMysticStandsOffAtRange made before reporting.</summary>
	[SaveableClass]
	public class MysticStandoffCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_mysticStandoffCheckTimer_");

		private Character attacker;
		private NPC dummy;
		private int startX, startY, startingHits;
		private int originalMysticRange;

		[LoadingInitializer]
		public MysticStandoffCheckTimer() {
		}

		public MysticStandoffCheckTimer(Character attacker, NPC dummy, int startX, int startY, int startingHits, int originalMysticRange) {
			this.attacker = attacker;
			this.dummy = dummy;
			this.startX = startX;
			this.startY = startY;
			this.startingHits = startingHits;
			this.originalMysticRange = originalMysticRange;
		}

		protected override void OnTimeout(TagHolder cont) {
			RangesSettings.instance.mystic = this.originalMysticRange;

			bool stayedPut = !this.attacker.IsDeleted && (this.attacker.X == this.startX) && (this.attacker.Y == this.startY);
			bool dealtDamage = this.dummy.IsDeleted || (this.dummy.Hits < this.startingHits);

			if (stayedPut && dealtDamage) {
				Console.WriteLine("MYSTIC_STANDOFF_TEST: PASS (mystic stayed at range and landed damage)");
			} else if (!stayedPut) {
				Console.WriteLine("MYSTIC_STANDOFF_TEST: FAIL (mystic moved from (" + this.startX + "," + this.startY + ") to (" +
					(this.attacker.IsDeleted ? -1 : this.attacker.X) + "," + (this.attacker.IsDeleted ? -1 : this.attacker.Y) + "))");
			} else {
				Console.WriteLine("MYSTIC_STANDOFF_TEST: FAIL (stayed at range but dealt no damage)");
			}
		}
	}
}
