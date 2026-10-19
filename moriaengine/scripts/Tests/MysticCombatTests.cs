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
	}
}
