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

		/// <summary>Karma as Sphere scales it, a seen crime remembered for the guards, a person's reaction.</summary>
		[SteamFunction]
		public static void TestSnoopWitness(Character self) {
			self.Karma = 200;
			CrimeWitness.ChangeKarma(self, -10, -500);
			int doubled = self.Karma;
			self.Karma = 105;
			CrimeWitness.ChangeKarma(self, -10, -500);
			self.Karma = -500;
			CrimeWitness.ChangeKarma(self, -10, -500);
			int floor = self.Karma;

			NPC seen = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.P());
			CrimeWitness.Notice(self, seen, null);
			bool remembered = CrimeWitness.SeenBy(self) == seen;
			bool criminal = Notoriety.IsCriminal(seen);
			seen.Delete();

			Console.WriteLine("SNOOP_WITNESS: doubled=" + doubled + " floor=" + floor + " remembered=" + remembered + " criminal=" + criminal);
		}

		[SteamFunction]
		public static void TestSnoopState(Character self) {
			Console.WriteLine("SNOOP_STATE: karma=" + self.Karma + " plugin=" + self.HasPlugin(SnoopingSkillDef.snoopedPluginKey));
		}
	}
}
