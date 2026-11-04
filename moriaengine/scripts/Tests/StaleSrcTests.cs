using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1155/#1156 - server side of Tests/Protocol/StaleSrcTests.cs.</summary>
	public static class StaleSrcTests {
		private static NPC attacker, rabbit;
		private static int rabbitX, rabbitY, rabbitHits;
		private static bool moved;

		private static NPC Rabbit(Character self, int dx) {
			NPC npc = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + dx, self.Y, self.Z, self.M);
			npc.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			npc.MaxHits = 30000;
			npc.Hits = 30000;
			return npc;
		}

		[SteamFunction]
		public static void TestStaleSrc(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			string line;
			switch (what) {
				case "knockback": {
						//#1155 - a blow that does no damage doesn't push; one that hurts does
						MovementSpeedTests.TestTeleportToOpenGround(self);
						VerificationTestUtils.CleanupNearbyNpcs(self, 12);
						NPC knocker = (NPC) ThingDef.GetByDefname("npc_test_knockback_attacker").Create(self.X, self.Y + 2, self.Z, self.M);
						knocker.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
						NPC shielded = Rabbit(self, 0);
						shielded.P(self.X, self.Y + 3, self.Z);
						shielded.Flag_Invulnerable = true;
						int x = shielded.X, y = shielded.Y;
						DamageManager.ProcessSwing(knocker, shielded);
						line = "noDamageNoPush=" + ((shielded.X == x) && (shielded.Y == y));
						shielded.Flag_Invulnerable = false;
						int hits = shielded.Hits;
						DamageManager.ProcessSwing(knocker, shielded);
						line += " hurt=" + (shielded.Hits < hits) + " pushed=" + ((shielded.X != x) || (shielded.Y != y));
						VerificationTestUtils.CleanupNearbyNpcs(self, 12);
						break;
					}
				case "setup": {
						//#1156 - a monster fights on by its own timers after the last player who sent a packet logged out
						MovementSpeedTests.TestTeleportToOpenGround(self);
						VerificationTestUtils.CleanupNearbyNpcs(self, 12);
						rabbit = Rabbit(self, 4);
						attacker = (NPC) ThingDef.GetByDefname("npc_test_knockback_attacker").Create(self.X + 5, self.Y, self.Z, self.M);
						NPCAiTimer.EnsureInstalled(attacker);
						NPCAiTimer.Attack(attacker, rabbit);
						rabbitX = rabbit.X;
						rabbitY = rabbit.Y;
						rabbitHits = rabbit.Hits;
						moved = false;
						rabbit.AddTimer(TimerKey.Acquire("_staleSrcWatch_"), new StaleSrcWatchTimer()).DueInSeconds = 0.2;
						line = "fighting";
						break;
					}
				case "after":
					line = "rabbitHurt=" + (rabbit.Hits < rabbitHits) + " knockedBack=" + moved;
					attacker.Delete();
					rabbit.Delete();
					break;
				default:
					line = "unknown";
					break;
			}
			Console.WriteLine("STALESRC_TEST: " + what + " " + line + " |");
		}

		internal static void Watch(NPC watched) {
			if ((watched.X != rabbitX) || (watched.Y != rabbitY)) {
				moved = true;
			}
		}
	}

	/// <summary>Notes whether the rabbit was ever knocked off its spot.</summary>
	[Persistence.SaveableClass, DeepCopyableClass]
	public class StaleSrcWatchTimer : BoundTimer {
		[Persistence.LoadingInitializer, DeepCopyImplementation]
		public StaleSrcWatchTimer() {
			this.PeriodInSeconds = 0.2;
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is NPC rabbit) && !rabbit.IsDeleted) {
				StaleSrcTests.Watch(rabbit);
			}
		}
	}
}
