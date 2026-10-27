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
	/// #734 - server-side setup helpers for Tests/Protocol/NpcSupportOnlySpellWeavingTests.cs.
	/// npc_test_support_only_caster (scripts/Tests/NpcSupportOnlySpellWeavingTest.def) mirrors the
	/// real guard that exposed the bug (c_vguard_mordor_hala_jezdec): its only spells are a heal
	/// and a self-buff, no offensive spell at all. The old isCaster flag (NPCAiTimer.Fight) went
	/// true for ANY NPC with ANY spell, so this NPC held at spell range (10 tiles) forever and
	/// never had an offensive reason to close in - it just stood on its spawn spot. Fixed by
	/// gating isCaster on HasOffensiveSpell (SpellFlag.IsHarmful) instead of "has any spell at
	/// all" - this NPC now falls into the ordinary melee branch and weaves its support spells in
	/// opportunistically (ChooseSpellToCast's utility scoring) instead of blocking approach.
	/// </summary>
	public static class NpcSupportOnlySpellWeavingTests {
		/// <summary>Calls NPCAiTimer.Fight directly, twice, same "deterministic, no real-time
		/// ticking" precedent as NpcRangeOverrideTests.TestReportMageSpellCastRangeOverride. At
		/// full HP with its buff not yet active, the first tick's ChooseSpellToCast should prefer
		/// casting the buff (score 0.5) over moving - proving the "opportunistic self-buff before
		/// engaging" half of #734's ask. The second tick, with the buff now active (so its own
		/// score drops to 0) and no offensive spell to hold range for, should approach instead -
		/// proving the actual bug fix: this NPC no longer stands frozen at spell range forever.
		/// Caller must have already teleported to open ground (TestTeleportToOpenGround) so the
		/// 6-tile approach has real room to happen.</summary>
		[SteamFunction]
		public static void TestSupportOnlyNpcBuffsOnceThenCloses(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef casterDef = (NPCDef) ThingDef.GetByDefname("npc_test_support_only_caster");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC caster = (NPC) casterDef.Create(self.X, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 6, self.Y, self.Z, self.M);

			NPCAiTimer timer = NPCAiTimer.GetFor(caster);
			timer.EngageTarget(dummy);

			int startX = caster.X;
			int startY = caster.Y;

			timer.Fight(caster, dummy); //expected: casts the buff, doesn't move
			bool movedOnFirstTick = (caster.X != startX) || (caster.Y != startY);
			int castAttemptsAfterFirst = timer.CastAttempts;

			timer.Fight(caster, dummy); //expected: buff already active, nothing left to cast - approaches instead
			bool movedOnSecondTick = (caster.X != startX) || (caster.Y != startY);

			Console.WriteLine("SUPPORT_ONLY_BUFF_THEN_CLOSE_TEST: movedOnFirstTick=" + movedOnFirstTick +
				" castAttemptsAfterFirst=" + castAttemptsAfterFirst + " movedOnSecondTick=" + movedOnSecondTick);
		}

		/// <summary>Same NPC, this time already hurt (30% HP) and adjacent to its target from the
		/// start (so "did it move" proves nothing either way) - proves the heal half of the same
		/// scoring: a self-heal (score up to 1.0 near death) outscores the self-buff (flat 0.5),
		/// so a genuinely hurt support-only NPC heals itself rather than buffing or just standing
		/// there.</summary>
		[SteamFunction]
		public static void TestSupportOnlyNpcHealsInsteadOfBuffingWhenHurt(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef casterDef = (NPCDef) ThingDef.GetByDefname("npc_test_support_only_caster");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC caster = (NPC) casterDef.Create(self.X + 1, self.Y, self.Z, self.M);
			caster.Hits = 300; //30% of MaxHits=1000 - clearly outweighs the buff's flat 0.5 score
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			NPCAiTimer timer = NPCAiTimer.GetFor(caster);
			timer.EngageTarget(dummy);

			int startingHits = caster.Hits;
			timer.Fight(caster, dummy);

			Console.WriteLine("SUPPORT_ONLY_HEALS_WHEN_HURT_TEST: castAttempts=" + timer.CastAttempts +
				" healed=" + (caster.Hits > startingHits));
		}
	}
}
