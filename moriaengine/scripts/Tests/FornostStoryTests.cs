using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - Fornost's stories on the server: the Rangers' watch (a client takes it, reads the tracks and reports the way,
	/// Tests/Protocol/FornostStoryTests.cs), then the barrow at night and the watch's rules here.</summary>
	public static class FornostStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("FORNOST_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_fornosttest_" + key + "_");
		}

		private static CityVenue OpenAir(string city, string type, int[] spot, byte m) {
			VenueDraft draft = new VenueDraft();
			draft.SetType(VenueType.Get(type).IndexOf());
			draft.M = m;
			draft.City = city;
			draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
			CityVenue venue = draft.Save(out string problem);
			if (venue == null) {
				throw new SEException(city + "'s " + type + " wasn't saved: " + problem);
			}
			Residents.FillRoles(venue);
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
			}
			return venue;
		}

		private static List<int[]> Spots(Point4D p, int count) {
			List<int[]> spots = new List<int[]>();
			foreach (int[] tile in WorldEvents.Reachable(Map.GetMap(p.M), new[] { p.X, p.Y, p.Z }, 25)) {
				if ((WorldSpots.Distance(tile, p.X, p.Y) >= 3) && spots.TrueForAll(o => WorldSpots.Distance(tile, o[0], o[1]) >= 6)) {
					spots.Add(tile);
				}
			}
			if (spots.Count < count) {
				throw new SEException("Only " + spots.Count + " spots for test venues near " + p + ".");
			}
			return spots;
		}

		private static Item Gem(Player player, string key, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(key));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if ((role == null) || (Residents.RoleKeyOf(gem) == role)) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>A barracks and a tavern in Fornost, a barracks in Edoras; only the Rangers' watch offered; the hero (Tracking 50.0)
		/// beside a guard.</summary>
		[SteamFunction]
		public static void TestFornostSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D fornost = WorldEvents.CentreOf("Fornost"), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> f = Spots(fornost, 2);
				player.SetTag(VenueKey("barracks"), OpenAir("Fornost", "barracks", f[0], fornost.M));
				player.SetTag(VenueKey("tavern"), OpenAir("Fornost", "tavern", f[1], fornost.M));
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "barracks", Spots(edoras, 1)[0], edoras.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new RangerWatchErrand());
				player.RemoveTag(Errands.LogTK);
				player.SetRealSkillValue((int) SkillName.Tracking, 500);
				NPC guard = Residents.BodyOf(Gem(player, "barracks", null));
				player.Go(new Point4D(guard.X, guard.Y, guard.Z, guard.M));
				Console.WriteLine("FORNOST_SETUP: guard=" + guard.Uid + " gold=" + player.Gold);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the next unread track; its uid, the errand's id and the right answer (0s if none).</summary>
		[SteamFunction]
		public static void TestFornostBesideTrack(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "rangerwatch");
			Thing track = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => !t.IsDeleted);
			if (track != null) {
				Beside(player, track);
			}
			Console.WriteLine("FORNOST_TRACK: uid=" + ((track == null) ? 0 : track.Uid) + " errand=" + ((errand == null) ? 0 : errand.id)
				+ " right=" + ((errand == null) ? 0 : errand.steps[errand.steps.Count - 1].count));
		}

		[SteamFunction]
		public static void TestFornostBesideGuard(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "rangerwatch");
			NPC guard = Residents.BodyOf(Gem(player, "barracks", null));
			player.Go(new Point4D(guard.X, guard.Y, guard.Z, guard.M));
			Report("theTracksAreRead", (errand != null) && (errand.index == 1));
			Console.WriteLine("FORNOST_GUARD: uid=" + guard.Uid);
		}

		[SteamFunction]
		public static void TestFornostState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theRightWayPays", !Errands.Active(player).Exists(e => e.key == "rangerwatch") && (player.Gold > gold));
			Console.WriteLine("FORNOST_STATE_DONE");
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city) {
			Errand errand = new Errand { key = template.Key, name = template.Name, giverId = Residents.IdOf(giver), city = city, tier = (int) template.Tier };
			return template.TryBuild(new ErrandContext(player, giver, city), errand) ? errand : null;
		}

		private static void Fresh(Player player) {
			ErrandLog log = Errands.Log(player);
			foreach (Errand left in new List<Errand>(log.active)) {
				Errands.GiveUp(player, left);
			}
			log.day = -1;
			log.lastKey = "";
			log.streak = 0;
		}

		private static void Beside(Player player, Thing thing) {
			if (thing is NPC npc) {
				NPCAiTimer.Freeze(npc, 60);
			}
			player.Go(new Point4D(thing.X, thing.Y, thing.Z, thing.M));
		}

		private static bool Talk(Player player, Errand errand, int choice = -1) {
			Item gem = (errand.Current == null) ? null : Residents.ById(errand.Current.residentId);
			if (gem == null) {
				return false; //a step without a resident (things): nobody to talk to
			}
			NPC body = Residents.BodyOf(gem);
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
			return Errands.Do(player, errand, body, choice);
		}

		[SteamFunction]
		public static void TestFornostChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				Chain(player);
			} finally {
				account.PLevel = plevel;
				VenueClock.Override = null;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("FORNOST_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new BarrowErrand());
			Item guard = Gem(player, "barracks", null), innkeeper = Gem(player, "tavern", "innkeeper"), edorasGuard = Gem(player, "edoras", null);
			Report("onlyFornostsRangers", (Build(player, new BarrowErrand(), edorasGuard, "Edoras") == null) && (Build(player, new RangerWatchErrand(), edorasGuard, "Edoras") == null)
				&& (Build(player, new BarrowErrand(), innkeeper, "Fornost") == null) && (Build(player, new RangerWatchErrand(), innkeeper, "Fornost") == null)
				&& (Build(player, new BarrowErrand(), guard, "Fornost") != null));

			//the barrow: only at night; the wight laid, the blade taken, brought back
			Fresh(player);
			Errand barrow = Build(player, new BarrowErrand(), guard, "Fornost");
			bool done = false, notByDay = false;
			if ((barrow != null) && Errands.Accept(player, barrow)) {
				VenueClock.Override = 12;
				notByDay = !Talk(player, barrow);
				VenueClock.Override = 2; //past midnight: the night runs 22 to 4
				bool night = Talk(player, barrow) && (barrow.index == 1);
				VenueClock.Override = null;
				List<Thing> wight = Errands.ThingsOf(player, barrow);
				bool laid = night && (wight.Count == 1) && (wight[0] is NPC);
				if (laid) {
					((NPC) wight[0]).CauseDeath(player);
					laid = barrow.index == 2;
				}
				List<Thing> blade = Errands.ThingsOf(player, barrow);
				bool taken = laid && (blade.Count == 1) && (blade[0] is Item);
				if (taken) {
					Beside(player, blade[0]);
					taken = Errands.UseThing(blade[0], player) && (barrow.index == 3);
				}
				done = taken && Talk(player, barrow) && !Errands.Active(player).Contains(barrow);
			}
			Report("theBarrowWakesAtNightAndIsLaid", done && notByDay);

			//the watch: not offered without Tracking; no Tracking, no reading (the track stays); a wrong way pays nothing
			Fresh(player);
			player.SetRealSkillValue((int) SkillName.Tracking, 100);
			bool notOffered = Build(player, new RangerWatchErrand(), guard, "Fornost") == null;
			player.SetRealSkillValue((int) SkillName.Tracking, 500);
			Errand watch = Build(player, new RangerWatchErrand(), guard, "Fornost");
			bool unread = false, wrongPaysNothing = false;
			if ((watch != null) && Errands.Accept(player, watch)) {
				player.SetRealSkillValue((int) SkillName.Tracking, 100);
				List<Thing> tracks = Errands.ThingsOf(player, watch);
				Beside(player, tracks[0]);
				Errands.UseThing(tracks[0], player); //refused by the story: the track stays, nothing counted
				unread = !tracks[0].IsDeleted && (watch.index == 0) && (Errands.ThingsOf(player, watch).Count == RangerWatchErrand.Tracks);
				player.SetRealSkillValue((int) SkillName.Tracking, 500);
				foreach (Thing track in Errands.ThingsOf(player, watch)) {
					Beside(player, track);
					Errands.UseThing(track, player);
				}
				int wrong = (watch.steps[1].count + 1) % RangerWatchErrand.Ways.Length;
				long gold = player.Gold;
				wrongPaysNothing = (watch.index == 1) && Talk(player, watch, wrong) && !Errands.Active(player).Contains(watch) && (player.Gold == gold);
			}
			Report("readingNeedsTracking", unread && notOffered);
			Report("theWrongWayPaysNothing", wrongPaysNothing);
		}
	}
}
