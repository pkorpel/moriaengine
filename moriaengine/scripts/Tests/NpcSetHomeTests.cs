using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1228 item 9 - .sethome on the server: the NPC and things a GM client targets (Tests/Protocol/NpcSetHomeTests.cs)
	/// are set out here, and the NPC's home is reported after each step.</summary>
	public static class NpcSetHomeTests {
		private static readonly TagKey ThingsTK = TagKey.Acquire("_sethometest_things_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("SETHOME_CHECK: " + name + "=" + ok);
		}

		/// <summary>A hare held still with its home where it stands, a chest, and a free spot.</summary>
		[SteamFunction]
		public static void TestSetHomeSetup(Character self) {
			Player player = (Player) self;
			VerificationTestUtils.CleanupNearbyNpcs(player, 20);
			NPC hare = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 1, player.Y + 2, player.Z, player.M);
			NPCAiTimer.Freeze(hare, 600);
			NPCAiTimer ai = NPCAiTimer.GetFor(hare);
			ai.homeX = hare.X;
			ai.homeY = hare.Y;
			ai.homeZ = (sbyte) hare.Z;
			ai.homeM = hare.M;
			ai.hasHome = true;
			Item chest = ((ItemDef) ThingDef.GetByDefname("i_chest_wooden_brass")).Create(player.X - 2, player.Y + 2, player.Z, player.M) as Item;
			int[] spot = WorldSpots.NearestStandable(player.GetMap(), player.X - 3, player.Y - 1, 2);
			player.SetTag(ThingsTK, hare.Uid + "," + chest.Uid);
			Console.WriteLine("SETHOME_SETUP: npc=" + hare.Uid + " chest=" + chest.Uid + " sx=" + spot[0] + " sy=" + spot[1] + " sz=" + spot[2] + " end");
		}

		/// <summary>The hare goes 2 tiles east (as a GM's .go would): its home stays where it was until .sethome moves it.</summary>
		[SteamFunction]
		public static void TestSetHomeMoveNpc(Character self) {
			string[] n = ((string) self.GetTag(ThingsTK)).Split(',');
			NPC hare = (NPC) Thing.UidGetThing(int.Parse(n[0]));
			NPCAiTimer ai = NPCAiTimer.GetFor(hare);
			int oldX = ai.homeX;
			hare.Go(hare.X + 2, hare.Y);
			Console.WriteLine("SETHOME_MOVED: x=" + hare.X + " y=" + hare.Y + " homeKept=" + (ai.homeX == oldX) + " end");
		}

		[SteamFunction]
		public static void TestSetHomeReport(Character self, ScriptArgs sa) {
			string[] n = ((string) self.GetTag(ThingsTK)).Split(',');
			NPC hare = (NPC) Thing.UidGetThing(int.Parse(n[0]));
			Item chest = (Item) Thing.UidGetThing(int.Parse(n[1]));
			NPCAiTimer ai = NPCAiTimer.GetFor(hare);
			string[] want = sa.Args.Trim('"').Split(' ');
			string step = want[0];
			bool ok;
			if (step == "npc") {
				ok = (ai.homeX == hare.X) && (ai.homeY == hare.Y) && (ai.homeZ == hare.Z);
			} else if (step == "chest") {
				ok = (ai.homeX == chest.X) && (ai.homeY == chest.Y) && (ai.homeZ == chest.Z);
			} else {
				ok = (ai.homeX == int.Parse(want[1])) && (ai.homeY == int.Parse(want[2])) && (ai.homeZ == int.Parse(want[3])) && (ai.homeM == hare.M);
			}
			Console.WriteLine("SETHOME_STATE: step=" + step + " home=" + ai.homeX + "," + ai.homeY + "," + ai.homeZ + "," + ai.homeM + " npc=" + hare.P() + " chest=" + chest.P());
			Report(step, ok && ai.hasHome);
			Console.WriteLine("SETHOME_DONE " + step);
		}
	}
}
