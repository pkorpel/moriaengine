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

			SpellBook book = self.FindLayer(1) as SpellBook;
			if (book == null) {
				foreach (Item i in self.Backpack.EnumShallow()) {
					book = i as SpellBook;
					if (book != null) {
						break;
					}
				}
			}

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
