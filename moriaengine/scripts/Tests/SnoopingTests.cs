using System;

namespace SteamEngine.CompiledScripts {
	public static class SnoopingTests {
		private static NPC mark;
		private static Item prize;

		/// <summary>A plain snoop beside a person of a given Dexterity, with something in their pack.</summary>
		[SteamFunction]
		public static void TestSnoopReady(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim('"').Split(' ');
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Snooping, int.Parse(args[0]));
			if (mark != null) {
				mark.Delete();
			}
			mark = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.P());
			mark.Dex = short.Parse(args[1]);
			mark.Karma = 0;
			prize = (Item) ((ItemDef) ThingDef.GetByDefname("i_dagger")).Create(mark.Backpack);
			self.Karma = 0;
			Console.WriteLine("SNOOP_READY: pack=" + (uint) mark.Backpack.FlaggedUid + " prize=" + (uint) prize.FlaggedUid);
		}

		[SteamFunction]
		public static void TestSnoopSteal(Character self) {
			SkillSequenceArgs stealing = SkillSequenceArgs.Acquire(self, SkillName.Stealing, prize, null, null, null, null);
			stealing.PhaseSelect();
			Console.WriteLine("SNOOP_STEAL: success=" + stealing.Success + " stillTheirs=" + (prize.TopObj() == mark));
		}

		[SteamFunction]
		public static void TestSnoopState(Character self) {
			Console.WriteLine("SNOOP_STATE: karma=" + self.Karma + " plugin=" + self.HasPlugin(SnoopingSkillDef.snoopedPluginKey));
		}
	}
}
