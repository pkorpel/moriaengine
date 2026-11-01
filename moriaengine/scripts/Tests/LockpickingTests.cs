using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#962 - what Erebor's lockpicking touches.</summary>
	public static class LockpickingTests {
		private static Item locked, plain, faraway;
		private static readonly TagKey difficultyTK = TagKey.Acquire("lockpick");

		private static Item Box(Character self, string type, int dx) {
			Item box = (Item) ((ItemDef) ThingDef.GetByDefname("i_backpack")).Create(self.Backpack);
			box.P((ushort) (self.X + dx), self.Y, self.Z, self.M);
			box.Type = (TriggerGroup) TriggerGroup.GetByDefname(type);
			return box;
		}

		/// <summary>A plain player with a pick of 3 tries, a lock beyond them, an open box and a lock out of reach. A candle to see by.</summary>
		[SteamFunction]
		public static void TestLockpickSetup(Character self) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Lockpicking, 500);
			Item pick = (Item) ((ItemDef) ThingDef.GetByDefname("i_lockpick_3")).Create(self.Backpack);
			locked = Box(self, "t_container_locked", 1);
			locked.SetTag(difficultyTK, 5000);
			plain = Box(self, "t_container", 0);
			faraway = Box(self, "t_container_locked", 5);
			((Item) ((ItemDef) ThingDef.GetByDefname("i_candle_lit")).Create(self.Backpack)).P(self.X, self.Y, self.Z, self.M);
			Console.WriteLine("LOCKPICK_SETUP: pick=" + (uint) pick.FlaggedUid + " locked=" + (uint) locked.FlaggedUid + " plain=" + (uint) plain.FlaggedUid
				+ " faraway=" + (uint) faraway.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}

		/// <summary>The lock made easy, and where the picks stand.</summary>
		[SteamFunction]
		public static void TestLockpickEasy(Character self) {
			self.SetRealSkillValue((int) SkillName.Lockpicking, 1000);
			locked.SetTag(difficultyTK, -5000);
			Console.WriteLine("LOCKPICK_EASY:");
		}

		[SteamFunction]
		public static void TestLockpickState(Character self) {
			string picks = "";
			uint first = 0;
			foreach (string defname in new[] { "i_lockpick", "i_lockpick_2", "i_lockpick_3" }) {
				int count = 0;
				foreach (AbstractItem item in self.Backpack) {
					if (item.Def == ThingDef.GetByDefname(defname)) {
						count += item.Amount;
						first = (uint) item.FlaggedUid;
					}
				}
				picks += " " + defname + "=" + count;
			}
			Console.WriteLine("LOCKPICK_STATE:" + picks + " held=" + first + " type=" + locked.Type.Defname + " picked=" + locked.HasTag(TagKey.Acquire("lockpicked")));
		}

		private static Item key;

		/// <summary>A key in the pack, or gone again.</summary>
		[SteamFunction]
		public static void TestLockpickKey(Character self) {
			if (key == null) {
				key = (Item) ((ItemDef) ThingDef.GetByDefname("i_key_copper")).Create(self.Backpack);
			} else {
				key.Delete();
				key = null;
			}
			Console.WriteLine("LOCKPICK_KEY:");
		}
	}
}
