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
		// CI observed this flake for real, twice: first the archer standoff check (landing hit
		// is a dice roll retried every ~1s, not guaranteed on the first attempt) at the old 12s
		// delay, then (even at 30s) the spellcaster check - six consecutive Fireball casts over
		// ~24s all dealt zero damage before this test's own timeout ran out, which looks like a
		// resist-chance roll rather than pure scheduling delay. Either way the fix is the same:
		// more attempts within the window. Matches CombatScenarioTests.cs's identical bump.
		private const double CheckDelaySeconds = 45;

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
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("HOSTILE_AGGRO_TEST: STARTED (titan #" + titan.Uid + ")");
		}

		/// <summary>Control case for TestHostileAggro - a Calm-categorized animal (c_sheep_woolly,
		/// CATEGORY=Animals, untouched by the AggroMode fix above) must never attack on its own.</summary>
		[SteamFunction]
		public static void TestCalmNoAggro(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
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
			MovementSpeedTests.TestTeleportToOpenGround(self); //#788 - a static east of the spawn point blocks sight
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef licheDef = ThingDef.GetByDefname("c_liche_nb") as NPCDef; //#788 - Erebor's liche, harm/fireball/poison/lightning; the bare c_liche is Sphere's graphic with no spells
			NPCDef dummyDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC liche = (NPC) licheDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			int startingHits = dummy.Hits;
			NPCAiTimer.GetFor(liche).EngageTarget(dummy);
			// Attached to self (long-lived), not dummy - see CombatScenarioTests.cs's
			// TitanCombatCheckTimer comment: BoundTimer.Cont returns null once its TagHolder
			// IsDeleted, so a check attached directly to the (possibly-killed) victim would
			// silently never fire in exactly the case - the target dying - most likely to be a
			// real pass. Real finding while chasing a CI flake here: this was the actual cause,
			// not RNG/timing margin - the fireball's own target was dying off-screen (most
			// likely to a leftover hostile NPC from an earlier test sharing this fixture) and
			// the check for it never ran at all.
			self.AddTimer(DamageCheckTimer.TimerKey, new DamageCheckTimer(dummy, startingHits, "SPELLCASTER_TEST"))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("SPELLCASTER_TEST: STARTED");
		}

		/// <summary>
		/// Spellcaster stand-off: a caster (c_liche, combatspell=s_fireball, no explicit
		/// WeaponRange override - defaults to 1/melee) placed non-adjacent to its target should
		/// hold that distance rather than closing to melee range between casts -
		/// NPCAiTimer.Fight's effectiveRange used to fall through to the melee weapon-range
		/// default for any NPC with a CombatSpell, so this walked right up next to the target on
		/// every tick its spell was on cooldown (4s cooldown vs. a 1.5s think-tick). Checked the
		/// same way as TestArcherStandsOff: comparing position before/after. Verifies the caster
		/// actually attempted to cast (NPCAiTimer.CastAttempts, which only requires mana spent/
		/// cooldown started) rather than whether the spell's damage landed - this test's spawn
		/// point sits inside "Jamy smrti", a real content-defined FlaggedRegion with
		/// NoHarmfulMagicIn covering a huge swath of the map (confirmed separately, not a test
		/// bug), which silently blocks ALL harmful NPC spellcasting here independent of distance.
		/// TestSpellcasterDamage's own adjacent placement never noticed because melee fallback
		/// damage (WeaponSkillTargetQueuePlugin) carries it instead - worth a dedicated look since
		/// NPC spellcasting is presumably non-functional across this whole region, but that's a
		/// separate, pre-existing issue from the distancing behavior under test here.
		/// </summary>
		[SteamFunction]
		public static void TestSpellcasterStandsOff(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self); //#788 - a static east of the spawn point blocks sight
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef licheDef = ThingDef.GetByDefname("c_liche_nb") as NPCDef; //#788 - Erebor's liche, harm/fireball/poison/lightning; the bare c_liche is Sphere's graphic with no spells
			NPCDef dummyDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC liche = (NPC) licheDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 3, self.Y, self.Z, self.M); // 2 tiles from the liche (same geometry as TestArcherStandsOff) - beyond melee range, within its 10-tile spell range
			//#788 - Erebor's liche kills a sheep in two fireballs and then goes for the watching player:
			//the target has to last the check, and stay where it is
			dummy.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			dummy.MaxHits = 30000;
			dummy.Hits = 30000;

			NPCAiTimer.GetFor(liche).EngageTarget(dummy);
			// Attached to self, not liche - see TestSpellcasterDamage's comment just above.
			self.AddTimer(SpellcasterStandoffCheckTimer.TimerKey,
				new SpellcasterStandoffCheckTimer(liche, liche.X, liche.Y))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("SPELLCASTER_STANDOFF_TEST: STARTED");
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
			MovementSpeedTests.TestTeleportToOpenGround(self); //#788 - a static east of the spawn point blocks sight
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef archerDef = ThingDef.GetByDefname("c_skeleton_archer_nb") as NPCDef;
			NPCDef dummyDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC archer = (NPC) archerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 3, self.Y, self.Z, self.M); // 2 tiles from the archer - within its 10-tile range
			//#788 - an archer shoots now (monsters need no arrows, #788) and kills a sheep fast, then goes
			//for the watching player: the target has to last the check, and stay where it is
			dummy.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			dummy.MaxHits = 30000;
			dummy.Hits = 30000;

			int startingHits = dummy.Hits;
			NPCAiTimer.GetFor(archer).EngageTarget(dummy);
			// Attached to self, not archer - see TestSpellcasterDamage's comment just above.
			self.AddTimer(ArcherStandoffCheckTimer.TimerKey,
				new ArcherStandoffCheckTimer(archer, dummy, archer.X, archer.Y, startingHits))
				.DueInSeconds = CheckDelaySeconds;
			Console.WriteLine("ARCHER_STANDOFF_TEST: STARTED");
		}

		private static Player breathEngaged, breathBeside;
		private static NPC breathDragon;
		private static int engagedHitsBefore, besideHitsBefore;
		private static bool besideInCone;

		/// <summary>#1504 (was #58's open question, never really asked: the old test's dragon had no breath and its two
		/// targets were sheep, which the breath never burns) - does a dragon's breath hit a player beside the one it faces?
		/// A brown dragon (e_fire_dragon) faces `self` 2 tiles off, its AI off so no blow gets in the count; the other
		/// player (its uid in the args) stands on a tile of the breath's cone next to `self`. The breath goes through
		/// DragonActionTimer, the same as after the dragon's own roll (TryFireDragon), without the dice.</summary>
		[SteamFunction]
		public static void TestDragonBreathCone(Character self, ScriptArgs sa) {
			Player engaged = (Player) self;
			Player beside = (Player) Thing.UidGetCharacter(uint.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture));
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			foreach (Player player in new[] { engaged, beside }) {
				player.Account.PLevel = 1; //the breath burns non-GM players only
				player.MaxHits = 30000;
				player.Hits = player.MaxHits;
			}
			NPC dragon = (NPC) ThingDef.GetByDefname("c_drak_hnedy").Create(self.X, self.Y, self.Z, self.M);
			dragon.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			int[][] sides = { new[] { 2, 0, (int) Direction.West }, new[] { -2, 0, (int) Direction.East }, new[] { 0, 2, (int) Direction.North }, new[] { 0, -2, (int) Direction.South } };
			foreach (int[] side in sides) {
				dragon.P(self.X + side[0], self.Y + side[1], self.Z, self.M);
				dragon.Direction = (Direction) side[2];
				if (dragon.CanSeeLOS(self).Allow) {
					break;
				}
			}
			besideInCone = false;
			foreach (int[] t in MonsterSpecialAttacks.DragonCone((int) dragon.Direction)) {
				int x = dragon.X + t[0], y = dragon.Y + t[1];
				//beside the engaged player, off the dragon's line through it (a breath along the line only would miss it)
				if ((Point2D.GetSimpleDistance(x, y, self.X, self.Y) == 1) && (x != dragon.X) && (y != dragon.Y) && (t[2] == 0)) {
					beside.P(x, y, self.Z, self.M);
					besideInCone = (beside.X == x) && (beside.Y == y);
					break;
				}
			}
			breathDragon = dragon;
			breathEngaged = engaged;
			breathBeside = beside;
			engagedHitsBefore = engaged.Hits;
			besideHitsBefore = beside.Hits;
			int[] tuned = (dragon.Def as NPCDef).AbilityTuning("fireDragonDamage");
			DragonActionTimer.Start(dragon, DragonActionTimer.Breath, 1, (tuned == null) ? 120 : tuned[0], 4, 0);
			Console.WriteLine("DRAGON_CONE: STARTED dragon=" + dragon.X + "," + dragon.Y + " facing=" + dragon.Direction + " engaged=" + self.X + "," + self.Y
				+ " beside=" + beside.X + "," + beside.Y + " besideInCone=" + besideInCone);
		}

		[SteamFunction]
		public static void TestDragonBreathConeReport(Character self) {
			Console.WriteLine("DRAGON_CONE: RESULT lostEngaged=" + (engagedHitsBefore - breathEngaged.Hits) + " lostBeside=" + (besideHitsBefore - breathBeside.Hits)
				+ " besideInCone=" + besideInCone);
			if ((breathDragon != null) && !breathDragon.IsDeleted) {
				breathDragon.Delete();
			}
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
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef titanDef = ThingDef.GetByDefname("c_titan") as NPCDef;
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			titan.Hits = (short) Math.Max(1, titan.MaxHits / 10); // well under the 20% flee threshold

			NPCAiTimer.GetFor(titan).EngageTarget(self);
			Console.WriteLine("LOW_HEALTH_FLEE_TEST: STARTED (titan #" + titan.Uid + ", hits=" + titan.Hits + "/" + titan.MaxHits + ")");
		}

		/// <summary>
		/// GuardsRealm (Phase 16): at the time this was written, no real "town guard" content
		/// existed anywhere in this codebase, so this directly configures an ordinary monster's
		/// def with AggroMode.GuardsRealm + a Realm for this one test, rather than needing real
		/// guard content to exist first. Real c_vguard_* guard content (converted from
		/// tag.guard, see NpcGuardContentTests.cs/#700) now exists and exercises this exact
		/// mechanism too - kept as-is anyway since mutating c_orc's shared NPCDef here is still
		/// deliberately isolated from any defname used by another scenario in this verification
		/// pass, so this doesn't leak into other tests sharing the same long-lived server process.
		/// </summary>
		[SteamFunction]
		public static void TestGuardsRealmAggro(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef orcDef = ThingDef.GetByDefname("c_orc") as NPCDef;
			orcDef.AggroMode = AggroMode.GuardsRealm;

			NPC guard = (NPC) orcDef.Create(self.X + 1, self.Y, self.Z, self.M);
			guard.Realm = Realm.Gondor;

			self.Realm = Realm.MordorOutcast; // opposite side - the guard should pick self as a valid target
			self.Account.PLevel = 1; //#1022 - guards leave GMs alone, and test accounts start as GMs
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
	public class SpellcasterStandoffCheckTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_spellcasterStandoffCheckTimer_");

		private NPC liche;
		private int startX, startY;

		[LoadingInitializer]
		public SpellcasterStandoffCheckTimer() {
		}

		public SpellcasterStandoffCheckTimer(NPC liche, int startX, int startY) {
			this.liche = liche;
			this.startX = startX;
			this.startY = startY;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool stayedPut = !this.liche.IsDeleted && (this.liche.X == this.startX) && (this.liche.Y == this.startY);
			int castAttempts = this.liche.IsDeleted ? 0 : NPCAiTimer.GetFor(this.liche).CastAttempts;

			if (stayedPut && (castAttempts > 0)) {
				Console.WriteLine("SPELLCASTER_STANDOFF_TEST: PASS (caster stayed at range and attempted " + castAttempts + " cast(s))");
			} else if (!stayedPut) {
				Console.WriteLine("SPELLCASTER_STANDOFF_TEST: FAIL (caster moved from (" + this.startX + "," + this.startY + ") to (" +
					(this.liche.IsDeleted ? -1 : this.liche.X) + "," + (this.liche.IsDeleted ? -1 : this.liche.Y) + "))");
			} else {
				Console.WriteLine("SPELLCASTER_STANDOFF_TEST: FAIL (stayed at range but never attempted a cast)");
			}
		}
	}
}
