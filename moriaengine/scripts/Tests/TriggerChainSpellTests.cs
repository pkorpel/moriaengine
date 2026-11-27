using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1558 - chain probes for a cast: a trigger group on the caster and its target records which triggers a cast's start
	/// and its success raise, in what order, and what a starving caster's attempt does. Written against the code before #1558,
	/// then the rework changed what Tests/Protocol/TriggerChainSpellTests.cs expects. Before: a start was me:skillStart,
	/// me:spellCastTry,me:denySpellCast,me:spellCast (a refused one stopped after denySpellCast), and a starving cast was hurt
	/// (10) and still got under way. The success line (power stages, then the effect's) is unchanged.</summary>
	public static class TriggerChainSpellTests {
		internal static readonly List<string> log = new List<string>();
		internal static Thing me;

		internal static void Log(Thing who, string what) {
			log.Add(((who == me) ? "me" : "it") + ":" + what);
		}

		private static void Check(string name, bool ok) {
			Console.WriteLine("CHAINS_CHECK: " + name + "=" + ok);
		}

		private static string Logged() {
			string s = string.Join(",", log);
			log.Clear();
			return s;
		}

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

		private static NPC Ready(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			self.MaxMana = 1000;
			self.Mana = 1000;
			self.Hits = self.MaxHits;
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if ((script is ItemDef def) && def.PrettyDefname.StartsWith("i_reag_", StringComparison.OrdinalIgnoreCase)) {
					((Item) def.Create(self.Backpack)).Amount = 50;
				}
			}
			HungerPlugin hunger = HungerPlugin.Of(self);
			if (hunger != null) {
				hunger.Food = 15;
			}
			NPC dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;
			NPCAiTimer.Freeze(dummy, 120);
			me = self;
			log.Clear();
			E_ChainProbeSpell.refuseCast = false;
			self.AddTriggerGroup(SingletonScript<E_ChainProbeSpell>.Instance);
			dummy.AddTriggerGroup(SingletonScript<E_ChainProbeSpell>.Instance);
			return dummy;
		}

		private static void Done(Character self, NPC dummy) {
			self.AbortSkill();
			self.RemoveTriggerGroup(SingletonScript<E_ChainProbeSpell>.Instance);
			E_ChainProbeSpell.refuseCast = false;
			HungerPlugin hunger = HungerPlugin.Of(self);
			if (hunger != null) {
				hunger.Food = 15;
			}
			if ((dummy != null) && !dummy.IsDeleted) {
				dummy.Delete();
			}
		}

		private static SkillSequenceArgs Cast(Character self, string spellName, IPoint4D target) {
			return SkillSequenceArgs.Acquire(self, SkillName.Magery, target, null, BookOf(self), SpellDef.GetByDefname(spellName), null);
		}

		/// <summary>A magic arrow on a sheep: what its start raises (the cast under way, its stroke waiting), then what its
		/// success raises per target. A refused cast stops before the spell's start.</summary>
		[SteamFunction]
		public static void TestChainCast(Character self) {
			NPC dummy = Ready(self);
			try {
				Cast(self, "s_magic_arrow", dummy).PhaseStart();
				string started = Logged();
				self.AbortSkill();
				Logged();
				Console.WriteLine("CHAINS_START: " + started + " |");
				Check("startOrder", started == "me:skillStart,me:denySpellCast,me:spellCast");

				self.Mana = 1000;
				Cast(self, "s_magic_arrow", dummy).PhaseSuccess();
				string succeeded = Logged();
				Console.WriteLine("CHAINS_SUCCESS: " + succeeded + " |");
				Check("successOrder", succeeded == "me:monsterSpellPower,me:spellPower,me:beforeDoEffect,it:beforeGetEffect,me:causeSpellEffect,it:spellEffect,me:spellSuccess");

				E_ChainProbeSpell.refuseCast = true;
				self.Mana = 1000;
				Cast(self, "s_magic_arrow", dummy).PhaseStart();
				string refused = Logged();
				self.AbortSkill();
				Logged();
				E_ChainProbeSpell.refuseCast = false;
				Console.WriteLine("CHAINS_REFUSED: " + refused + " |");
				Check("refusedStopsBeforeTheCast", refused == "me:skillStart,me:denySpellCast");
			} finally {
				Done(self, dummy);
			}
			Console.WriteLine("CHAINS_CAST_DONE");
		}

		/// <summary>A starving caster tries a magic arrow: the hunger damage, and whether the cast still gets under way.</summary>
		[SteamFunction]
		public static void TestChainStarvingCast(Character self) {
			NPC dummy = Ready(self);
			try {
				HungerPlugin hunger = HungerPlugin.Of(self);
				hunger.Food = 0;
				int hits = self.Hits;
				Cast(self, "s_magic_arrow", dummy).PhaseStart();
				string tried = Logged();
				bool underWay = self.CurrentSkillArgs != null;
				self.AbortSkill();
				Logged();
				Console.WriteLine("CHAINS_STARVING: " + tried + " hurt=" + (hits - self.Hits) + " underWay=" + underWay + " |");
				Check("starvingHurts", self.Hits < hits);
				Check("starvingFails", !underWay && !tried.Contains("me:spellCast"));
			} finally {
				Done(self, dummy);
			}
			Console.WriteLine("CHAINS_STARVING_DONE");
		}
	}

	/// <summary>On the caster and its target: the cast's triggers. `refuseCast` cancels the caster's @denySpellCast.</summary>
	public partial class E_ChainProbeSpell : CompiledTriggerGroup {
		internal static bool refuseCast;

		public void On_SkillStart(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "skillStart"); }
		public void On_SpellCastTry(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "spellCastTry"); }
		public TriggerResult On_DenySpellCast(Character self, ScriptArgs args) {
			TriggerChainSpellTests.Log(self, "denySpellCast");
			return refuseCast ? TriggerResult.Cancel : TriggerResult.Continue;
		}
		public void On_SpellCast(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "spellCast"); }
		public void On_MonsterSpellPower(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "monsterSpellPower"); }
		public void On_SpellPower(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "spellPower"); }
		public void On_BeforeDoEffect(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "beforeDoEffect"); }
		public void On_BeforeGetEffect(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "beforeGetEffect"); }
		public void On_CauseSpellEffect(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "causeSpellEffect"); }
		public void On_SpellEffect(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "spellEffect"); }
		public void On_SpellSuccess(Character self, ScriptArgs args) { TriggerChainSpellTests.Log(self, "spellSuccess"); }
	}
}
