using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/NecromancyTests.cs.</summary>
	public static class NecromancyTestHelpers {
		private static SpellBook book;
		private static NPC dummy;

		[SteamFunction]
		public static void TestNecroReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player player = (Player) self;
			player.Profession = Professions.Necromant;
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			self.Int = 300;
			self.MaxMana = 300;
			self.Mana = 300;
			book = (SpellBook) ThingDef.GetByDefname("i_spellbook_necro").Create(self.Backpack);
			Item firebolt = (Item) ThingDef.GetByDefname("i_scroll_firebolt").Create(self.Backpack);
			Item frostbolt = (Item) ThingDef.GetByDefname("i_scroll_frostbolt").Create(self.Backpack);
			dummy = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			dummy.MaxHits = 5000;
			dummy.Hits = 5000;
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("NECRO_READY: pack=#" + self.Backpack.Uid + " book=#" + book.Uid + " firebolt=#" + firebolt.Uid + " frostbolt=#" + frostbolt.Uid + " dummy=#" + dummy.Uid + " has=" + book.HasSpell(69));
		}

		[SteamFunction]
		public static void TestNecroReport(Character self) {
			Console.WriteLine("NECRO_REPORT: firebolt=" + book.HasSpell(69) + " frostbolt=" + book.HasSpell(70) + " mana=" + self.Mana + " hits=" + dummy.Hits + " frozen=" + (dummy.GetPlugin(PluginKey.Acquire("_frostBolt_")) != null));
		}

		private static Corpse corpse;
		private static NPC raised;

		private static Corpse DropACorpse(Character self) {
			NPC victim = (NPC) ThingDef.GetByDefname("c_orc").Create(self.X + 1, self.Y, self.Z, self.M);
			victim.CauseDeath(null);
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is Corpse one) && !one.IsDeleted && (Point2D.GetSimpleDistance(one, self) <= 2)) {
					return one;
				}
			}
			return null;
		}

		[SteamFunction]
		public static void TestAnimateReady(Character self) {
			self.Mana = 300;
			corpse = DropACorpse(self);
			Console.WriteLine("ANIMATE_READY: corpse=#" + corpse.Uid + " dead=" + self.Flag_Dead + " x=" + (self.X + 1) + " y=" + self.Y + " z=" + self.Z);
		}

		[SteamFunction]
		public static void TestNecroAddAnimate(Character self) {
			book.AddSpell(66);
			Console.WriteLine("NECRO_ADDED: done");
		}

		[SteamFunction]
		public static void TestNecroAddSummon(Character self) {
			book.AddSpell(40);
			Console.WriteLine("NECRO_ADDED: done");
		}

		[SteamFunction]
		public static void TestAnimateStaff(Character self) {
			self.Mana = 300;
			Item staff = (Item) ThingDef.GetByDefname("i_staff_gnarled_mithril").Create(self.Backpack);
			self.TryEquip(staff);
			Console.WriteLine("ANIMATE_STAFF: done");
		}

		[SteamFunction]
		public static void TestAnimateReport(Character self) {
			raised = null;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is NPC npc) && !npc.IsDeleted && AnimateDeadSpellDef.IsAnimated(npc)) {
					raised = npc;
				}
			}
			Console.WriteLine("ANIMATE_REPORT: raised=" + (raised != null) + " owned=" + ((raised != null) && (raised.ControlMaster == self)) + " corpse=" + !corpse.IsDeleted + " mana=" + self.Mana);
		}

		[SteamFunction]
		public static void TestAnimatedDies(Character self) {
			Point4D where = raised.P();
			raised.CauseDeath(null);
			int corpses = 0;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is Corpse one) && !one.IsDeleted && (Point2D.GetSimpleDistance(one, where) == 0)) {
					corpses++;
				}
			}
			Console.WriteLine("ANIMATE_DIED: corpses=" + corpses);
		}

		[SteamFunction]
		public static void TestSummonRow(Character self, ScriptArgs sa) {
			self.Mana = 300;
			string creature = sa.Argv[0].ToString();
			Console.WriteLine("SUMMON_ROW: row=" + (Array.FindIndex(SummonMenu.rows, row => row.creature == creature) + 1));
		}

		[SteamFunction]
		public static void TestSummonReport(Character self, ScriptArgs sa) {
			ThingDef wanted = ThingDef.GetByDefname(sa.Argv[0].ToString());
			bool found = false;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing.Def == wanted) && (thing is NPC npc) && (npc.ControlMaster == self)) {
					found = true;
				}
			}
			Console.WriteLine("SUMMON_REPORT: found=" + found + " mana=" + self.Mana);
		}

		[SteamFunction]
		public static void TestBoneReady(Character self) {
			self.Mana = 300;
			book.AddSpell(67);
			Console.WriteLine("BONE_READY: done");
		}

		[SteamFunction]
		public static void TestBoneReport(Character self) {
			Console.WriteLine("BONE_REPORT: chest=" + (self.FindLayer(13)?.Def == ThingDef.GetByDefname("i_necro_chest")) + " mana=" + self.Mana + " guarded=" + (self.GetPlugin(PluginKey.Acquire("spellEffect_protection")) != null));
		}

		[SteamFunction]
		public static void TestSpellAudit(Character self) {
			EffectFlag flags = EffectFlag.FromSpellBook | EffectFlag.HarmfulEffect;
			dummy.SetRealSkillValue((int) SkillName.Magery, 0);
			dummy.MaxMana = 100;
			dummy.Mana = 100;
			SpellDef.GetByDefname("s_mana_drain").ApplyEffectChar(self, dummy, 1000, flags);
			int unskilled = dummy.Mana;
			dummy.SetRealSkillValue((int) SkillName.Magery, 500);
			SpellDef.GetByDefname("s_mana_drain").ApplyEffectChar(self, dummy, 1000, flags);
			int skilled = dummy.Mana;

			dummy.Flag_Hidden = true;
			SpellDef.GetByDefname("s_reveal").ApplyEffectChar(self, dummy, 1000, flags);
			bool hidden = dummy.Flag_Hidden;

			ParalyzeEffectPluginDef.Paralyze(dummy, PluginKey.Acquire("_auditPara_"), TimeSpan.FromSeconds(30));
			self.Int = 300;
			dummy.Int = 50;
			int hits = dummy.Hits;
			SpellDef.GetByDefname("s_mind_blast").ApplyEffectChar(self, dummy, 1000, flags);
			Console.WriteLine("SPELL_AUDIT: unskilled=" + unskilled + " skilled=" + skilled + " hidden=" + hidden + " paralyzed=" + ParalyzeEffectPluginDef.IsParalyzed(dummy) + " lost=" + (hits - dummy.Hits));
		}

		[SteamFunction]
		public static void TestNecroMakeMystic(Character self) {
			((Player) self).Profession = Professions.Mystic;
			book.AddSpell(SpellDef.GetByDefname("s_heal"));
			Console.WriteLine("NECRO_MYSTIC: done");
		}

		[SteamFunction]
		public static void TestNecroCast(Character self, ScriptArgs sa) {
			MagerySkillDef.TryCastSpellFromBook(self, SpellDef.GetByDefname(sa.Argv[0].ToString()));
			Console.WriteLine("NECRO_CAST: done");
		}
	}
}
