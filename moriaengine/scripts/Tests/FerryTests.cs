using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1307 - the ferry to Ostrov nestalosti on the server: who goes where, the fare, a follower along, no fare no ride.
	/// Tests/Protocol/FerryTests.cs takes the boat from Dol Amroth through a client first.</summary>
	public static class FerryTests {
		private static readonly TagKey ferrymanTK = TagKey.Acquire("_ferrytest_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("FERRY_CHECK: " + name + "=" + ok);
		}

		private static TagKey KeyOf(FerryStop stop) {
			return TagKey.Acquire("_ferrytest_" + stop.Key + "_");
		}

		private static NPC Ferryman(Player player, string key) {
			return (NPC) player.GetTag(KeyOf(Ferry.Get(key)));
		}

		private static void Beside(Player player, NPC npc) {
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		/// <summary>A ferryman at each stop, the hero beside Dol Amroth's with gold, a plain player.</summary>
		[SteamFunction]
		public static void TestFerrySetup(Character self) {
			Player player = (Player) self;
			foreach (FerryStop stop in Ferry.All) {
				int[] spot = stop.Spot;
				if (spot == null) {
					throw new SEException("No spot for the ferryman of " + stop.Name + ".");
				}
				NPC ferryman = (NPC) ((NPCDef) ThingDef.GetByDefname(Ferry.FerrymanDef)).Create(spot[0], spot[1], spot[2], stop.M);
				NPCAiTimer.Freeze(ferryman, 900);
				player.SetTag(KeyOf(stop), ferryman);
			}
			player.Account.PLevel = 1; //GMs pay nothing
			GoldUtils.Grant(player, 5000);
			NPC from = Ferryman(player, "dol_amroth");
			Beside(player, from);
			Console.WriteLine("FERRY_SETUP: ferryman=" + from.Uid + " gold=" + player.Gold);
		}

		[SteamFunction]
		public static void TestFerryAfterClient(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Point4D landing = Ferry.Get(Ferry.Island).Landing;
			Report("theClientSailedToTheIsland", (player.M == landing.M) && (Point2D.GetSimpleDistance(player, landing) <= 1)
				&& (player.Gold == gold - TravelSettings.instance.ferryPrice) && ((FlaggedRegion) StaticRegion.GetByDefname("a_Ostrov_nestalosti")).ContainsPoint(new Point2D(player.X, player.Y)));
			Console.WriteLine("FERRY_CLIENT_DONE");
		}

		[SteamFunction]
		public static void TestFerryChain(Character self) {
			Player player = (Player) self;
			try {
				Chain(player);
			} finally {
				foreach (FerryStop stop in Ferry.All) {
					(player.GetTag(KeyOf(stop)) as NPC)?.Delete();
					player.RemoveTag(KeyOf(stop));
				}
				Console.WriteLine("FERRY_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			NPC island = Ferryman(player, Ferry.Island), dolAmroth = Ferryman(player, "dol_amroth"), umbar = Ferryman(player, "umbar");

			//who goes where: the island to either harbour, a harbour to the island only
			List<FerryStop> fromIsland = Ferry.DestinationsFrom(Ferry.StopOf(island)), fromHarbour = Ferry.DestinationsFrom(Ferry.StopOf(umbar));
			Report("theRoutes", (Ferry.StopOf(island).Key == Ferry.Island) && (Ferry.StopOf(dolAmroth).Key == "dol_amroth")
				&& (fromIsland.Count == 2) && fromIsland.Contains(Ferry.Get("umbar")) && fromIsland.Contains(Ferry.Get("dol_amroth"))
				&& (fromHarbour.Count == 1) && (fromHarbour[0].Key == Ferry.Island));

			//to Umbar with a pet that follows; not to the other harbour from a harbour
			Beside(player, island);
			NPC pet = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 1, player.Y, player.Z, player.M);
			pet.ControlMaster = player;
			long gold = player.Gold;
			bool rode = Ferry.Take(player, island, Ferry.Get("umbar"));
			Point4D umbarLanding = Ferry.Get("umbar").Landing;
			bool there = (Point2D.GetSimpleDistance(player, umbarLanding) <= 1) && (player.Gold == gold - TravelSettings.instance.ferryPrice)
				&& (Point2D.GetSimpleDistance(pet, umbarLanding) <= 1);
			Beside(player, umbar);
			gold = player.Gold;
			bool notToTheOtherHarbour = !Ferry.Take(player, umbar, Ferry.Get("dol_amroth")) && (player.Gold == gold);
			pet.Delete();
			Report("aRideWithItsFollowers", rode && there && notToTheOtherHarbour);

			//no fare, no ride
			GoldUtils.Pay(player, (int) player.Gold, PaySource.Usual, "");
			bool broke = player.Gold == 0;
			bool stays = !Ferry.Take(player, umbar, Ferry.Get(Ferry.Island)) && (Point2D.GetSimpleDistance(player, umbar) <= 1);
			Report("noFareNoRide", broke && stays);
		}
	}
}
