using System;
using System.Reflection;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1155/#1156 - server side of Tests/Protocol/StaleSrcTests.cs.</summary>
	public static class StaleSrcTests {
		private static NPC attacker, rabbit;
		private static int rabbitX, rabbitY, rabbitHits;
		private static bool moved, reported;
		//#1180 - the hit the client waits for must come after the setup's player logged out: the watch arms then
		private static Character leaver;
		private static bool armed;
		private static int armedX, armedY, armedHits;

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
						reported = false;
						leaver = self;
						armed = false;
						rabbit.AddTimer(TimerKey.Acquire("_staleSrcWatch_"), new StaleSrcWatchTimer()).DueInSeconds = 0.2;
						line = "fighting";
						break;
					}
				case "after": {
					line = "rabbitHurt=" + (rabbit.Hits < rabbitHits) + " knockedBack=" + moved;
					//#1156's other half (no swing reaches it since the damage number stopped using src, #1180): a src with no
					//client, as an NPC or a player who logged out, has no connection, and reading it doesn't throw
					ISrc previous = Globals.Src;
					MethodInfo setSrc = typeof(Globals).GetMethod("SetSrc", BindingFlags.NonPublic | BindingFlags.Static);
					bool noClientSrc;
					try {
						setSrc.Invoke(null, new object[] { rabbit });
						noClientSrc = Globals.SrcTcpConnection == null;
					} catch (Exception) {
						noClientSrc = false;
					} finally {
						setSrc.Invoke(null, new object[] { previous });
					}
					line += " noClientSrc=" + noClientSrc;
					attacker.Delete();
					rabbit.Delete();
					break;
				}
				case "timersrc":
					//#1502 - a timer armed right after this client's own command: its OnTimeout must see no Globals.Src (this
					//client sent the last packet), and a MessageTo line still reaches the client
					self.AddTimer(TimerKey.Acquire("_timerSrcProbe_"), new TimerSrcProbe()).DueInSeconds = 0.3;
					line = "armed";
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
			if (!armed) {
				if ((leaver != null) && (leaver.GameState == null)) { //logged out: from here on, a blow comes with nobody's packet
					armed = true;
					armedX = watched.X;
					armedY = watched.Y;
					armedHits = watched.Hits;
				}
			} else if (!reported && ((watched.X != armedX) || (watched.Y != armedY)) && (watched.Hits < armedHits)) {
				reported = true; //the client waits for this line instead of a fixed time; no client is logged in meanwhile
				Console.WriteLine("STALESRC_TEST: hit |");
			}
		}
	}

	/// <summary>#1502 - reports what Globals.Src is while a timer runs, and sends its holder a line with MessageTo.</summary>
	public class TimerSrcProbe : BoundTimer {
		protected override void OnTimeout(TagHolder cont) {
			Character ch = cont as Character;
			bool srcNull = Globals.Src == null;
			if ((ch != null) && !ch.IsDeleted) {
				ch.MessageTo(ch, "Casovac te vidi");
			}
			Console.WriteLine("STALESRC_TIMER: srcNull=" + srcNull);
			this.Delete();
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
