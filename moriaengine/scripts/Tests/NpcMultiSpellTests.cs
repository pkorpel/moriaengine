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
	/// Server-side setup helper for #564's protocol test (Tests/Protocol/
	/// NpcMultiSpellTests.cs) - samples NPCAiTimer.PickSpell directly (internal, not
	/// private, specifically so this test can call it many times without needing real
	/// combat timing/cooldowns) against npc_test_multi_spell
	/// (scripts/Tests/NpcMultiSpellTest.def, two AddSpell entries) and
	/// npc_test_single_combat_spell (CombatSpell only, no AddSpell at all).
	/// </summary>
	public static class NpcMultiSpellTests {
		[SteamFunction]
		public static void TestSampleSpellPickDistribution(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_multi_spell");
			//compare by reference against the resolved SpellDef, not by Defname/PrettyDefname
			//string - these are numeric-header defs (e.g. "[DamageSpellDef 5] DEFNAME=
			//s_magic_arrow"), and AbstractScript.PrettyDefname reports the object's own PRIMARY
			//name, not whichever DEFNAME= alias was used to look it up (same gotcha documented
			//elsewhere this session for c_titan-style aliased chardefs).
			SpellDef magicArrow = SpellDef.GetByDefname("s_magic_arrow");
			SpellDef fireball = SpellDef.GetByDefname("s_fireball");
			int magicArrowCount = 0;
			int fireballCount = 0;
			const int samples = 400;
			for (int i = 0; i < samples; i++) {
				SpellDef picked = NPCAiTimer.PickSpell(def);
				if (picked == magicArrow) {
					magicArrowCount++;
				} else if (picked == fireball) {
					fireballCount++;
				}
			}
			Console.WriteLine("NPC_SPELL_PICK_TEST_RESULT: magicArrow=" + magicArrowCount + " fireball=" + fireballCount + " total=" + samples);
		}

		[SteamFunction]
		public static void TestSampleSingleCombatSpellPick(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_single_combat_spell");
			SpellDef magicArrow = SpellDef.GetByDefname("s_magic_arrow");
			SpellDef picked = NPCAiTimer.PickSpell(def);
			Console.WriteLine("NPC_SINGLE_SPELL_PICK_TEST_RESULT: matches=" + (picked == magicArrow));
		}
	}
}
