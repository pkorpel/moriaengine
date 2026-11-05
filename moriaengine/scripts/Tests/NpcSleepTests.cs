using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1201 - NPCs sleep where no player is near. With the awake time cut to a few seconds: a wild NPC three areas
	/// away falls asleep, one beyond its leash is put home as it does; one beside the player, a pet following its
	/// owner, a follower and a NeverSleeps one keep thinking; a pet told to stay and a held NPC sleep where they stand. A sleeper summoned next to the player wakes, a player
	/// coming to a sleeper wakes it, and it falls asleep again once the player has been gone the awake time.
	/// </summary>
	public static class NpcSleepTests {
		public const double TestAwakeSeconds = 3;
		private const int Far = 3 * ActivityGrid.AreaSize; //three areas east: not a neighbour of the player's
		private static readonly Dictionary<string, NPC> npcs = new Dictionary<string, NPC>();
		private static Point4D start, homeOfE, spotOfB;
		private static double oldAwakeSeconds = -1;

		[SteamFunction]
		public static void TestNpcSleepSetup(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 24);
			if (oldAwakeSeconds < 0) {
				oldAwakeSeconds = ActivityGrid.AwakeSeconds;
			}
			ActivityGrid.AwakeSeconds = TestAwakeSeconds;
			Map map = self.GetMap();
			start = new Point4D(self.X, self.Y, self.Z, self.M);
			npcs.Clear();

			npcs["A"] = Spawn(map, self.X + 2, self.Y); //beside the player
			npcs["B"] = Spawn(map, self.X + Far, self.Y); //wild, far
			spotOfB = new Point4D(npcs["B"].X, npcs["B"].Y, npcs["B"].Z, self.M);
			NPC pet = Spawn(map, self.X + Far, self.Y + 4); //a pet following its owner
			pet.ControlMaster = self;
			pet.ShouldFollowOwner = true;
			npcs["C"] = pet;
			NPC follower = Spawn(map, self.X + Far, self.Y + 8); //a follower
			follower.FollowTarget = self;
			follower.ShouldFollowOwner = true;
			npcs["D"] = follower;
			NPC keeper = Spawn(map, self.X + Far, self.Y + 12);
			NPCAiTimer.GetFor(keeper).NeverSleeps = true;
			npcs["F"] = keeper;

			NPC stray = Spawn(map, self.X + Far + 20, self.Y + 16); //20 tiles from its home, beyond the leash
			NPCAiTimer strayAi = NPCAiTimer.GetFor(stray);
			homeOfE = new Point4D(self.X + Far, self.Y + 16, map.GetTileZ(self.X + Far, self.Y + 16), self.M);
			strayAi.homeX = homeOfE.X;
			strayAi.homeY = homeOfE.Y;
			strayAi.homeZ = (sbyte) homeOfE.Z;
			strayAi.homeM = homeOfE.M;
			strayAi.hasHome = true;
			npcs["E"] = stray;

			NPC stayer = Spawn(map, self.X + Far + 20, self.Y + 20); //a pet told to stay, 20 tiles from its old home
			stayer.ControlMaster = self;
			stayer.ShouldFollowOwner = false;
			NPCAiTimer stayerAi = NPCAiTimer.GetFor(stayer);
			stayerAi.homeX = homeOfE.X;
			stayerAi.homeY = homeOfE.Y + 4;
			stayerAi.homeZ = (sbyte) homeOfE.Z;
			stayerAi.homeM = homeOfE.M;
			stayerAi.hasHome = true;
			npcs["G"] = stayer;

			NPC held = Spawn(map, self.X + Far + 20, self.Y + 24); //a held wild NPC (a guard told "stop"), 20 tiles from home
			NPCAiTimer heldAi = NPCAiTimer.GetFor(held);
			heldAi.isHeld = true;
			heldAi.homeX = homeOfE.X;
			heldAi.homeY = homeOfE.Y + 8;
			heldAi.homeZ = (sbyte) homeOfE.Z;
			heldAi.homeM = homeOfE.M;
			heldAi.hasHome = true;
			npcs["H"] = held;

			Console.WriteLine("NPC_SLEEP_SETUP: player=" + start + " awakeSeconds=" + ActivityGrid.AwakeSeconds +
				" playerArea=" + (self.X >> ActivityGrid.AreaShift) + " farArea=" + ((self.X + Far) >> ActivityGrid.AreaShift));
		}

		private static NPC Spawn(Map map, int x, int y) {
			return (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(x, y, map.GetTileZ(x, y), map.M);
		}

		[SteamFunction]
		public static void TestNpcSleepReport(Character self, ScriptArgs sa) {
			string label = (sa == null) ? "" : sa.Args.Trim().Trim('"');
			foreach (KeyValuePair<string, NPC> pair in npcs) {
				NPC npc = pair.Value;
				NPCAiTimer ai = NPCAiTimer.GetFor(npc);
				Console.WriteLine("NPC_SLEEP: at=" + label + " npc=" + pair.Key + " sleeping=" + ai.IsSleeping + " paused=" + ai.IsPaused +
					" pos=" + npc.X + "," + npc.Y + " atHome=" + ((npc.X == ai.homeX) && (npc.Y == ai.homeY)) + " state=" + ai.LastState);
			}
			Console.WriteLine("NPC_SLEEP_REPORTED: " + label);
		}

		/// <summary>E (asleep) is summoned beside the player: its new area is awake, it wakes at once.</summary>
		[SteamFunction]
		public static void TestNpcSleepSummon(Character self) {
			NPC stray = npcs["E"];
			bool before = NPCAiTimer.GetFor(stray).IsSleeping;
			stray.Go(self.X - 2, self.Y);
			Console.WriteLine("NPC_SLEEP_SUMMON: before=" + before + " after=" + NPCAiTimer.GetFor(stray).IsSleeping);
		}

		/// <summary>The player goes to B (asleep): its area wakes, and B with it, at once.</summary>
		[SteamFunction]
		public static void TestNpcSleepVisit(Character self) {
			NPC wild = npcs["B"];
			bool before = NPCAiTimer.GetFor(wild).IsSleeping;
			self.Go(spotOfB.X - 2, spotOfB.Y);
			Console.WriteLine("NPC_SLEEP_VISIT: before=" + before + " after=" + NPCAiTimer.GetFor(wild).IsSleeping);
		}

		[SteamFunction]
		public static void TestNpcSleepGoBack(Character self) {
			self.Go(start.X, start.Y, start.Z);
			Console.WriteLine("NPC_SLEEP_BACK: " + self.X + "," + self.Y);
		}

		[SteamFunction]
		public static void TestNpcSleepCleanup(Character self) {
			foreach (NPC npc in npcs.Values) {
				if (!npc.IsDeleted) {
					npc.Delete();
				}
			}
			npcs.Clear();
			if (oldAwakeSeconds >= 0) {
				ActivityGrid.AwakeSeconds = oldAwakeSeconds;
				oldAwakeSeconds = -1;
			}
			Console.WriteLine("NPC_SLEEP_CLEANED");
		}
	}
}
