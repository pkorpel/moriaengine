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

		/// <summary>#485 - scans for a nearby summoned creature owned by self. Reports "none"
		/// rather than throwing if it's already despawned, so the same command can confirm both
		/// "it appeared" (shortly after casting) and "it's gone" (after the duration elapses).</summary>
		[SteamFunction]
		public static void TestReportNearbySummon(Character self) {
			foreach (AbstractCharacter ac in self.GetMap().GetCharsInRange(self.X, self.Y, 5)) {
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
