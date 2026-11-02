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
