using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helpers for Tests/Protocol/DoorTests.cs.</summary>
	public static class DoorTests {
		private static Item door;

		private static Item Make(string defname, Character self) {
			return (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(self.X + 1, self.Y, self.Z, self.M);
		}

		/// <summary>Every offset once: where the door lands, against Sphere's table.</summary>
		[SteamFunction]
		public static void TestDoorTable(Character self) {
			string line = "";
			for (int offset = 0; offset < 16; offset++) {
				Item probe = Make("i_flip_door_probe", self);
				probe.Model = probe.TypeDef.Model + offset;
				int x = probe.X, y = probe.Y;
				probe.UseDoor();
				line += (probe.Model - probe.TypeDef.Model) + ":" + (probe.X - x) + "," + (probe.Y - y) + " ";
				probe.Delete();
			}
			Console.WriteLine("DOOR_TABLE: " + line.Trim());
		}

		/// <summary>A dupe model is its master, and so a door.</summary>
		[SteamFunction]
		public static void TestDoorDupe(Character self) {
			Item second = Make("i_gate_iron", self);
			Item dupe = (Item) ((ItemDef) ThingDef.GetByDefname("i_0x826")).Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("DOOR_DUPE: type=" + dupe.Type.Defname + " offset=" + (dupe.Model - dupe.TypeDef.Model));
			second.Delete();
			dupe.Delete();
		}

		[SteamFunction]
		public static void TestDoorSetup(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			if (door != null) {
				door.Delete();
			}
			door = Make("i_flip_door_probe", self);
			if (sa.Args == "locked") {
				door.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door_locked");
				door.LockUid = 77;
			}
			Console.WriteLine("DOOR_SETUP: uid=" + (uint) door.FlaggedUid + " model=" + door.Model);
		}

		[SteamFunction]
		public static void TestDoorState(Character self) {
			DoorCloseTimer timer = (DoorCloseTimer) door.GetTimer(DoorCloseTimer.Key);
			Console.WriteLine("DOOR_STATE: open=" + door.IsDoorOpen + " model=" + door.Model + " timer=" + (timer != null));
		}

		/// <summary>The door's 60 seconds, cut short.</summary>
		[SteamFunction]
		public static void TestDoorHurry(Character self) {
			door.GetTimer(DoorCloseTimer.Key).DueInSeconds = 1;
			Console.WriteLine("DOOR_HURRY: done");
		}

		[SteamFunction]
		public static void TestDoorKey(Character self, ScriptArgs sa) {
			Item key = (Item) ((ItemDef) ThingDef.GetByDefname("i_door_key_probe")).Create(self.Backpack);
			key.LockUid = Convert.ToInt32(sa.Args, System.Globalization.CultureInfo.InvariantCulture);
			Console.WriteLine("DOOR_KEY: done");
		}

		[SteamFunction]
		public static void TestDoorGm(Character self) {
			self.Account.PLevel = 7;
			Console.WriteLine("DOOR_GM: done");
		}

		/// <summary>A closed door stops a step onto its tile, an open one has moved away.</summary>
		[SteamFunction]
		public static void TestDoorBlocks(Character self) {
			self.Account.PLevel = 1;
			self.Direction = Direction.East; //a step the other way only turns
			int start = self.X;
			self.WalkRunOrFly(Direction.East, false, false);
			bool free = self.X != start;
			Item blocker = Make("i_flip_door_probe", self);
			int x = self.X;
			self.WalkRunOrFly(Direction.East, false, false);
			Console.WriteLine("DOOR_BLOCK: free=" + free + " moved=" + (self.X != x));
			blocker.Delete();
		}
	}
}
