using System;
using SteamEngine.Persistence;
using SteamEngine.Timers;

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

		/// <summary>#1187 - an open door whose close timer is due in the given seconds, saved with the world: a recompile saves it
		/// and loads it again, timer included.</summary>
		[SteamFunction]
		public static void TestTimerArmSaved(Character self, ScriptArgs sa) {
			int seconds = int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			Item probe = (Item) ((ItemDef) ThingDef.GetByDefname("i_flip_door_probe")).Create(self.X, self.Y, self.Z, self.M);
			probe.UseDoor();
			probe.GetTimer(DoorCloseTimer.Key).DueInSeconds = seconds;
			Console.WriteLine("TIMER_SAVED: uid=" + probe.Uid + " open=" + probe.IsDoorOpen + " |");
		}

		/// <summary>#1188 - many timers on Globals firing every tenth of a second, saved with it, so the timer thread is busy
		/// firing them when a recompile takes globalLock and waits holding one. A recompile replaces Globals with a new instance
		/// and the old one never reads as deleted (unlike a forgotten Thing, whose bound timers drop themselves): a probe firing
		/// for a Globals that isn't the current one is an old timer the thread carried through the recompile.</summary>
		[SteamFunction]
		public static void TestStaleProbeArm(Character self, ScriptArgs sa) {
			int count = int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			for (int i = 0; i < count; i++) {
				Globals.Instance.AddTimer(TimerKey.Acquire("_stale_probe_" + i), new StaleProbeTimer()).PeriodInSeconds = 0.1;
			}
			Console.WriteLine("STALE_ARMED: count=" + count + " |");
		}

		[SteamFunction]
		public static void TestStaleProbeStop(Character self, ScriptArgs sa) {
			int count = int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			int found = 0;
			for (int i = 0; i < count; i++) {
				TimerKey key = TimerKey.Acquire("_stale_probe_" + i);
				if (Globals.Instance.GetTimer(key) != null) {
					found++;
					Globals.Instance.DeleteTimer(key);
				}
			}
			Console.WriteLine("STALE_STOPPED: found=" + found + " fired=" + StaleProbeTimer.fired + " |");
		}

		/// <summary>The door TestTimerArmSaved made, found by its uid (after a recompile no script static survives).</summary>
		[SteamFunction]
		public static void TestTimerDoorAt(Character self, ScriptArgs sa) {
			int uid = int.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture);
			Item door = Thing.UidGetThing(uid) as Item;
			Console.WriteLine("TIMER_DOOR: found=" + (door != null) + " open=" + ((door != null) && door.IsDoorOpen) + " |");
		}
	}

	/// <summary>#1188 - see TimerBurstTests.TestStaleProbeArm. Quiet but for a count, unless it fires for an old Globals.</summary>
	[SaveableClass, DeepCopyableClass]
	public class StaleProbeTimer : BoundTimer {
		internal static int fired;

		[LoadingInitializer, DeepCopyImplementation]
		public StaleProbeTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			fired++;
			if (cont != Globals.Instance) {
				Console.WriteLine("STALE_PROBE: a timer of the old world fired |");
			}
		}
	}
}
