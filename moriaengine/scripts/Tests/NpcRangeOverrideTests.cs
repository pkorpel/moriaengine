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
	/// #716 - per-monster override for a bow/crossbow NPC's real attack range (NPCDef.
	/// WeaponRange, already existed, but used to SKIP the SetRangedWeaponRange call entirely
	/// once configured above the unconfigured sentinel - leaving StrikeStartRange/StopRange at
	/// NPC.ct's own melee-shaped defaults while WeaponRangeVsM/VsP sat at the configured value, a
	/// real deadlock risk for any override bigger than the unmatched StrikeStartRange). Mutates
	/// the shared NPCDef's WeaponRange directly (same precedent as ArmorEffectTests mutating
	/// dummyDef.ArmorVsP) - always restores it in a finally block so this doesn't leak into any
	/// other test sharing the same chardef.
	/// </summary>
	public static class NpcRangeOverrideTests {
		[SteamFunction]
		public static void TestReportRangerWeaponRangeOverride(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef rangerDef = (NPCDef) ThingDef.GetByDefname("c_titan_ranger");
			int originalRange = rangerDef.WeaponRange;
			try {
				NPC defaultRanger = (NPC) rangerDef.Create(self.X, self.Y, self.Z, self.M);
				int defaultRange = defaultRanger.WeaponRangeVsM;
				int defaultStrikeStart = defaultRanger.WeaponStrikeStartRange;
				int defaultStrikeStop = defaultRanger.WeaponStrikeStopRange;

				rangerDef.WeaponRange = 15;
				NPC overriddenRanger = (NPC) rangerDef.Create(self.X + 1, self.Y, self.Z, self.M);
				int overriddenRange = overriddenRanger.WeaponRangeVsM;
				int overriddenStrikeStart = overriddenRanger.WeaponStrikeStartRange;
				int overriddenStrikeStop = overriddenRanger.WeaponStrikeStopRange;

				Console.WriteLine("WEAPON_RANGE_OVERRIDE_TEST: defaultRange=" + defaultRange +
					" defaultStrikeStart=" + defaultStrikeStart + " defaultStrikeStop=" + defaultStrikeStop +
					" overriddenRange=" + overriddenRange +
					" overriddenStrikeStart=" + overriddenStrikeStart + " overriddenStrikeStop=" + overriddenStrikeStop);
			} finally {
				rangerDef.WeaponRange = originalRange;
			}
		}

		/// <summary>#716 - same idea for a spellcaster's own hold-distance range
		/// (NPCDef.SpellCastRange, new) - drives a real NPCAiTimer.Fight() decision at a fixed,
		/// short distance (6 tiles - same magnitude already proven reliable by
		/// NpcRangedApproachTests, unlike a longer distance which can run into real, unrelated
		/// terrain/LOS obstructions this far from the default spawn point - found the hard way
		/// building this test). Default range (10) should hold at 6 tiles; an override SMALLER
		/// than the current distance (3) should force the caster to close in instead, proving
		/// the override is actually read rather than just accepted and ignored.</summary>
		[SteamFunction]
		public static void TestReportMageSpellCastRangeOverride(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef mageDef = (NPCDef) ThingDef.GetByDefname("c_titan_mage");
			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			double originalRange = mageDef.SpellCastRange;
			try {
				NPC defaultMage = (NPC) mageDef.Create(self.X, self.Y, self.Z, self.M);
				defaultMage.Mana = 0; //forces TryCastSpell's own mana check to fail, so Fight() always falls through to the approach-decision logic this test actually targets, instead of possibly casting (and so never moving) for an unrelated reason
				NPC titan1 = (NPC) titanDef.Create(self.X + 6, self.Y, self.Z, self.M);
				int startX1 = defaultMage.X;
				int startY1 = defaultMage.Y;
				NPCAiTimer.GetFor(defaultMage).Fight(defaultMage, titan1);
				bool defaultApproached = (defaultMage.X != startX1) || (defaultMage.Y != startY1);

				mageDef.SpellCastRange = 3;
				NPC overriddenMage = (NPC) mageDef.Create(self.X, self.Y + 3, self.Z, self.M);
				overriddenMage.Mana = 0;
				NPC titan2 = (NPC) titanDef.Create(self.X + 6, self.Y + 3, self.Z, self.M);
				int startX2 = overriddenMage.X;
				int startY2 = overriddenMage.Y;
				NPCAiTimer.GetFor(overriddenMage).Fight(overriddenMage, titan2);
				bool overriddenApproached = (overriddenMage.X != startX2) || (overriddenMage.Y != startY2);

				Console.WriteLine("SPELLCAST_RANGE_OVERRIDE_TEST: defaultHeld=" + !defaultApproached +
					" overriddenApproached=" + overriddenApproached);
			} finally {
				mageDef.SpellCastRange = originalRange;
			}
		}
	}
}
