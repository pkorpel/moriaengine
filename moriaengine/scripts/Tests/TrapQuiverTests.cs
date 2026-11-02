using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/TrapQuiverTests.cs.</summary>
	public static class TrapQuiverTestHelpers {
		private static Item trap;
		private static Item quiver;

		[SteamFunction]
		public static void TestTrapReady(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			self.MaxHits = 1000;
			self.Hits = 1000;
			trap = (Item) ThingDef.GetByDefname("i_past").Create(self.X + 1, self.Y, self.Z, self.M);
			quiver = (Item) ThingDef.GetByDefname("i_toulec").Create(self.Backpack);
			Item spare = (Item) ThingDef.GetByDefname("i_past").Create(self.X, self.Y + 1, self.Z, self.M);
			Item arrows = (Item) ThingDef.GetByDefname("i_arrow_piercing_copper").Create(self.Backpack);
			arrows.Amount = 30;
			Console.WriteLine("TRAP_READY: trap=#" + trap.Uid + " spare=#" + spare.Uid + " pack=#" + self.Backpack.Uid + " quiver=#" + quiver.Uid + " arrows=#" + arrows.Uid);
		}

		[SteamFunction]
		public static void TestTrapStep(Character self) {
			int before = self.Hits;
			SingletonScript<t_trap_custom>.Instance.On_Step(trap, self);
			Console.WriteLine("TRAP_STEPPED: lost=" + (before - self.Hits));
		}

		[SteamFunction]
		public static void TestQuiverReport(Character self) {
			Console.WriteLine("QUIVER_REPORT: count=" + t_quiver.CountIn(quiver) + " name=" + quiver.GetTag(t_quiver.nameTK));
		}
	}
}
