using System;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1100 - helpers for Tests/Protocol/GuardSightTests.cs' pursuit test.</summary>
	public static class GuardPursuitTests {
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static NPC guard;
		private static Item gem;

		private static NPCAiTimer QuietAi(NPC npc) {
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			ai.PeriodInSeconds = 3600; //the test moves it, not its own think-ticks
			ai.DueInSeconds = 3600;
			ai.hasHome = true;
			ai.homeX = npc.X;
			ai.homeY = npc.Y;
			ai.homeZ = (sbyte) npc.Z;
			ai.homeM = npc.M;
			return ai;
		}

		/// <summary>A Mordor guard 12 tiles off a Gondor player, in sight: it runs 8 steps at once, then stops short.</summary>
		[SteamFunction]
		public static void TestPursuitRun(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 30);
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
			Notoriety.ClearCriminal(self);
			guard = null;
			string tried = "";
			Map map = self.GetMap();
			for (int d = 0; (d < 8) && (guard == null); d++) {
				//walked out from the player by the real movement check, so the way back is open ground
				NPC candidate = (NPC) ThingDef.GetByDefname("c_vguard_mordor_hala_jezdec").Create(self.X, self.Y, self.Z, self.M);
				int x, y, z, steps = 0;
				while ((steps < 12) && map.CheckMovement(candidate, candidate.MovementSettings, (Direction) d, false, out x, out y, out z)) {
					candidate.Go(x, y, z, self.M);
					steps++;
				}
				DenyResult los = candidate.CanSeeLOS(self);
				tried += " d" + d + ":" + steps + "/" + los.Allow + "/" + Point2D.GetSimpleDistance(candidate, self) + "@" + candidate.X + "," + candidate.Y + "," + candidate.Z;
				if ((steps >= 9) && los.Allow) { //the open ground near the test spot is about 9 tiles wide
					guard = candidate;
				} else {
					candidate.Delete();
				}
			}
			if (guard == null) {
				Console.WriteLine("PURSUIT_RUN: noSpot from " + self.X + "," + self.Y + "," + self.Z + tried);
				return;
			}
			NPCAiTimer ai = QuietAi(guard);
			ai.EngageTarget(self);
			ai.Fight(guard, self);
			bool pursuing = GuardPursuit.IsPursuing(guard);
			guard.GetTimer(GuardPursuitTimer.Key).DueInSeconds = 3600;
			int before = Point2D.GetSimpleDistance(guard, self);
			int cap = NpcAggroSettings.instance.guardPursuitSteps;
			NpcAggroSettings.instance.guardPursuitSteps = 4; //a cap below the distance, as the open ground isn't wide enough for 8
			GuardPursuit.Pursue(guard, ai, self);
			NpcAggroSettings.instance.guardPursuitSteps = cap;
			int afterRun = Point2D.GetSimpleDistance(guard, self);
			GuardPursuit.Pursue(guard, ai, self);
			int afterSecond = Point2D.GetSimpleDistance(guard, self);
			Console.WriteLine("PURSUIT_RUN: guard=" + GuardPursuit.IsGuard(guard) + " pursuing=" + pursuing + " before=" + before + " afterRun=" + afterRun + " afterSecond=" + afterSecond + " cap=" + cap);
		}

		/// <summary>A spawn gem 75 tiles away: the guard teleports home instead of running.</summary>
		[SteamFunction]
		public static void TestPursuitFarFromSpawn(Character self) {
			NPCAiTimer ai = NPCAiTimer.GetFor(guard);
			ai.homeX = guard.X + 3; //home is where it teleports to
			ai.homeY = guard.Y;
			gem = (Item) ThingDef.GetByDefname("i_worldgem_bit").Create((ushort) (guard.X + 75), guard.Y, guard.Z, guard.M);
			guard.SetTag(spawnGemTK, gem);
			int fromSpawn = GuardPursuit.DistanceFromSpawn(guard, ai);
			GuardPursuit.Pursue(guard, ai, self);
			bool home = (guard.X == ai.homeX) && (guard.Y == ai.homeY);
			gem.Delete();
			guard.RemoveTag(spawnGemTK);
			ai.homeX = guard.X;
			ai.homeY = guard.Y;
			ai.Disengage(); //the fight is over: the pursuit leaves at its next second
			guard.GetTimer(GuardPursuitTimer.Key).DueInSeconds = GuardPursuitTimer.Seconds;
			Console.WriteLine("PURSUIT_FAR: fromSpawn=" + fromSpawn + " wentHome=" + home);
		}

		/// <summary>No fight left: the pursuit is gone, and the guard teleports home from beyond 7 tiles, not from 5.</summary>
		[SteamFunction]
		public static void TestPursuitHome(Character self) {
			NPCAiTimer ai = NPCAiTimer.GetFor(guard);
			bool ended = !GuardPursuit.IsPursuing(guard);
			int homeX = ai.homeX;
			guard.Go(homeX + 5, ai.homeY, ai.homeZ, ai.homeM);
			bool nearStays = !GuardPursuit.TeleportsHome(guard, ai) && (guard.X == homeX + 5);
			guard.Go(homeX + 10, ai.homeY, ai.homeZ, ai.homeM);
			bool farGoes = GuardPursuit.TeleportsHome(guard, ai) && (guard.X == homeX);
			Console.WriteLine("PURSUIT_HOME: ended=" + ended + " nearStays=" + nearStays + " farGoes=" + farGoes);
		}

		/// <summary>e_guard's @skillstroke: a guard fighting an NPC spawned in a town drops it and goes home.</summary>
		[SteamFunction]
		public static void TestPursuitTownNpc(Character self) {
			NPCAiTimer ai = NPCAiTimer.GetFor(guard);
			gem = null;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				if ((region is FlaggedRegion area) && !string.IsNullOrEmpty(area.City) && (area.P.M == guard.M)) {
					Item candidate = (Item) ThingDef.GetByDefname("i_worldgem_bit").Create(area.P);
					FlaggedRegion at = RegionEventSystem.AreaOf(candidate.Region);
					if ((at != null) && !string.IsNullOrEmpty(at.City)) {
						gem = candidate;
						break;
					}
					candidate.Delete();
				}
			}
			int homeX = ai.homeX;
			guard.Go(homeX + 4, ai.homeY, ai.homeZ, ai.homeM);
			NPC townsman = (NPC) ThingDef.GetByDefname("c_wolf").Create(guard.X + 1, guard.Y, guard.Z, guard.M);
			townsman.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			ai.EngageTarget(townsman);
			ai.Fight(guard, townsman);
			bool wildFought = ai.Target == townsman;
			townsman.SetTag(spawnGemTK, gem);
			ai.Fight(guard, townsman);
			bool dropped = (ai.Target == null) && (guard.X == homeX);
			townsman.Delete();
			if (gem != null) {
				gem.Delete();
			}
			guard.Delete();
			self.Realm = Realm.None;
			self.Account.PLevel = 7;
			Console.WriteLine("PURSUIT_TOWN: townGem=" + (gem != null) + " wildFought=" + wildFought + " dropped=" + dropped);
		}
	}
}
