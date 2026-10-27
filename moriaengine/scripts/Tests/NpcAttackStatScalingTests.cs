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
	/// #711 - real-user-reported: c_titan_ranger (WeaponAttack=40) dealt exactly 0 damage to
	/// c_titan (ArmorVsM=80) across ~28 real landed shots in a live repro - not RNG, a
	/// deterministic wall. Root cause: CombatCalculator.CalculateArmorClassEffect's formula is a
	/// subtractive floor (max(0, (attack-armor)*damageMod)), and NPCs' attack never got the same
	/// Tactics/Anatomy/ArmsLore/Str scaling the player branch already applies - confirmed via the
	/// real numbers: ranger's computed attack was only ~70.4 against titan's flat 80 armor.
	/// Fixed by sharing that scaling formula with the NPC branch too (user's explicit choice,
	/// accepting that most monsters have far higher Str than a player and so now hit harder
	/// across the board - not attempting a dampened NPC-only modifier).
	///
	/// This reads CombatCalculator.CalculateArmorClassEffect's output directly (via
	/// DamageManager.GetWeaponSwingArgs) rather than sampling real random strokes over time -
	/// the attack/armor numbers themselves are deterministic, only CheckSuccess's hit/miss roll
	/// is random, so this is a faster, non-flaky way to prove the fix than
	/// RangedCombatSustainTests.cs's own real-time sustained-fire test.
	/// </summary>
	public static class NpcAttackStatScalingTests {
		[SteamFunction]
		public static void TestReportRangerVsTitanSwingArgs(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			NPCDef rangerDef = ThingDef.GetByDefname("c_titan_ranger") as NPCDef;
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC ranger = (NPC) rangerDef.Create(self.X, self.Y, self.Z, self.M);
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);

			WeaponSwingArgs args = DamageManager.GetWeaponSwingArgs(ranger, titan);
			Console.WriteLine("RANGER_VS_TITAN_SWING_TEST: attack=" + args.attack +
				" armorClass=" + args.armorClass + " damageAfterAC=" + args.DamageAfterAC);
		}
	}
}
