using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1307 - world events survive a real save and restart (the user's "save everything"): a troll at the ford and a
	/// festival in Minas Tirith keep their ids, NPCs, heroes, guests and rounds; a city's price outcome, the realm's experience bonus,
	/// a landmark and the criers' news stay; nothing restored is swept as a past run's leftover. Tests/Protocol/WorldEventsRestartTests.cs
	/// boots twice.</summary>
	public static class WorldEventsRestartTests {
		private const string News = "Ve meste se mluvi o zkousce.";

		//two records the same, field by field
		private static bool Same(WorldEventRecord a, WorldEventRecord b) {
			return (a.kind == b.kind) && (a.city == b.city) && (a.id == b.id) && (a.centreX == b.centreX) && (a.centreY == b.centreY) && (a.startedAt == b.startedAt)
				&& (a.endsAt == b.endsAt) && (a.ended == b.ended) && (a.won == b.won) && Seq(a.npcs, b.npcs) && Seq(a.items, b.items) && Seq(a.heroes, b.heroes)
				&& Seq(a.heroKills, b.heroKills) && Seq(a.intKeys, b.intKeys) && Seq(a.intValues, b.intValues) && Seq(a.doubleKeys, b.doubleKeys)
				&& Seq(a.doubleValues, b.doubleValues) && Seq(a.stringKeys, b.stringKeys) && Seq(a.stringValues, b.stringValues) && Seq(a.thingKeys, b.thingKeys)
				&& Seq(a.thingValues, b.thingValues) && Seq(a.numberKeys, b.numberKeys) && Seq(a.numberValues, b.numberValues);
		}

		private static bool Seq<T>(List<T> a, List<T> b) {
			if (a.Count != b.Count) {
				return false;
			}
			for (int i = 0; i < a.Count; i++) {
				if (!Equals(a[i], b[i])) {
					return false;
				}
			}
			return true;
		}

		/// <summary>Each kind's own state written to a record and read back gives the same record: a caravan underway, a raid, the
		/// beacons partly lit, a fire with a helper (the troll and the festival go through the real restart).</summary>
		[SteamFunction]
		public static void TestWorldEventsRecordRoundTrip(Character self) {
			Player player = (Player) self;
			WorldEvents.ResetForTests();
			List<string> results = new List<string>();
			try {
				VenueClock.Override = 18; //dusk: the raid's time
				Caravan caravan = WorldEvents.Start(WorldEvents.KindOf("karavana"), "Hobitin") as Caravan;
				if (caravan != null) {
					caravan.next = 3;
					caravan.Ambushed = true;
					caravan.Escorts.Add(player);
				}
				WorldEvent raid = WorldEvents.Start(WorldEvents.KindOf("najezd"), "Edoras");
				if (raid != null) {
					raid.Heroes[player] = 1;
				}
				Beacons beacons = WorldEvents.Start(WorldEvents.KindOf("majaky"), "Minas_Tirith") as Beacons;
				if (beacons != null) {
					beacons.Lit = 2;
					beacons.Lighters.Add(player);
				}
				Fire fire = WorldEvents.Start(WorldEvents.KindOf("pozar"), "Dol_Amroth") as Fire;
				if (fire != null) {
					fire.Helpers[player] = 3;
				}
				foreach (WorldEvent e in new WorldEvent[] { caravan, raid, beacons, fire }) {
					if (e == null) {
						results.Add("none");
						continue;
					}
					WorldEventRecord before = WorldEventRecord.Of(e);
					WorldEvent again = before.Restore();
					results.Add(e.Kind.Key + "=" + ((again != null) && (again.GetType() == e.GetType()) && Same(before, WorldEventRecord.Of(again)) && ((e is Warband) || (before.thingKeys.Count + before.intKeys.Count > 0)))); //a raid has no state of its own
				}
			} finally {
				VenueClock.Override = null;
				WorldEvents.ResetForTests();
			}
			Console.WriteLine("WEVR_ROUNDTRIP: " + string.Join(" ", results) + " end");
		}

		[SteamFunction]
		public static void TestWorldEventsRestartPrepare(Character self) {
			Player player = (Player) self;
			WorldEvents.ResetForTests();
			CityNews.Clear();
			player.Account.PLevel = 1;
			player.Realm = Realm.Gondor;
			VenueClock.Override = 23; //night: the troll's time
			Troll troll = WorldEvents.Start(WorldEvents.KindOf("trol"), "Edoras") as Troll;
			VenueClock.Override = null;
			if (troll != null) {
				troll.Heroes[player] = 2;
				troll.sawNight = false; //boot 2's real clock may read morning: a troll that saw no night doesn't turn to stone then
			}
			Festival feast = WorldEvents.Start(WorldEvents.KindOf("slunovrat"), "Minas_Tirith") as Festival;
			if (feast != null) {
				feast.Guests.Add(player);
				feast.Catches[player] = 1;
				feast.Rounds = 2;
			}
			WorldEvents.SetOutcome("Hobitin", -10, 120, "Hobitin slavi dobrou urodu.");
			WorldEvents.SetExperienceBonus(Realm.Gondor, 10, 60);
			CityNews.Add("Edoras", News);
			Point4D edoras = WorldEvents.CentreOf("Edoras");
			Item landmark = (Item) ((ItemDef) ThingDef.GetByDefname("i_bread_loaf")).Create(edoras.X + 2, edoras.Y + 2, edoras.Z, edoras.M);
			WorldEvents.AddLandmark(landmark, 60);
			Console.WriteLine("WEVR_READY: troll=" + ((troll == null) ? "none" : troll.Id) + " beast=" + ((troll == null) ? 0 : troll.Beast.Uid)
				+ " feast=" + ((feast == null) ? "none" : feast.Id) + " host=" + ((feast == null) ? 0 : feast.Host.Uid) + " animal=" + ((feast?.Animal == null) ? 0 : feast.Animal.Uid)
				+ " price=" + WorldEvents.PricePercent("Hobitin") + " landmark=" + landmark.Uid + " serial=" + WorldEvents.serial + " end");
		}

		[SteamFunction]
		public static void TestWorldEventsRestartCheck(Character self) {
			Troll troll = null;
			Festival feast = null;
			foreach (WorldEvent e in WorldEvents.Running) {
				troll = troll ?? (e as Troll);
				feast = feast ?? (e as Festival);
			}
			int swept = WorldEvents.ClearLeftovers(); //nothing restored is a past run's leftover
			string guest = "none";
			int catches = -1;
			if (feast != null) {
				foreach (Player p in feast.Guests) {
					guest = p.Name.Replace(' ', '_');
				}
				foreach (KeyValuePair<Player, int> c in feast.Catches) {
					catches = c.Value;
				}
			}
			int heroKills = -1;
			if (troll != null) {
				foreach (KeyValuePair<Player, int> h in troll.Heroes) {
					heroKills = h.Value;
				}
			}
			bool bonus = WorldEvents.experienceBonus.TryGetValue(Realm.Gondor, out KeyValuePair<int, double> b) && (b.Key == 10) && (b.Value > Globals.TimeInSeconds);
			Thing landmark = WorldEvents.landmarks.Count > 0 ? WorldEvents.landmarks[0] : null;
			bool ticks = true;
			try {
				WorldEvents.Tick(); //the restored events move on
			} catch (Exception) {
				ticks = false;
			}
			Console.WriteLine("WEVR_CHECK: running=" + WorldEvents.Running.Count + " troll=" + ((troll == null) ? "none" : troll.Id)
				+ " beast=" + (((troll != null) && (troll.Beast != null) && !troll.Beast.IsDeleted) ? troll.Beast.Uid : 0) + " heroKills=" + heroKills
				+ " feast=" + ((feast == null) ? "none" : feast.Id) + " host=" + (((feast != null) && (feast.Host != null) && !feast.Host.IsDeleted) ? feast.Host.Uid : 0)
				+ " animal=" + (((feast?.Animal != null) && !feast.Animal.IsDeleted) ? feast.Animal.Uid : 0) + " guest=" + guest + " catches=" + catches
				+ " rounds=" + ((feast == null) ? -1 : feast.Rounds) + " price=" + WorldEvents.PricePercent("Hobitin") + " bonus=" + bonus
				+ " news=" + CityNews.Of("Edoras").Contains(News) + " landmark=" + (((landmark != null) && !landmark.IsDeleted) ? landmark.Uid : 0)
				+ " swept=" + swept + " serial=" + WorldEvents.serial + " timer=" + Globals.Instance.HasTimer(WorldEvents.TimerKeyForTests) + " ticks=" + ticks + " end");
		}
	}
}
