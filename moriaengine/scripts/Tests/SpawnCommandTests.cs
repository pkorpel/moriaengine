using System;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>Verification-harness diagnostics for the GM ".spawn" command (#191) - see
	/// Tests/Protocol/SpawnCommandTests.cs.</summary>
	public static class SpawnCommandTests {
		/// <summary>Prints whether a spawn gem (ItemDef altdefname "i_worldgem_bit") and at least
		/// one other character are present near self. The gem's canonical Defname is an
		/// auto-generated hex id (e.g. "i_0x1ea7") - "i_worldgem_bit" only exists as its
		/// Altdefname, which is why this checks Altdefname rather than Defname.</summary>
		[SteamFunction]
		public static void TestVerifySpawnResult(Character self) {
			bool foundGem = false;
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 20)) {
				if (item.Def.Altdefname != null &&
					item.Def.Altdefname.Equals("i_worldgem_bit", StringComparison.OrdinalIgnoreCase)) {
					foundGem = true;
					break;
				}
			}

			bool foundNpc = false;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				if (ch != self) {
					foundNpc = true;
					break;
				}
			}

			Console.WriteLine("SPAWN_TEST: gem=" + foundGem + " npc=" + foundNpc);
		}

		/// <summary>Reports the WanderLeash actually set on the nearest spawned NPC's AI timer
		/// (#206 - SpawnInfo.Homedist was stored on the gem but never applied anywhere; confirms
		/// spawn.scp's @spawnNow now wires it through to NPCAiTimer.GetFor(char).WanderLeash).</summary>
		[SteamFunction]
		public static void TestVerifySpawnedNpcWanderLeash(Character self) {
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				NPC npc = ch as NPC;
				if (npc == null) {
					continue;
				}
				NPCAiTimer timer = NPCAiTimer.GetFor(npc);
				if (timer != null) {
					Console.WriteLine("WANDER_LEASH_TEST: " + timer.WanderLeash);
					return;
				}
			}
			Console.WriteLine("WANDER_LEASH_TEST: none found");
		}

		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly TagKey spawnedCountTK = TagKey.Acquire("spawnedCount");
		private static readonly TimerKey spawnTimerTK = TimerKey.Acquire("SpawnTimer");

		/// <summary>Kills the nearest NPC carrying a spawnGem tag (i.e. one created by a spawn
		/// gem) via a deferred timer, same pattern/reason as LevelingTests.DeferredNpcKillTimer
		/// (#146) - CauseDeath can't run synchronously inside this chat command's own call
		/// stack. Used to investigate #206's "spawn didn't create a new monster after I killed
		/// the old one" report.</summary>
		[SteamFunction]
		public static void TestKillNearbySpawnedNpc(Character self) {
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				NPC npc = ch as NPC;
				if ((npc != null) && npc.HasTag(spawnGemTK)) {
					self.AddTimer(DeferredSpawnedNpcKillTimer.TimerKey, new DeferredSpawnedNpcKillTimer(npc)).DueInSeconds = 0;
					return;
				}
			}
			Console.WriteLine("SPAWN_KILL_TEST: no spawned NPC found");
		}

		/// <summary>Reports the spawn gem's own spawnedCount tag and whether it currently has a
		/// pending SpawnTimer scheduled - the two things e_spawned_char's "on=@destroy" handler
		/// is supposed to update after a spawned NPC dies (#206).</summary>
		[SteamFunction]
		public static void TestVerifySpawnGemState(Character self) {
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 20)) {
				if (item.Def.Altdefname != null &&
					item.Def.Altdefname.Equals("i_worldgem_bit", StringComparison.OrdinalIgnoreCase)) {
					object spawnedCount = item.GetTag(spawnedCountTK);
					bool hasTimer = item.HasTimer(spawnTimerTK);
					Console.WriteLine("GEM_STATE_TEST: spawnedCount=" + spawnedCount + " hasTimer=" + hasTimer);
					return;
				}
			}
			Console.WriteLine("GEM_STATE_TEST: no gem found");
		}

		/// <summary>Reports the nearby spawn gem's uid (so a test can double-click it) and its
		/// current SpawnInfo fields - used to verify the double-click-to-edit flow (#208).</summary>
		[SteamFunction]
		public static void TestReportSpawnGemUidAndInfo(Character self) {
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 20)) {
				if (item.Def.Altdefname != null &&
					item.Def.Altdefname.Equals("i_worldgem_bit", StringComparison.OrdinalIgnoreCase)) {
					SpawnInfo info = item.GetTag(TagKey.Acquire("spawnInfo")) as SpawnInfo;
					Console.WriteLine("GEM_INFO_TEST: uid=" + item.Uid + " amount=" + (info == null ? -1 : info.Amount) +
						" minTime=" + (info == null ? -1 : info.MinTime) + " maxTime=" + (info == null ? -1 : info.MaxTime) +
						" homedist=" + (info == null ? -1 : info.Homedist));
					return;
				}
			}
			Console.WriteLine("GEM_INFO_TEST: no gem found");
		}
	}

	/// <summary>See SpawnCommandTests.TestKillNearbySpawnedNpc's doc comment (#206) for why this
	/// exists - runs the kill outside the chat command's own call stack.</summary>
	[SaveableClass]
	public class DeferredSpawnedNpcKillTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_deferredSpawnedNpcKillTimer_");

		private NPC victim;

		[LoadingInitializer]
		public DeferredSpawnedNpcKillTimer() {
		}

		public DeferredSpawnedNpcKillTimer(NPC victim) {
			this.victim = victim;
		}

		protected override void OnTimeout(TagHolder cont) {
			this.victim.CauseDeath(null);
			Console.WriteLine("SPAWN_KILL_TEST: killed");
		}
	}
}
