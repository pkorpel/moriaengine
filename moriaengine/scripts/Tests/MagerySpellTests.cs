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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Test infrastructure for the Mage/spellcasting combat-system initiative (#483), starting
	/// with Phase 0 (#484) - zero Magery/spell test coverage existed anywhere in this codebase
	/// before this. First consumer: proving SpellDef.Difficulty's fallback fix (most spells never
	/// set an explicit difficulty, which used to mean 0 - essentially guaranteed success once
	/// mana was sufficient) actually makes the fizzle chance vary with skill.
	/// </summary>
	public static class MagerySpellTests {

		/// <summary>Grants a real spellbook (i_spellbook_3) containing every spell this engine
		/// has real content for, plus generous mana/stats so mana-gating never blocks a test
		/// cast. Ground placement (not backpack) - same "freshly granted container items aren't
		/// reachable without first opening them" reasoning used throughout this session's other
		/// test helpers; TryCastSpellFromBook only needs the book to be found via
		/// FindLayer/Backpack iteration server-side, no client-side "open" step required, but
		/// ground placement is simplest and matches precedent regardless.</summary>
		[SteamFunction]
		public static void TestGrantFullSpellbook(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self); //#788 - a static east of the spawn point refused every ground cast there
			//#788 - Erebor's darkness rule refuses a cast on a light roll, and a long run reaches night
			PotionEffectPlugin.Apply(self, PotionEffectPlugin.NightSightSlot, PotionStat.NightSight, 0, 3600, "Nightsight", 0x8C5);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Str = 100;
			self.Dex = 100;
			self.Int = 100;
			self.MaxMana = 1000;
			self.Mana = 1000;
			// On_Start's Requirements check (Magery.cs) is a hard gate, not just a fizzle-chance
			// input - a caster below a spell's own Requirements=X Magery threshold never even
			// reaches the Stroke, regardless of mana. Max skill avoids that blocking every cast.
			self.SetRealSkillValue((int) SkillName.Magery, 1000);

			ThingDef bookDef = ThingDef.GetByDefname("i_spellbook_3");
			SpellBook book = (SpellBook) bookDef.Create(self.Backpack);
			foreach (SpellDef spell in SpellDef.AllSpellDefs) {
				book.AddSpell(spell);
			}
			Console.WriteLine("MAGERY_TEST: spellbook granted, uid=" + book.Uid);
		}

		/// <summary>#484 - directly reports SpellDef.Difficulty for a given spell defname.
		/// Before the fix this would print 0 for every spell except the DamageSpellDef ones
		/// (damageSpells.def is the only file that sets difficulty= explicitly) - after the fix,
		/// any spell with a real Requirements=X Magery line reports that threshold instead.</summary>
		[SteamFunction]
		public static void TestReportSpellDifficulty(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			SpellDef spell = SpellDef.GetByDefname(defname);
			Console.WriteLine("MAGERY_DIFFICULTY_TEST: defname=" + defname + " difficulty=" + spell.Difficulty);
		}

		/// <summary>#487 - reports SpellDef.CastTime for a given spell defname (e.g. confirming
		/// s_Cunning's fixed 1.5s, not the old 8s typo).</summary>
		[SteamFunction]
		public static void TestReportSpellCastTime(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			SpellDef spell = SpellDef.GetByDefname(defname);
			Console.WriteLine("MAGERY_CASTTIME_TEST: defname=" + defname + " castTime=" + spell.CastTime.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>#487 - reports a DamageSpellDef's DamageType for a given spell defname (e.g.
		/// confirming s_harm's fixed MagicCold, not the old unbacked MagicAcid).</summary>
		[SteamFunction]
		public static void TestReportSpellDamageType(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			DamageSpellDef spell = (DamageSpellDef) SpellDef.GetByDefname(defname);
			Console.WriteLine("MAGERY_DAMAGETYPE_TEST: defname=" + defname + " damageType=" + spell.DamageType);
		}

		/// <summary>#489 - reports a DurableSpellDef's raw Duration range (min,max in seconds)
		/// for a given spell defname (e.g. s_gate_travel's 3,4, Erebor's live value, #1001).</summary>
		[SteamFunction]
		public static void TestReportSpellDuration(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			DurableSpellDef spell = (DurableSpellDef) SpellDef.GetByDefname(defname);
			double[] duration = spell.Duration;
			Console.WriteLine("MAGERY_DURATION_TEST: defname=" + defname +
				" min=" + duration[0].ToString(CultureInfo.InvariantCulture) +
				" max=" + duration[1].ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>#489 - casts the test-only s_test_invisibility (flat 3s duration, see
		/// MageryInvisibilityTest.def) on self via scroll, same reasoning as s_test_summon -
		/// SpellBook.AddSpell refuses ids outside [FirstSpellId, FirstSpellId+64).</summary>
		[SteamFunction]
		public static void TestCastInvisibilityOnSelf(Character self) {
			SpellDef spell = SpellDef.GetByDefname("s_test_invisibility");
			ThingDef scrollDef = ThingDef.GetByDefname("i_scroll_test_invisibility");
			SpellScroll scroll = (SpellScroll) scrollDef.Create(self.Backpack);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, self, null, scroll, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_INVIS_TEST: STARTED");
		}

		/// <summary>#489 - reports the given character's Flag_Hidden/Flag_InvisByMagic - reused
		/// by both the Invisibility test (self's own uid) and the Reveal test (its two dummy
		/// targets).</summary>
		[SteamFunction]
		public static void TestReportVisibilityFlags(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			Console.WriteLine("MAGERY_VISIBILITY_TEST: hidden=" + target.Flag_Hidden + " invisByMagic=" + target.Flag_InvisByMagic);
		}

		/// <summary>#489 - spawns a hidden dummy (Hiding skill 0, so the Magery-vs-Hiding contest
		/// is as deterministic as the bell curve allows at max Magery) and a magically-invisible
		/// dummy near the ground target, then casts the real s_reveal at that point - proving
		/// both of Reveal's two paths (unconditional Flag_InvisByMagic clear, and the skill-
		/// contest-gated Flag_Hidden clear).</summary>
		[SteamFunction]
		public static void TestCastRevealNearSelf(Character self) {
			SpellDef spell = SpellDef.GetByDefname("s_reveal");
			SpellBook book = FindBook(self);

			int targetX = self.X + 2;
			int targetZ = self.GetMap().GetTileZ(targetX, self.Y);
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");

			NPC hiddenDummy = (NPC) dummyDef.Create(targetX, self.Y, targetZ, self.M);
			hiddenDummy.Flag_Hidden = true;
			hiddenDummy.SetRealSkillValue((int) SkillName.Hiding, 0);

			NPC invisDummy = (NPC) dummyDef.Create(targetX + 1, self.Y, targetZ, self.M);
			invisDummy.Flag_InvisByMagic = true;

			Point4D groundTarget = new Point4D(targetX, self.Y, targetZ, self.M);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, groundTarget, null, book, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_REVEAL_TEST: STARTED hiddenUid=" + hiddenDummy.Uid + " invisUid=" + invisDummy.Uid);
		}

		/// <summary>#484 - the real regression test for the fizzle fix. Temporarily sets self's
		/// raw Magery skill value, then samples SkillDef.CheckSuccess (the exact same roll
		/// MagerySkillDef.On_Stroke uses) `sampleCount` times against the given spell's real
		/// Difficulty, reporting how many succeeded. MUST run on a non-GM character -
		/// SkillDef.CheckSuccess has its own "GM is always successful" bypass that would mask
		/// the fix entirely.</summary>
		[SteamFunction]
		public static void TestSampleFizzleRate(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			string defname = parts[0];
			int skillValue = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			int sampleCount = Convert.ToInt32(parts[2], CultureInfo.InvariantCulture);

			SpellDef spell = SpellDef.GetByDefname(defname);
			self.SetRealSkillValue((int) SkillName.Magery, skillValue);

			SkillDef magery = SkillDef.GetBySkillName(SkillName.Magery);
			int difficulty = spell.GetDifficulty(false);
			int successes = 0;
			for (int i = 0; i < sampleCount; i++) {
				if (magery.CheckSuccess(self, difficulty)) {
					successes++;
				}
			}
			Console.WriteLine("MAGERY_FIZZLE_SAMPLE: defname=" + defname + " skill=" + skillValue +
				" difficulty=" + difficulty + " successes=" + successes + " total=" + sampleCount);
		}

		/// <summary>#484 - end-to-end sanity check that touching SpellDef.cs didn't break the
		/// real casting pipeline: casts a real damage spell from a real spellbook at a real
		/// target via the exact same SkillSequenceArgs shape TryCastSpellFromBook builds, except
		/// with Target1 preset (SpellDef.Trigger_Select skips the target-cursor step entirely
		/// when Target1 is already non-null) - avoids needing a live target-cursor packet
		/// round-trip for a test. Call TestGrantFullSpellbook first. Logs starting state; the
		/// protocol test sleeps past the spell's own cast time, then calls
		/// TestReportCastResult to compare.</summary>
		[SteamFunction]
		public static void TestCastSpellAtDummy(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			SpellDef spell = SpellDef.GetByDefname(defname);

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);

			SpellBook book = FindBook(self);

			// Mana is deducted synchronously inside On_Start (Magery.cs), BEFORE the delayed
			// Stroke is even scheduled - capture it here, before PhaseSelect runs the whole
			// Select->Start chain synchronously, not after.
			int startMana = self.Mana;
			int startHits = dummy.Hits;

			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, dummy, null, book, spell, null);
			magery.PhaseSelect();

			Console.WriteLine("MAGERY_CAST_STARTED: defname=" + defname + " dummyUid=" + dummy.Uid +
				" startHits=" + startHits + " startMana=" + startMana);
		}

		[SteamFunction]
		public static void TestReportCastResult(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			bool deleted = (target == null) || target.IsDeleted;
			Console.WriteLine("MAGERY_CAST_RESULT: casterMana=" + self.Mana +
				" targetDeleted=" + deleted + " targetHits=" + (deleted ? -1 : target.Hits));
		}

		/// <summary>#486 - casts a real beneficial buff (s_bless, a DurableCharEffectSpellDef)
		/// on self, via spellbook - sets EffectFlag.FromSpellBook, the exact flag
		/// EffectDurationPlugin.Dispellable checks for (potion effects are deliberately not
		/// dispellable - see that property's own doc comment, unaffected by this test).</summary>
		[SteamFunction]
		public static void TestCastBuffOnSelf(Character self) {
			SpellDef spell = SpellDef.GetByDefname("s_bless");
			SpellBook book = FindBook(self);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, self, null, book, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_DISPEL_TEST: BUFF_CAST_STARTED");
		}

		[SteamFunction]
		public static void TestReportDispellablePluginCount(Character self) {
			int count = 0;
			foreach (Plugin plugin in self.GetAllPlugins()) {
				EffectDurationPlugin durationPlugin = plugin as EffectDurationPlugin;
				if ((durationPlugin != null) && durationPlugin.Dispellable) {
					count++;
				}
			}
			Console.WriteLine("MAGERY_DISPEL_TEST: dispellablePluginCount=" + count);
		}

		/// <summary>#486 - casts s_dispel at the given target (by uid) - self for the buffed-
		/// character case, a summoned creature's uid for the summon-removal case.</summary>
		[SteamFunction]
		public static void TestCastDispelOnTarget(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			SpellDef spell = SpellDef.GetByDefname("s_dispel");
			SpellBook book = FindBook(self);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, target, null, book, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_DISPEL_TEST: DISPEL_CAST_STARTED");
		}

		/// <summary>#488 - defensive test-only cleanup for a Hostile summon (Blade Spirits).
		/// TestCastGroundTargetedSpellNearSelf's own `distance` parameter already keeps it
		/// outside NpcAggroSettings.instance.aggroRange (#762, default 14) so it never actually
		/// engages the test caster in the first place - this is belt-and-suspenders on top of
		/// that, not the primary defense.
		/// Deliberately a plain property set (AggroMode), nothing that touches the NPC's
		/// lifecycle: both outright Thing.Delete() and CauseDeath were tried against an
		/// ACTIVELY-ENGAGED NPC (back when distance wasn't yet used to avoid engagement at all)
		/// and both hung the whole server with no further output at all - a real NPCAiTimer-tick-
		/// thread-vs-command-thread concurrency hazard, flagged not fixed here. AggroMode.Calm
		/// alone never showed that hang even then - this just also stops it from threatening
		/// anyone in the rare case something else wanders into range later.</summary>
		[SteamFunction]
		public static void TestNeutralizeCharacterByUid(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			NPC target = Thing.UidGetCharacter(targetUid) as NPC;
			if (target != null) {
				target.AggroMode = AggroMode.Calm;
			}
			Console.WriteLine("MAGERY_NEUTRALIZE_TEST: neutralized=" + (target != null));
		}

		/// <summary>#489 - casts Paralyze Field at a ground point near self - WallSpellDef.
		/// On_EffectGround always places one wall tile exactly at the target point itself (the
		/// "offset 0" tile, before the +/-1/+/-2 spread). Deliberately casts at an EMPTY point
		/// first (no dummy there yet) rather than spawning the dummy before casting -
		/// SpellEffectItem.CheckPositionForItem's own checkCharacters=true occupancy check would
		/// otherwise refuse to place that one center tile exactly where a character already
		/// stands, silently leaving a "hole" in the field at the one spot the test cares about
		/// most (confirmed directly: a dummy pre-placed there before casting never got
		/// paralyzed, while the field's other 4 tiles - never occupied - were created fine and
		/// blocked the dummy's own idle-wander movement). Reports the target point so the caller
		/// can spawn a dummy there AFTER the field item already exists.</summary>
		[SteamFunction]
		public static void TestCastParalyzeFieldNearSelf(Character self) {
			SpellDef spell = SpellDef.GetByDefname("s_paralyzation_field");
			SpellBook book = FindBook(self);
			self.MaxMana = 5000; //#962 - Erebor's live 4044 mana
			self.Mana = 5000;

			// Caller teleports to confirmed open ground (TestTeleportToOpenGround, no static
			// items within 7 tiles) before calling this - distance=3 stays safely inside that
			// guarantee. The default spawn point's own terrain is too cramped/irregular for
			// SpellEffectItem.CheckPositionForItem to place a real item anywhere nearby
			// (confirmed directly: canFit=False at every candidate tile there, even at
			// distance=15 and even reusing Fire Field's own already-shipped model).
			int targetX = self.X + 3;
			int targetZ = self.GetMap().GetTileZ(targetX, self.Y);
			Point4D groundTarget = new Point4D(targetX, self.Y, targetZ, self.M);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, groundTarget, null, book, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_PARAFIELD_TEST: STARTED targetX=" + targetX + " targetY=" + self.Y +
				" targetZ=" + targetZ + " targetM=" + self.M);
		}

		/// <summary>#489 - spawns a dummy NPC at an exact point (by X/Y/Z/M) - used to place it
		/// on Paralyze Field's already-created center tile (see TestCastParalyzeFieldNearSelf's
		/// own doc comment for why spawn order matters here).</summary>
		[SteamFunction]
		public static void TestSpawnDummyAt(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			int x = Convert.ToInt32(parts[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			int z = Convert.ToInt32(parts[2], CultureInfo.InvariantCulture);
			byte m = Convert.ToByte(parts[3], CultureInfo.InvariantCulture);
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(x, y, z, m);
			Console.WriteLine("MAGERY_SPAWN_TEST: dummyUid=" + dummy.Uid);
		}

		/// <summary>#489 - reports whether the given character currently carries a real
		/// ParalyzeEffectPlugin (the same plugin Circle V's shipped Paralyze spell uses) - a
		/// direct check of the actual paralyze effect, not an indirect proxy like movement.</summary>
		[SteamFunction]
		public static void TestReportIsParalyzed(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			bool paralyzed = false;
			if (target != null) {
				foreach (Plugin plugin in target.GetAllPlugins()) {
					if (plugin is ParalyzeEffectPlugin) {
						paralyzed = true;
						break;
					}
				}
			}
			Console.WriteLine("MAGERY_PARALYZE_TEST: paralyzed=" + paralyzed);
		}

		/// <summary>#788 - Summon Creature's menu (#999, Erebor's sm_summon): picks the giant rat the way the menu
		/// does (SummonMenu.Chose starts the cast with the creature), and says where the cursor should land.</summary>
		[SteamFunction]
		public static void TestSummonCreatureChoose(Character self) {
			SummonMenu.Row rat = Array.Find(SummonMenu.rows, row => row.creature == "c_rat_giant_mage");
			SummonMenu.Chose((Player) self, FindBook(self), SpellDef.GetByDefname("s_summon_creature"), rat);
			int x = self.X + 2;
			Console.WriteLine("MAGERY_SUMMONCHOICE: at=" + x + "," + self.Y + "," + self.GetMap().GetTileZ(x, self.Y));
		}

		private static SpellBook FindBook(Character self) {
			SpellBook book = self.FindLayer(1) as SpellBook;
			if (book == null) {
				foreach (Item i in self.Backpack.EnumShallow()) {
					book = i as SpellBook;
					if (book != null) {
						break;
					}
				}
			}
			return book;
		}

		/// <summary>#485 - casts the test-only s_test_summon spell at a point adjacent to self.
		/// Via a scroll, not the spellbook TestGrantFullSpellbook already granted -
		/// SpellBook.AddSpell silently refuses spell id 9000 (outside the book's valid
		/// [FirstSpellId, FirstSpellId+64) slot range by design, see MagerySummonTest.def's own
		/// comment), so a dedicated test scroll is created here instead. Ground-targeted (not a
		/// character), so Target1 is preset to a real Point4D the same way TestCastSpellAtDummy
		/// presets a Character - skips the live target-cursor step.</summary>
		[SteamFunction]
		public static void TestCastSummonSpell(Character self) {
			SpellDef spell = SpellDef.GetByDefname("s_test_summon");

			ThingDef scrollDef = ThingDef.GetByDefname("i_scroll_test_summon");
			SpellScroll scroll = (SpellScroll) scrollDef.Create(self.Backpack);

			Point4D groundTarget = new Point4D(self.X + 1, self.Y, self.Z, self.M);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, groundTarget, null, scroll, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_SUMMON_TEST: STARTED startMana=" + self.Mana);
		}

		/// <summary>#485 - scans for a nearby summoned creature, the first within 20 tiles, whoever summoned it. Reports "none"
		/// rather than throwing if it's already despawned, so the same command can confirm both
		/// "it appeared" (shortly after casting) and "it's gone" (after the duration elapses).
		/// Range widened from 5 to 20 for #488's own Blade Spirits test, which deliberately
		/// summons outside NpcAggroSettings.instance.aggroRange (#762, default 14 - was
		/// NPCAiTimer.AggroRange=8 before that setting shipped) - see
		/// TestCastGroundTargetedSpellNearSelf's own doc comment for why, and for why the
		/// distance can't just be pushed arbitrarily far out (Globals.MaxUpdateRange=18 is a
		/// hard ceiling on ground-targeted casts, unrelated to aggro).</summary>
		[SteamFunction]
		public static void TestReportNearbySummon(Character self) {
			foreach (AbstractCharacter ac in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				Character candidate = ac as Character;
				if ((candidate != null) && SummonSpellDef.IsSummonedCreature(candidate)) {
					Console.WriteLine("MAGERY_SUMMON_TEST: FOUND uid=" + candidate.Uid +
						" controlMasterUid=" + (candidate.ControlMaster != null ? candidate.ControlMaster.Uid.ToString() : "null") +
						" warMode=" + candidate.Flag_WarMode);
					return;
				}
			}
			Console.WriteLine("MAGERY_SUMMON_TEST: NONE");
		}

		/// <summary>#490 - reports whether the nearby summoned creature's actual NPCDef matches
		/// the one looked up by `expectedDefname`, by object reference rather than by comparing
		/// `.Defname` strings - confirmed directly that AbstractScript.Defname reports the
		/// object's own PRIMARY name (the auto-generated "c_&lt;hex&gt;" derived from its `.def`
		/// header, e.g. "c_0x23d" for Energy Vortex's `[NPCDef 0x23d]` block), NOT whichever
		/// `DEFNAME=` alias was used to look it up - a later `DEFNAME=` property line registers
		/// an additional lookup alias in the global by-defname table without renaming the
		/// object's own primary identity string. Comparing by resolved reference sidesteps that
		/// aliasing entirely and is the actually-correct check anyway (what matters is "the right
		/// NPCDef got used", not which of its names strings happens to match).</summary>
		[SteamFunction]
		public static void TestReportNearbySummonMatchesDefname(Character self, ScriptArgs sa) {
			string expectedDefname = SplitArgs(sa)[0];
			NPCDef expectedDef = (NPCDef) ThingDef.GetByDefname(expectedDefname);
			foreach (AbstractCharacter ac in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				Character candidate = ac as Character;
				if ((candidate != null) && SummonSpellDef.IsSummonedCreature(candidate)) {
					Console.WriteLine("MAGERY_SUMMON_DEFNAME_TEST: matches=" + (candidate.Def == expectedDef));
					return;
				}
			}
			Console.WriteLine("MAGERY_SUMMON_DEFNAME_TEST: matches=False");
		}

		/// <summary>#490 - deletes every nearby non-player Character (VerificationTestUtils'
		/// established, already-proven-safe cleanup helper) so a test iterating several
		/// different controlled, non-hostile summons in sequence always sees exactly the summon
		/// it just cast, not a leftover from a previous cast in the same test. Safe specifically
		/// because these summons are idle/non-engaged (Flag_WarMode=false, no autonomous
		/// aggro) - deleting an ACTIVELY-ENGAGED hostile NPC is the one case confirmed to hang
		/// the server (see Phase 4's Blade Spirits test comment), which doesn't apply here.</summary>
		[SteamFunction]
		public static void TestCleanupNearbySummons(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Console.WriteLine("MAGERY_SUMMON_CLEANUP_TEST: DONE");
		}

		/// <summary>#488 - casts any REAL (spellbook-slotted, id 1-64) ground-targeted spell at a
		/// point `distance` tiles from self, via the spellbook TestGrantFullSpellbook already
		/// granted - unlike TestCastSummonSpell's test-only s_test_summon (id 9000, deliberately
		/// outside the spellbook's slot range), Blade Spirits/Summon Creature are real content
		/// with real ids, so no dedicated scroll workaround is needed here. `distance` matters
		/// specifically for a Hostile summon (Blade Spirits) - anything beyond
		/// NpcAggroSettings.instance.aggroRange (#762, default 14) from self never
		/// autonomously engages its own caster at all,
		/// which matters because deleting (or even CauseDeath-ing) an ACTIVELY-ENGAGED NPC was
		/// confirmed directly to hang the whole server (a real NPCAiTimer-tick-thread-vs-command-
		/// thread concurrency hazard, flagged not fixed here) - an idle, never-engaged NPC has no
		/// such risk, so this sidesteps the hazard rather than relying on cleanup timing.</summary>
		[SteamFunction]
		public static void TestCastGroundTargetedSpellNearSelf(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			string defname = parts[0];
			int distance = (parts.Length > 1) ? Convert.ToInt32(parts[1], CultureInfo.InvariantCulture) : 1;
			SpellDef spell = SpellDef.GetByDefname(defname);
			SpellBook book = FindBook(self);

			// Real terrain Z (Map.GetTileZ), not self.Z blindly reused - fine for distance=1
			// (terrain height rarely differs tile-to-tile) but confirmed directly to fail
			// CanInteractWithMessage's LOS check at distance=15 (self.Z no longer matches the
			// actual ground there, so the LOS ray treats the point as embedded/floating).
			int groundX = self.X + distance;
			int groundZ = self.GetMap().GetTileZ(groundX, self.Y);
			Point4D groundTarget = new Point4D(groundX, self.Y, groundZ, self.M);
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, groundTarget, null, book, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGERY_GROUNDCAST_TEST: STARTED defname=" + defname);
		}

		/// <summary>#488 - casts s_mana_drain at a dummy NPC with a real mana pool (c_sheep_woolly
		/// has none by default - NPC.MaxMana is a plain stored field, not Int-derived, so it must
		/// be set explicitly here). Reports both starting manas so the protocol test can confirm
		/// the target lost mana AND the caster's own mana never went up (the "does not transfer"
		/// distinguishing feature vs. the not-yet-built Mana Vampire).</summary>
		[SteamFunction]
		public static void TestCastManaDrainAtDummy(Character self) {
			SpellDef spell = SpellDef.GetByDefname("s_mana_drain");

			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.MaxMana = 500;
			dummy.Mana = 500;
			dummy.SetRealSkillValue((int) SkillName.Magery, 500); //#962 - Mana Drain leaves a mind without Magery alone
			// Confirmed directly (intermittent test failures, ~1 in 3-4 runs): RegenerationPlugin
			// ticks every ~1s (MIN_TIMER/MAX_TIMER=1.0) - the test's own margin for CASTTIME+stroke
			// delay is comfortably long enough for several regen ticks to fully restore the drain
			// before the result gets checked, racing the assertion rather than the spell itself
			// being unreliable. Removing the dummy's own regen makes the drain's effect
			// deterministic to observe, matching this file's established "pin down the one
			// variable that would otherwise make a result racy" pattern (e.g. TestGrantFullSpellbook
			// maxing Magery skill for exactly this reason).
			dummy.DeletePlugin(RegenerationPlugin.regenerationsPluginKey);

			SpellBook book = FindBook(self);
			int startCasterMana = self.Mana;
			int startTargetMana = dummy.Mana;

			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, dummy, null, book, spell, null);
			magery.PhaseSelect();

			Console.WriteLine("MAGERY_MANADRAIN_TEST: STARTED dummyUid=" + dummy.Uid +
				" startCasterMana=" + startCasterMana + " startTargetMana=" + startTargetMana);
		}

		[SteamFunction]
		public static void TestReportManaDrainResult(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			Console.WriteLine("MAGERY_MANADRAIN_TEST: RESULT casterMana=" + self.Mana + " targetMana=" + target.Mana);
		}

		/// <summary>Same ScriptArgs dual-shape handling already established elsewhere this
		/// session (LevelingTests.cs/AttrFlagTests.cs) - copied rather than shared, matching
		/// this codebase's existing per-file convention.</summary>
		private static string[] SplitArgs(ScriptArgs sa) {
			if ((sa.Argv.Length == 1) && (sa.Argv[0] is string combined)) {
				return combined.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			}
			string[] parts = new string[sa.Argv.Length];
			for (int i = 0; i < sa.Argv.Length; i++) {
				parts[i] = Convert.ToString(sa.Argv[i], CultureInfo.InvariantCulture);
			}
			return parts;
		}
	}
}
