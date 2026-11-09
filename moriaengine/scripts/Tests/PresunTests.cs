using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1331 - .presun on the server: what a GM client moves (Tests/Protocol/PresunTests.cs) is set out here, and where it
	/// ended up is reported.</summary>
	public static class PresunTests {
		private static readonly TagKey ThingsTK = TagKey.Acquire("_presuntest_things_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("PRESUN_CHECK: " + name + "=" + ok);
		}

		private static Item Make(string def, Thing where, int dx, int dy) {
			ItemDef itemDef = (ItemDef) ThingDef.GetByDefname(def);
			return (where is Container cont) ? (Item) itemDef.Create(cont) : (Item) itemDef.Create(where.X + dx, where.Y + dy, where.Z, where.M);
		}

		/// <summary>Bones on the ground, a torch in the GM's pack, a rock in a chest, a hare held still; two free spots.</summary>
		[SteamFunction]
		public static void TestPresunSetup(Character self) {
			Player player = (Player) self;
			VerificationTestUtils.CleanupNearbyNpcs(player, 20);
			Item bones = Make("i_bones", player, 2, 0), torch = Make("i_torch", player.Backpack, 0, 0), chest = Make("i_chest_wooden_brass", player, -2, 2);
			Item rock = Make("i_rock_plain", chest, 0, 0);
			NPC hare = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 1, player.Y + 2, player.Z, player.M);
			NPCAiTimer.Freeze(hare, 300);
			Map map = player.GetMap();
			int[] spot = WorldSpots.NearestStandable(map, player.X - 3, player.Y - 1, 2), spot2 = WorldSpots.NearestStandable(map, player.X - 3, player.Y + 1, 2);
			player.SetTag(ThingsTK, bones.Uid + "," + torch.Uid + "," + chest.Uid + "," + rock.Uid + "," + hare.Uid + "," + spot[0] + "," + spot[1] + "," + spot[2] + ","
				+ spot2[0] + "," + spot2[1] + "," + spot2[2]);
			Console.WriteLine("PRESUN_SETUP: bones=" + bones.Uid + " torch=" + torch.Uid + " pack=" + player.Backpack.Uid + " chest=" + chest.Uid + " rock=" + rock.Uid + " hare=" + hare.Uid
				+ " sx=" + spot[0] + " sy=" + spot[1] + " sz=" + spot[2] + " tx=" + spot2[0] + " ty=" + spot2[1] + " tz=" + spot2[2] + " end");
		}

		private static bool At(Thing thing, int x, int y) {
			return (thing != null) && !thing.IsDeleted && (thing.TopObj() == thing) && (thing.X == x) && (thing.Y == y);
		}

		/// <summary>After the client's four moves: the torch went from the pack to the first spot, the hare onto the chest (the rock
		/// inside it stands for the chest's place), and the bones, after a first target on the ground was asked again, to the
		/// second spot.</summary>
		[SteamFunction]
		public static void TestPresunState(Character self) {
			Player player = (Player) self;
			string[] parts = ((string) player.GetTag(ThingsTK)).Split(',');
			int[] n = Array.ConvertAll(parts, int.Parse);
			Thing bones = Thing.UidGetThing(n[0]), torch = Thing.UidGetThing(n[1]), chest = Thing.UidGetThing(n[2]), rock = Thing.UidGetThing(n[3]),
				hare = Thing.UidGetThing(n[4]);
			Console.WriteLine("PRESUN_STATE: bones=" + ((bones == null) ? "gone" : bones.TopObj().P().ToString()) + " torch=" + ((torch == null) ? "gone" : torch.TopObj().P() + " inPack=" + (torch.TopObj() == player))
				+ " hare=" + ((hare == null) ? "gone" : hare.P().ToString()) + " chest=" + ((chest == null) ? "gone" : chest.P().ToString()) + " rockInChest=" + ((rock != null) && (rock.TopObj() == chest)));
			Report("anItemLeavesThePack", At(torch, n[5], n[6]));
			Report("aCreatureGoesToAThingsPlace", (hare != null) && (chest != null) && (hare.X == chest.X) && (hare.Y == chest.Y) && (rock.TopObj() == chest));
			Report("theGroundFirstAsksAgain", At(bones, n[8], n[9]));
			Console.WriteLine("PRESUN_DONE");
		}
	}
}
