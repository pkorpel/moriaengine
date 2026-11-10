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

	/// <summary>Real-user-reported: an NPC (e.g. c_titan_mage) casting a beneficial spell (Magic
	/// Reflection) aimed it straight at its enemy target instead of itself, same as an offensive
	/// spell. npc_test_beneficial_caster's ONLY configured spell is s_magic_reflection - no
	/// randomness to account for, unlike c_titan_mage's real 3-spell mix. Verification is done
	/// client-side (Tests/Protocol/NpcBeneficialSpellTests.cs) by reading NPCAiTimer.
	/// TryCastSpell's own debug log line directly ("casts Magic Reflect at <name>") rather than
	/// the resulting MagicReflectionPlugin installation (when this was written, "p_magic_reflection" didn't
	/// exist on a cold boot; fixed by #927 and #1369), so the test stays about the targeting.</summary>
	public static class NpcBeneficialSpellTests {
		[SteamFunction]
		public static void TestBeneficialSpellCastsOnSelf(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef casterDef = ThingDef.GetByDefname("npc_test_beneficial_caster") as NPCDef;
			NPC caster = (NPC) casterDef.Create(self.X + 1, self.Y, self.Z, self.M);
			//c_a_jackrabbit, not c_sheep_woolly - the latter's own Color field uses a weighted
			//brace-list syntax ("{ colors_white 9 color_gray_lt 1 }") that hits the pre-existing,
			//already-tracked #616 lazy-FieldValue-parse bug the first time ANY instance of it is
			//ever created in a fresh server boot - unrelated to this test, not fixed here.
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;

			NPCAiTimer casterTimer = NPCAiTimer.GetFor(caster);
			casterTimer.EngageTarget(dummy);

			Console.WriteLine("BENEFICIAL_SPELL_TEST: STARTED caster=" + caster.Uid + " dummy=" + dummy.Uid);
		}
	}
}
