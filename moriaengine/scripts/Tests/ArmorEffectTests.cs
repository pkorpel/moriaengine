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
	/// Regression tests for #96 (ArmorVsP/M discarded for every case except monster-attacks-
	/// player). Covers player-attacks-monster specifically - the single most common combat
	/// scenario, and the one directly blocking Ranger's resistance-correctness phase (#458).
	/// Calls DamageManager.GetWeaponSwingArgs directly across many trials (same direct-call
	/// harness style used throughout this combat work) to average out the swingDamage*
	/// randomization rather than asserting on one noisy swing.
	/// </summary>
	public static class ArmorEffectTests {

		[SteamFunction]
		public static void TestArmorReducesPvMDamage(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.DisArm();
			self.Str = 300;

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			int originalArmorVsP = dummyDef.ArmorVsP;

			dummyDef.ArmorVsP = 0;
			dummy.InvalidateCombatArmorValues(); // mutating the shared Def's raw field has no trigger path to invalidate this instance's cache
			double totalUnarmored = 0;
			for (int i = 0; i < 500; i++) {
				totalUnarmored += DamageManager.GetWeaponSwingArgs(self, dummy).DamageAfterAC;
			}

			dummyDef.ArmorVsP = 10;
			dummy.InvalidateCombatArmorValues();
			double totalArmored = 0;
			for (int i = 0; i < 500; i++) {
				totalArmored += DamageManager.GetWeaponSwingArgs(self, dummy).DamageAfterAC;
			}

			dummyDef.ArmorVsP = originalArmorVsP; // restore the shared Def

			Console.WriteLine("ARMOREFFECT_PVM_TEST: avgUnarmored=" + (totalUnarmored / 500) +
				" avgArmored=" + (totalArmored / 500));
		}

		/// <summary>Confirms the already-working monster-attacks-player case wasn't broken by
		/// removing the isMvP branch during the #96 fix - same methodology, roles reversed.</summary>
		[SteamFunction]
		public static void TestArmorReducesMvPDamage(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);

			self.MindDefenseModifier = 0; // unrelated to armor, just keeping self's state explicit
			double totalUnarmored = 0;
			for (int i = 0; i < 500; i++) {
				totalUnarmored += DamageManager.GetWeaponSwingArgs(titan, self).DamageAfterAC;
			}

			self.ArmorClassModifier = 20; // flat override, same tag-backed mechanism Mystic's MindDefenseModifier uses - avoids needing a real equip for this check
			double totalArmored = 0;
			for (int i = 0; i < 500; i++) {
				totalArmored += DamageManager.GetWeaponSwingArgs(titan, self).DamageAfterAC;
			}
			self.ArmorClassModifier = 0;

			Console.WriteLine("ARMOREFFECT_MVP_TEST: avgUnarmored=" + (totalUnarmored / 500) +
				" avgArmored=" + (totalArmored / 500));
		}
	}
}
