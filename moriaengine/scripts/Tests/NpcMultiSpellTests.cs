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
	/// Server-side setup helper for #564/#734's protocol tests (Tests/Protocol/
	/// NpcMultiSpellTests.cs). #564's original version of this file proved NPCAiTimer.PickSpell
	/// picked uniformly at random among NPCDef.Spells (addspell=) with no concept of readiness -
	/// #734 replaced PickSpell with NPCAiTimer.ChooseSpellToCast, a utility-scored choice, so
	/// "uniform random regardless of state" is no longer the real contract to test. These now
	/// prove the new one instead: ChooseSpellToCast still returns a valid candidate while nothing
	/// has been cast yet, but once ANY spell is actually cast, EVERY spell this NPC knows (not
	/// just the one just cast) is blocked until the shared cast cooldown clears - real-user-
	/// reported follow-up: a caster can only ever have one spell "in flight" at a time, same as a
	/// real spellcaster mid-channel, regardless of which spell started that channel.
	/// </summary>
	public static class NpcMultiSpellTests {
		[SteamFunction]
		public static void TestCastingAnySpellBlocksEveryOtherSpellToo(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef casterDef = (NPCDef) ThingDef.GetByDefname("npc_test_multi_spell");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC caster = (NPC) casterDef.Create(self.X + 1, self.Y, self.Z, self.M);
			caster.Mana = 1000;
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;

			NPCAiTimer timer = NPCAiTimer.GetFor(caster);
			timer.EngageTarget(dummy);

			//compare by reference against the resolved SpellDef, not by Defname/PrettyDefname
			//string - these are numeric-header defs (e.g. "[DamageSpellDef 5] DEFNAME=
			//s_magic_arrow"), and AbstractScript.PrettyDefname reports the object's own PRIMARY
			//name, not whichever DEFNAME= alias was used to look it up.
			SpellDef magicArrow = SpellDef.GetByDefname("s_magic_arrow");
			SpellDef fireball = SpellDef.GetByDefname("s_fireball");

			SpellDef beforeCast = timer.ChooseSpellToCast(caster, dummy);
			bool beforeCastValid = (beforeCast == magicArrow) || (beforeCast == fireball);

			//a real cast - exercises Fight's own choice (with its small random tie-break between
			//the two, since both are ready at this point) and starts the SHARED cast cooldown,
			//which must block BOTH spells now, not just whichever one actually got cast.
			timer.Fight(caster, dummy);

			SpellDef afterCast1 = timer.ChooseSpellToCast(caster, dummy);
			SpellDef afterCast2 = timer.ChooseSpellToCast(caster, dummy);

			Console.WriteLine("MULTI_SPELL_EXCLUSIVITY_TEST: beforeCastValid=" + beforeCastValid +
				" castAttempts=" + timer.CastAttempts +
				" afterCastIsNull=" + (afterCast1 == null) + " stableAcrossCalls=" + (afterCast1 == afterCast2));
		}

		[SteamFunction]
		public static void TestSingleSpellGoesOnCooldown(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef casterDef = (NPCDef) ThingDef.GetByDefname("npc_test_single_combat_spell");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC caster = (NPC) casterDef.Create(self.X + 1, self.Y, self.Z, self.M);
			caster.Mana = 1000;
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;

			NPCAiTimer timer = NPCAiTimer.GetFor(caster);
			timer.EngageTarget(dummy);

			SpellDef readyBefore = timer.ChooseSpellToCast(caster, dummy);
			timer.Fight(caster, dummy); //its only spell gets cast and goes on cooldown
			SpellDef readyAfter = timer.ChooseSpellToCast(caster, dummy);

			Console.WriteLine("SINGLE_SPELL_COOLDOWN_TEST: readyBeforeIsSpell=" + (readyBefore != null) +
				" castAttempts=" + timer.CastAttempts + " readyAfterIsNull=" + (readyAfter == null));
		}
	}
}
