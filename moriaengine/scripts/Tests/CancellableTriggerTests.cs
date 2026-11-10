using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1370 - Thing.CancellableTrigger (the one that doesn't catch exceptions) called itself instead of its base, so any
	/// call overflowed the stack. On a dagger and an NPC: without the trigger group it continues, with it (as EVENTS) it's cancelled.</summary>
	public static class CancellableTriggerTests {
		private static readonly TriggerKey testTK = TriggerKey.Acquire("testcancellable");

		[SteamFunction]
		public static void TestCancellableTrigger(Character self) {
			TriggerGroup group = TriggerGroup.GetByDefname("e_test_cancellable");
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X + 1, self.Y, self.Z, self.M);
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_rabbit").Create(self.X + 2, self.Y, self.Z, self.M);
			string line = "";
			foreach (Thing thing in new Thing[] { dagger, rabbit }) {
				TriggerResult before = thing.CancellableTrigger(testTK, null);
				thing.AddTriggerGroup(group);
				TriggerResult after = thing.CancellableTrigger(testTK, null);
				line += " " + ((thing is Item) ? "dagger" : "rabbit") + "=" + before + "/" + after;
				thing.Delete();
			}
			Console.WriteLine("CANCELLABLE_TRIGGER:" + line + " end");
		}
	}
}
