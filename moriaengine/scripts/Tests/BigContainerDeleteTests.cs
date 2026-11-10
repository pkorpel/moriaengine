using System;
using System.Diagnostics;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1228 item 7, #1364 - how long one Delete of a big container holds the world: a chest of n daggers, the same split
	/// into bags of 100 (as a bank box or a house chest holds them), n daggers each with a decay timer and a tag, and a chest open
	/// to someone (a looter at a decaying corpse). Measured 2026-11-10 in Debug: about 1 us an item; an open one was 23 us an
	/// item (every item checked each viewer's reach and sent its own delete: 20,000 = 266-467 ms) until #1364 skipped that for
	/// contents deleted with their container (15 ms). No slicing needed.</summary>
	public static class BigContainerDeleteTests {
		private static readonly TagKey MarkTK = TagKey.Acquire("_bigdelete_");

		private static Item Chest(Character self) {
			return (Item) ((ItemDef) ThingDef.GetByDefname("i_chest_wooden_brass")).Create(self.X + 1, self.Y + 1, self.Z, self.M);
		}

		private static double TimeDelete(Item container) {
			GC.Collect();
			long t0 = Stopwatch.GetTimestamp();
			container.Delete();
			return (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
		}

		[SteamFunction]
		public static void TestBigContainerDelete(Character self) {
			ItemDef dagger = (ItemDef) ThingDef.GetByDefname("i_dagger"), bag = (ItemDef) ThingDef.GetByDefname("i_bag");
			double worstTimed = 0, worstOpen = 0;
			bool allGone = true;
			foreach (int n in new[] { 150, 1000, 5000, 20000 }) {
				int before = AbstractItem.Instances;
				Item flat = Chest(self);
				for (int i = 0; i < n; i++) {
					dagger.Create((Container) flat);
				}
				Item nested = Chest(self);
				for (int b = 0; b < n / 100; b++) {
					Container inner = (Container) bag.Create((Container) nested);
					for (int i = 0; i < 100; i++) {
						dagger.Create(inner);
					}
				}
				Item timed = Chest(self);
				for (int i = 0; i < n; i++) {
					Item it = (Item) dagger.Create((Container) timed);
					ItemDecaySystem.ScheduleIn(it, 3600);
					it.SetTag(MarkTK, i);
				}
				Item open = Chest(self); //the review's case: someone has it open (a looter at a corpse that decays)
				for (int i = 0; i < n; i++) {
					dagger.Create((Container) open);
				}
				OpenedContainers.SetContainerOpened(self, open);
				double flatMs = TimeDelete(flat), nestedMs = TimeDelete(nested), timedMs = TimeDelete(timed), openMs = TimeDelete(open);
				allGone &= (AbstractItem.Instances == before); //every chest, bag and dagger
				if (n == 20000) {
					worstTimed = timedMs;
					worstOpen = openMs;
				}
				Console.WriteLine("BIGDELETE: n=" + n + " flatMs=" + flatMs.ToString("F1", CultureInfo.InvariantCulture) +
					" nestedMs=" + nestedMs.ToString("F1", CultureInfo.InvariantCulture) + " timedMs=" + timedMs.ToString("F1", CultureInfo.InvariantCulture) +
					" openMs=" + openMs.ToString("F1", CultureInfo.InvariantCulture));
			}
			Console.WriteLine("BIGDELETE_CHECK: allGone=" + allGone);
			//39 ms measured in Debug: a generous bound, it's a guard against something per item becoming slow
			Console.WriteLine("BIGDELETE_CHECK: twentyThousandTimedUnderHalfASecond=" + (worstTimed < 500));
			//#1364 - 266-467 ms before the fix, 15 after: every item checked each viewer's reach and sent its own delete
			Console.WriteLine("BIGDELETE_CHECK: twentyThousandOpenUnderATenthOfASecond=" + (worstOpen < 100));
			Console.WriteLine("BIGDELETE_DONE");
		}

		private static readonly TagKey ViewChestTK = TagKey.Acquire("_bigdelete_chest_");

		/// <summary>#1364 - a chest of 50 daggers beside the player, for the client to open.</summary>
		[SteamFunction]
		public static void TestBigContainerViewSetup(Character self) {
			ItemDef dagger = (ItemDef) ThingDef.GetByDefname("i_dagger");
			Item chest = (Item) ((ItemDef) ThingDef.GetByDefname("i_chest_wooden_brass")).Create(self.X + 1, self.Y, self.Z, self.M);
			var uids = new System.Collections.Generic.List<int>();
			for (int i = 0; i < 50; i++) {
				uids.Add(((Item) dagger.Create((Container) chest)).Uid);
			}
			self.SetTag(ViewChestTK, chest);
			Console.WriteLine("BIGDELETE_VIEW: chest=" + chest.Uid + " daggers=" + string.Join(",", uids) + " end");
		}

		/// <summary>The control: one dagger alone is deleted from the open chest (its viewer gets that dagger's delete).</summary>
		[SteamFunction]
		public static void TestBigContainerViewDeleteOne(Character self) {
			Item chest = (Item) self.GetTag(ViewChestTK);
			Item one = null;
			foreach (Item it in (Container) chest) {
				one = it;
				break;
			}
			int uid = one.Uid; //-1 once deleted
			one.Delete();
			Console.WriteLine("BIGDELETE_VIEWONE: dagger=" + uid + " gone=" + one.IsDeleted + " end");
		}

		/// <summary>The opened chest is deleted (its client should get the chest's delete, none for the daggers in it).</summary>
		[SteamFunction]
		public static void TestBigContainerViewDelete(Character self) {
			Item chest = (Item) self.GetTag(ViewChestTK);
			bool open = OpenedContainers.HasContainerOpen(self, chest).Allow;
			chest.Delete();
			Console.WriteLine("BIGDELETE_VIEWDELETED: wasOpen=" + open + " gone=" + chest.IsDeleted + " end");
		}
	}
}
