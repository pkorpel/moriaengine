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
			player.Realm = Realm.Gondor;
			GoldUtils.Grant(player, 5000);
			TravelSettings.instance.rideSeconds = 1; //the boat's count, short for the client (put back by the chain)
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
				TravelSettings.instance.rideSeconds = 15;
				player.Realm = Realm.Gondor;
				player.DeleteTimer(FerryRide.Key);
				foreach (FerryStop stop in Ferry.All) {
					(player.GetTag(KeyOf(stop)) as NPC)?.Delete();
					player.RemoveTag(KeyOf(stop));
				}
				Console.WriteLine("FERRY_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			NPC island = Ferryman(player, Ferry.Island), dolAmroth = Ferryman(player, "dol_amroth"), umbar = Ferryman(player, "umbar");
			FerryStop islandStop = Ferry.Get(Ferry.Island), dolAmrothStop = Ferry.Get("dol_amroth"), umbarStop = Ferry.Get("umbar");

			//who goes where: a harbour to the island, whoever asks; the island to one's own side's harbour only (an outcast's: the side
			//they came from); a stray ferryman far from any stop is no ferry
			player.Realm = Realm.Gondor;
			List<FerryStop> gondorFromIsland = Ferry.DestinationsFrom(Ferry.StopOf(island), player), gondorFromUmbar = Ferry.DestinationsFrom(Ferry.StopOf(umbar), player);
			player.Realm = Realm.Mordor;
			List<FerryStop> mordorFromIsland = Ferry.DestinationsFrom(Ferry.StopOf(island), player);
			player.Realm = Realm.MordorOutcast;
			List<FerryStop> outcastFromIsland = Ferry.DestinationsFrom(Ferry.StopOf(island), player);
			player.Realm = Realm.GondorOutcast;
			bool gondorOutcast = Ferry.DestinationsFrom(Ferry.StopOf(island), player).Contains(dolAmrothStop);
			player.Realm = Realm.Gondor;
			NPC stray = (NPC) ((NPCDef) ThingDef.GetByDefname(Ferry.FerrymanDef)).Create(player.X, player.Y + 200, player.Z, player.M);
			bool strayIsNone = Ferry.StopOf(stray) == null;
			stray.Delete();
			Report("theRoutes", (Ferry.StopOf(island) == islandStop) && (Ferry.StopOf(dolAmroth) == dolAmrothStop)
				&& (gondorFromIsland.Count == 1) && (gondorFromIsland[0] == dolAmrothStop) && (gondorFromUmbar.Count == 1) && (gondorFromUmbar[0] == islandStop)
				&& (mordorFromIsland.Count == 1) && (mordorFromIsland[0] == umbarStop) && (outcastFromIsland.Count == 1) && (outcastFromIsland[0] == umbarStop)
				&& gondorOutcast && strayIsNone);

			//a Gondor hero on the island isn't taken to Umbar; to Dol Amroth with a pet that follows, after the count
			Beside(player, island);
			long gold = player.Gold;
			bool notToTheEnemy = !Ferry.Take(player, island, umbarStop) && (player.Gold == gold);
			NPC pet = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(player.X + 1, player.Y, player.Z, player.M);
			pet.ControlMaster = player;
			bool booked = Ferry.Take(player, island, dolAmrothStop) && (player.Gold == gold - TravelSettings.instance.ferryPrice);
			FerryRide ride = player.GetTimer(FerryRide.Key) as FerryRide;
			bool waits = (ride != null) && (Point2D.GetSimpleDistance(player, island) <= 1); //not yet: the count first
			ride?.Complete(player);
			Point4D dolLanding = dolAmrothStop.Landing;
			bool there = (Point2D.GetSimpleDistance(player, dolLanding) <= 1) && (Point2D.GetSimpleDistance(pet, dolLanding) <= 1)
				&& ((player.X != dolAmroth.X) || (player.Y != dolAmroth.Y)); //beside the ferryman, not on his tile
			pet.Delete();
			Report("aRideWithItsFollowersAfterTheCount", notToTheEnemy && booked && waits && there);

			//strayed by the count's end: the boat sails without you, the fare stays paid
			Beside(player, dolAmroth);
			gold = player.Gold;
			Ferry.Take(player, dolAmroth, islandStop);
			ride = player.GetTimer(FerryRide.Key) as FerryRide;
			player.Go(new Point4D(dolAmroth.X + TravelSettings.instance.strayTiles + 3, dolAmroth.Y, dolAmroth.Z, dolAmroth.M));
			ride?.Complete(player);
			Report("strayAndTheBoatLeaves", (ride != null) && (player.Gold == gold - TravelSettings.instance.ferryPrice) && (Point2D.GetSimpleDistance(player, dolAmroth) > 1)
				&& !((FlaggedRegion) StaticRegion.GetByDefname("a_Ostrov_nestalosti")).ContainsPoint(new Point2D(player.X, player.Y)));

			//no ghosts; no fare, no ride
			Beside(player, dolAmroth);
			player.CauseDeath(null);
			GoldUtils.GrantToBank(player, 1000); //a ghost could pay from the bank: the refusal must be what stops them
			Beside(player, dolAmroth);
			gold = player.Gold;
			bool noGhosts = player.Flag_Dead && !Ferry.Take(player, dolAmroth, islandStop) && (player.Gold == gold);
			player.Resurrect();
			GoldUtils.Pay(player, (int) player.Gold, PaySource.Usual, "");
			bool broke = player.Gold == 0;
			bool stays = !Ferry.Take(player, dolAmroth, islandStop) && (player.GetTimer(FerryRide.Key) == null);
			Report("noGhostsNoFareNoRide", noGhosts && broke && stays);
		}
	}
}
