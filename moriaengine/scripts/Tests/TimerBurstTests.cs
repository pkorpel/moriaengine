using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#960 - server-side helpers for Tests/Protocol/TimerBurstTests.cs.</summary>
	public static class TimerBurstTests {
		[SteamFunction]
		public static void TestTimerBurst(Character self) {
			Item probe = (Item) ((ItemDef) ThingDef.GetByDefname("i_flip_door_probe")).Create(self.X, self.Y, self.Z, self.M);
			for (int i = 0; i < 20000; i++) {
				probe.AddTimer(DoorCloseTimer.Key, new DoorCloseTimer()).DueInSeconds = 60;
				probe.DeleteTimer(DoorCloseTimer.Key);
			}
			probe.Delete();
			Console.WriteLine("TIMER_BURST: done");
		}

		/// <summary>The timer thread is alive if a timer still fires after the burst.</summary>
		[SteamFunction]
		public static void TestTimerAfterBurst(Character self) {
			Item probe = (Item) ((ItemDef) ThingDef.GetByDefname("i_flip_door_probe")).Create(self.X, self.Y, self.Z, self.M);
			probe.UseDoor();
			probe.GetTimer(DoorCloseTimer.Key).DueInSeconds = 1;
			lastProbe = probe;
			Console.WriteLine("TIMER_ARMED: open=" + probe.IsDoorOpen);
		}

		/// <summary>Every Thing enumerated after each new one, past where the array of all Things grows.</summary>
		[SteamFunction]
		public static void TestThingsFillingTheArray(Character self) {
			ItemDef def = (ItemDef) ThingDef.GetByDefname("i_gold");
			System.Collections.Generic.List<Item> made = new System.Collections.Generic.List<Item>();
			try {
				for (int i = 0; i < 2100; i++) {
					made.Add((Item) def.Create(self.X, self.Y, self.Z, self.M));
					int seen = 0;
					foreach (Thing thing in Thing.AllThings) {
						seen++;
					}
				}
				Console.WriteLine("THINGS_ARRAY: good made=" + made.Count);
			} catch (IndexOutOfRangeException) {
				Console.WriteLine("THINGS_ARRAY: threw at " + made.Count);
			}
			foreach (Item item in made) {
				item.Delete();
			}
		}

		private static Item lastProbe;

		[SteamFunction]
		public static void TestTimerFired(Character self) {
			Console.WriteLine("TIMER_FIRED: open=" + lastProbe.IsDoorOpen);
		}
	}
}
