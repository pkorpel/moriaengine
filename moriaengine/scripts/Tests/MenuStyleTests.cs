using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1004 - helpers for Tests/Protocol/MenuStyleTests.cs.</summary>
	public static class MenuStyleTestHelpers {
		private static SpellBook book;
		private static NPC rabbit;

		[SteamFunction]
		public static void TestMenuReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mage;
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			self.SetRealSkillValue((int) SkillName.Tracking, 1000);
			book = (SpellBook) ThingDef.GetByDefname("i_spellbook").Create(self.Backpack);
			book.AddSpell(SpellDef.GetByDefname("s_polymorph"));
			book.AddSpell(SpellDef.GetByDefname("s_summon_creature"));
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y + 1, self.Z, self.M);
			rabbit = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 2, self.Y, self.Z, self.M);
			rabbit.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			Console.WriteLine("MENU_READY: rabbit=" + rabbit.Name + " model=" + self.Model + " daemon=" + ThingDef.GetByDefname("c_daemon").Model + " man=" + ThingDef.GetByDefname("c_man").Model);
		}

		[SteamFunction]
		public static void TestMenuPlevel(Character self, ScriptArgs sa) {
			self.Account.PLevel = Convert.ToByte(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			Console.WriteLine("MENU_PLEVEL: done");
		}

		[SteamFunction]
		public static void TestMenuTrack(Character self) {
			self.SelectSkill(SkillName.Tracking);
			Console.WriteLine("MENU_TRACK: done");
		}

		[SteamFunction]
		public static void TestMenuCast(Character self, ScriptArgs sa) {
			self.Mana = self.MaxMana;
			SkillSequenceArgs.Acquire(self, SkillName.Magery, null, null, book, SpellDef.GetByDefname(sa.Argv[0].ToString()), null).PhaseSelect();
			Console.WriteLine("MENU_CAST: done");
		}

		[SteamFunction]
		public static void TestMenuReport(Character self) {
			Console.WriteLine("MENU_REPORT: tracking=" + (self.GetPlugin(NPCTrackingPlugin.npcTrackingPluginKey) != null)
				+ " model=" + self.Model + " open=" + self.HasOpenedDialog(SingletonScript<D_PolymorphMenu>.Instance)
				+ " skill=" + (self.CurrentSkillName != SkillName.None));
		}

		[SteamFunction]
		public static void TestMenuEnd(Character self) {
			PolymorphSpellDef.End(self);
			self.DeletePlugin(NPCTrackingPlugin.npcTrackingPluginKey);
			Console.WriteLine("MENU_END: done");
		}
	}
}
