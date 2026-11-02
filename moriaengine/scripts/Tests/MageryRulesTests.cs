using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/MageryRulesTests.cs.</summary>
	public static class MageryRulesTestHelpers {
		private static NPC dummy;

		private static SpellBook BookOf(Character self) {
			SpellBook book = self.Backpack.FindByClass(typeof(SpellBook)) as SpellBook;
			if (book == null) {
				book = (SpellBook) ThingDef.GetByDefname("i_spellbook_3").Create(self.Backpack);
				foreach (SpellDef spell in SpellDef.AllSpellDefs) {
					book.AddSpell(spell);
				}
			}
			return book;
		}

		[SteamFunction]
		public static void TestRulesPrepare(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M);
			self.SetRealSkillValue((int) SkillName.Magery, int.Parse(sa.Args.Trim('"'), CultureInfo.InvariantCulture));
			self.MaxMana = 200;
			self.Mana = 200;
			dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if ((script is ItemDef def) && def.PrettyDefname.StartsWith("i_reag_", StringComparison.OrdinalIgnoreCase)) {
					((Item) def.Create(self.Backpack)).Amount = 20;
				}
			}
			Console.WriteLine("RULES_READY: book=#" + BookOf(self).Uid);
		}

		[SteamFunction]
		public static void TestRulesCast(Character self, ScriptArgs sa) {
			string[] words = sa.Args.Trim('"').Split(' ');
			SpellDef spell = SpellDef.GetByDefname(words[0]);
			IPoint3D target = (words.Length > 1) && (words[1] == "self") ? self : dummy;
			self.Mana = 200;
			SkillSequenceArgs.Acquire(self, SkillName.Magery, (Thing) target, null, BookOf(self), spell, null).PhaseSelect();
			Console.WriteLine("RULES_CAST: mana=" + self.Mana + " skill=" + self.CurrentSkillName);
		}

		[SteamFunction]
		public static void TestRulesReport(Character self) {
			PoisonTimer poison = dummy.GetTimer(PoisonTimer.KeyOf(PoisonKind.AbsoluteDamage)) as PoisonTimer;
			Console.WriteLine("RULES_REPORT: mana=" + self.Mana + " hits=" + dummy.Hits + " poison=" + poison?.strength + " share=" + StatModSpellsUtils.Lowering(dummy, 200, -15));
		}

		[SteamFunction]
		public static void TestRulesValues(Character self) {
			SpellDef flame = SpellDef.GetByDefname("s_flamestrike");
			Console.WriteLine("RULES_VALUES: time=" + flame.CastTime.ToString(CultureInfo.InvariantCulture) + " mana=" + flame.ManaUse + " difficulty=" + flame.Difficulty
				+ " scroll=" + flame.GetDifficulty(true));
		}
	}
}
