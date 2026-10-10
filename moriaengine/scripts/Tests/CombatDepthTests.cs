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
	/// V-1 (verification pass, #69): combat-depth scenarios beyond plain melee (already covered
	/// by CombatScenarioTests.cs's titan test). Each function spawns NPCs adjacent to `self` (the
	/// connecting test character) rather than a few tiles out - PlayerCreation.startingPosition's
	/// immediate surroundings turn out to be a small elevated platform (see CombatScenarioTests.cs's
	/// own comment on this), so movement a few tiles out is unreliable there regardless of any AI
	/// logic being tested. Every scenario here is designed so the behavior under test doesn't
	/// depend on that movement actually succeeding.
	/// </summary>
	public static class CombatDepthTests {
		private const double CheckDelaySeconds = 12;

		/// <summary>
		/// #69 real finding: NPCDef.AggroMode (built in Phase 16, enhancements #18) was never
		/// actually settable from .def content - LoadScriptLine had no "aggromode" case, and
		/// nothing anywhere ever did `.AggroMode = ...` in C# either, so despite the Phase 16
		/// plan's own claim ("Hostile - monsters attack any player in range"), every single
		/// monster in the game defaulted to Calm and never attacked autonomously. This went
		/// unnoticed because all prior combat testing (including CombatScenarioTests.cs's titan
		/// test) always used an explicit EngageTarget/"kill" order, which bypasses autonomous
		/// aggro entirely. Fixed: NPC.cs now has a "aggromode" LoadScriptLine case, and c_titan/
		/// c_liche/c_dragon_lord/c_skeleton_archer_nb (the monsters this verification pass
		/// exercises) are now seeded with AggroMode = Hostile. The other ~450 CATEGORY=Monsters
		/// blocks across the content base are NOT touched here - that's a much bigger, separate
		/// gameplay-balance decision for the user, not something to do unilaterally while writing
		/// tests.
		/// </summary>
		[SteamFunction]
		public static void TestHostileAggro(Character self) {
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("HOSTILE_AGGRO_TEST: STARTED (titan #" + titan.Uid + ")");
		}

		/// <summary>Control case for TestHostileAggro - a Calm-categorized animal (c_sheep_woolly,
		/// CATEGORY=Animals, untouched by the AggroMode fix above) must never attack on its own.</summary>
		[SteamFunction]
		public static void TestCalmNoAggro(Character self) {
			NPCDef sheepDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			sheepDef.Create(self.X + 1, self.Y, self.Z, self.M);
			int startingHits = self.Hits;
			self.AddTimer(NoDamageCheckTimer.TimerKey, new NoDamageCheckTimer(self, startingHits, "CALM_AGGRO_TEST"))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("CALM_AGGRO_TEST: STARTED");
		}

		/// <summary>NPC spellcasting (#57) actually lands damage on an explicitly engaged target -
		/// the dummy target is a Calm sheep (not another Monster) specifically so it can't also
		/// start fighting back or auto-aggro, keeping this test's damage source unambiguous.</summary>
		[SteamFunction]
		public static void TestSpellcasterDamage(Character self) {
			NPCDef licheDef = ThingDef.GetByDefname("c_liche") as NPCDef;
			NPCDef dummyDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC liche = (NPC) licheDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			int startingHits = dummy.Hits;
			NPCAiTimer.GetFor(liche).EngageTarget(dummy);
			dummy.AddTimer(DamageCheckTimer.TimerKey, new DamageCheckTimer(dummy, startingHits, "SPELLCASTER_TEST"))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("SPELLCASTER_TEST: STARTED");
		}

		/// <summary>
		/// Archer stand-off (#56): with a real WeaponRange (10, from #56's content fix) instead of
		/// the melee default (1), NPCAiTimer.Fight's effectiveRange check (this verification
		/// pass's own V-0-adjacent fix, see NPCAiTimer.Fight's comment) should mean the archer
		/// never walks any closer when already within range - a real UO archer keeps its
		/// distance rather than rushing to melee. Checked by comparing the archer's position
		/// before and after, not just whether damage landed.
		/// </summary>
		[SteamFunction]
		public static void TestArcherStandsOff(Character self) {
			NPCDef archerDef = ThingDef.GetByDefname("c_skeleton_archer_nb") as NPCDef;
			NPCDef dummyDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC archer = (NPC) archerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 3, self.Y, self.Z, self.M); // 2 tiles from the archer - within its 10-tile range

			int startingHits = dummy.Hits;
			NPCAiTimer.GetFor(archer).EngageTarget(dummy);
			archer.AddTimer(ArcherStandoffCheckTimer.TimerKey,
				new ArcherStandoffCheckTimer(archer, dummy, archer.X, archer.Y, startingHits))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("ARCHER_STANDOFF_TEST: STARTED");
		}

		/// <summary>
		/// Dragon breath (#58's open question): does the AOE spell bypass actually fan out to a
		/// second target standing right next to the explicitly engaged one, or does it only ever
		/// land on the one target passed to SpellEffectArgs? Answers it either way rather than
		/// assuming - both outcomes are a "PASS" in the sense that the question is now answered;
		/// the marker text itself carries which answer it was.
		/// </summary>
		[SteamFunction]
		public static void TestDragonBreathAoe(Character self) {
			NPCDef dragonDef = ThingDef.GetByDefname("c_dragon_lord") as NPCDef;
			NPCDef dummyDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC dragon = (NPC) dragonDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy1 = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			NPC dummy2 = (NPC) dummyDef.Create(self.X + 2, self.Y + 1, self.Z, self.M); // adjacent to dummy1, never itself engaged

			int startingHits1 = dummy1.Hits;
			int startingHits2 = dummy2.Hits;
			NPCAiTimer.GetFor(dragon).EngageTarget(dummy1);
			self.AddTimer(DragonAoeCheckTimer.TimerKey,
				new DragonAoeCheckTimer(dummy1, dummy2, startingHits1, startingHits2))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("DRAGON_AOE_TEST: STARTED");
		}

		/// <summary>
		/// Low-health flee (Phase 16's FleeHealthFraction threshold): directly sets a titan's
		/// Hits below the 20% flee threshold, then gives it a target via EngageTarget (any valid
		/// target - flee doesn't require the target to actually be attacking, just to exist) and
		/// confirms Think() takes the Fleeing branch rather than Fighting. NPCAiTimer.Flee had no
		/// debug logging at all before this - added one line (matching the rest of the file's
		/// style) since there was otherwise no way to observe the state transition without relying
		/// on movement actually succeeding near this cramped spawn point.
		/// </summary>
		[SteamFunction]
		public static void TestLowHealthFlee(Character self) {
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			titan.Hits = (short) Math.Max(1, titan.MaxHits / 10); // well under the 20% flee threshold

			NPCAiTimer.GetFor(titan).EngageTarget(self);
			Console.WriteLine("LOW_HEALTH_FLEE_TEST: STARTED (titan #" + titan.Uid + ", hits=" + titan.Hits + "/" + titan.MaxHits + ")");
		}

		/// <summary>
		/// GuardsRealm (Phase 16): no real "town guard" content exists anywhere in this codebase
		/// (confirmed - no c_guard defname), so this directly configures an ordinary monster's
		/// def with AggroMode.GuardsRealm + a Realm for this one test, rather than needing real
		/// guard content to exist first. Mutates c_orc's shared NPCDef - deliberately NOT a
		/// defname used by any other scenario in this verification pass, so this doesn't leak
		/// into other tests sharing the same long-lived server process.
		/// </summary>
		[SteamFunction]
		public static void TestGuardsRealmAggro(Character self) {
			NPCDef orcDef = ThingDef.GetByDefname("c_orc") as NPCDef;
			orcDef.AggroMode = AggroMode.GuardsRealm;

			NPC guard = (NPC) orcDef.Create(self.X + 1, self.Y, self.Z, self.M);
			guard.Realm = Realm.Gondor;

			self.Realm = Realm.MordorOutcast; // opposite side - the guard should pick self as a valid target
			Console.WriteLine("GUARDSREALM_TEST: STARTED (guard #" + guard.Uid + ")");
		}
	}

	/// <summary>Generic "did this victim take damage (or die)" check, reusable across scenarios -
	/// mirrors TitanCombatCheckTimer's pattern from CombatScenarioTests.cs but parameterized by a
	/// marker prefix instead of being hardcoded to one scenario.</summary>
	[SaveableClass]
	public class DamageCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_damageCheckTimer_");

		private NPC victim;
		private int startingHits;
		private string markerPrefix;

		[LoadingInitializer]
		public DamageCheckTimer() {
		}

		public DamageCheckTimer(NPC victim, int startingHits, string markerPrefix) {
			this.victim = victim;
			this.startingHits = startingHits;
			this.markerPrefix = markerPrefix;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (this.victim.IsDeleted) {
				Console.WriteLine(this.markerPrefix + ": PASS (victim died - started at " + this.startingHits + " hits)");
			} else if (this.victim.Hits < this.startingHits) {
				Console.WriteLine(this.markerPrefix + ": PASS (hits " + this.startingHits + " -> " + this.victim.Hits + ")");
			} else {
				Console.WriteLine(this.markerPrefix + ": FAIL (no damage dealt - still at " + this.victim.Hits + " hits)");
			}
		}
	}

	/// <summary>Inverse of DamageCheckTimer - PASSes when the victim did NOT take damage (the Calm/no-aggro control case).</summary>
	[SaveableClass]
	public class NoDamageCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_noDamageCheckTimer_");

		private Character victim;
		private int startingHits;
		private string markerPrefix;

		[LoadingInitializer]
		public NoDamageCheckTimer() {
		}

		public NoDamageCheckTimer(Character victim, int startingHits, string markerPrefix) {
			this.victim = victim;
			this.startingHits = startingHits;
			this.markerPrefix = markerPrefix;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.victim.IsDeleted && this.victim.Hits == this.startingHits) {
				Console.WriteLine(this.markerPrefix + ": PASS (no damage taken, still at " + this.startingHits + " hits)");
			} else {
				Console.WriteLine(this.markerPrefix + ": FAIL (hits changed - started at " + this.startingHits + ", now " +
					(this.victim.IsDeleted ? -1 : this.victim.Hits) + ")");
			}
		}
	}

	[SaveableClass]
	public class ArcherStandoffCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_archerStandoffCheckTimer_");

		private NPC archer;
		private NPC dummy;
		private int startX, startY, startingHits;

		[LoadingInitializer]
		public ArcherStandoffCheckTimer() {
		}

		public ArcherStandoffCheckTimer(NPC archer, NPC dummy, int startX, int startY, int startingHits) {
			this.archer = archer;
			this.dummy = dummy;
			this.startX = startX;
			this.startY = startY;
			this.startingHits = startingHits;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool stayedPut = !this.archer.IsDeleted && (this.archer.X == this.startX) && (this.archer.Y == this.startY);
			bool dealtDamage = this.dummy.IsDeleted || (this.dummy.Hits < this.startingHits);

			if (stayedPut && dealtDamage) {
				Console.WriteLine("ARCHER_STANDOFF_TEST: PASS (archer stayed at range and landed damage)");
			} else if (!stayedPut) {
				Console.WriteLine("ARCHER_STANDOFF_TEST: FAIL (archer moved from (" + this.startX + "," + this.startY + ") to (" +
					(this.archer.IsDeleted ? -1 : this.archer.X) + "," + (this.archer.IsDeleted ? -1 : this.archer.Y) + "))");
			} else {
				Console.WriteLine("ARCHER_STANDOFF_TEST: FAIL (stayed at range but dealt no damage)");
			}
		}
	}

	[SaveableClass]
	public class DragonAoeCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_dragonAoeCheckTimer_");

		private NPC dummy1, dummy2;
		private int startingHits1, startingHits2;

		[LoadingInitializer]
		public DragonAoeCheckTimer() {
		}

		public DragonAoeCheckTimer(NPC dummy1, NPC dummy2, int startingHits1, int startingHits2) {
			this.dummy1 = dummy1;
			this.dummy2 = dummy2;
			this.startingHits1 = startingHits1;
			this.startingHits2 = startingHits2;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool hit1 = this.dummy1.IsDeleted || (this.dummy1.Hits < this.startingHits1);
			bool hit2 = this.dummy2.IsDeleted || (this.dummy2.Hits < this.startingHits2);

			if (!hit1) {
				Console.WriteLine("DRAGON_AOE_TEST: NO_DAMAGE (engaged target itself was never hit - test setup issue, not an AOE answer)");
			} else if (hit2) {
				Console.WriteLine("DRAGON_AOE_TEST: AOE_CONFIRMED (adjacent non-engaged target also took damage)");
			} else {
				Console.WriteLine("DRAGON_AOE_TEST: SINGLE_TARGET_ONLY (adjacent non-engaged target was unaffected)");
			}
		}
	}
}
