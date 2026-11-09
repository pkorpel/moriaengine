using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - Edoras's stories on the server: the eored's muster (a client takes it, rides to the banners and reports,
	/// Tests/Protocol/EdorasStoryTests.cs), then the muster on foot and the song for the hall here.</summary>
	public static class EdorasStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("EDORAS_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_edorastest_" + key + "_");
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

		/// <summary>A stable and a tavern in Edoras, a stable in Hobitin; only the muster offered; the hero on a horse beside a
		/// stable hand.</summary>
		[SteamFunction]
		public static void TestEdorasSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D edoras = WorldEvents.CentreOf("Edoras"), hobitin = WorldEvents.CentreOf("Hobitin");
				List<int[]> e = Spots(edoras, 2);
				player.SetTag(VenueKey("stable"), OpenAir("Edoras", "stable", e[0], edoras.M));
				player.SetTag(VenueKey("tavern"), OpenAir("Edoras", "tavern", e[1], edoras.M));
				List<int[]> h = Spots(hobitin, 2);
				player.SetTag(VenueKey("hobitin"), OpenAir("Hobitin", "stable", h[0], hobitin.M));
				player.SetTag(VenueKey("hobitintavern"), OpenAir("Hobitin", "tavern", h[1], hobitin.M)); //a hall elsewhere: only the city keeps the song out
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new EoredMusterErrand());
				player.RemoveTag(Errands.LogTK);
				NPC hand = Residents.BodyOf(Gem(player, "stable", null));
				player.Go(new Point4D(hand.X, hand.Y, hand.Z, hand.M));
				Character horse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(player.P());
				player.Mount = horse;
				Console.WriteLine("EDORAS_SETUP: hand=" + hand.Uid + " gold=" + player.Gold + " riding=" + player.Flag_Riding);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero (still riding) beside the next banner; its uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestEdorasBesideBanner(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "eored");
			Thing banner = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => !t.IsDeleted);
			if (banner != null) {
				Beside(player, banner);
			}
			Console.WriteLine("EDORAS_BANNER: uid=" + ((banner == null) ? 0 : banner.Uid) + " errand=" + ((errand == null) ? 0 : errand.id) + " riding=" + player.Flag_Riding);
		}

		[SteamFunction]
		public static void TestEdorasBesideHand(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "eored");
			NPC hand = Residents.BodyOf(Gem(player, "stable", null));
			player.Go(new Point4D(hand.X, hand.Y, hand.Z, hand.M));
			Report("theBannersAreRidden", (errand != null) && (errand.index == 1));
			Console.WriteLine("EDORAS_HAND: uid=" + hand.Uid);
		}

		[SteamFunction]
		public static void TestEdorasState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theMusterIsReported", !Errands.Active(player).Exists(e => e.key == "eored") && (player.Gold > gold));
			Console.WriteLine("EDORAS_STATE_DONE");
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
		public static void TestEdorasChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				Chain(player);
			} finally {
				account.PLevel = plevel;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("EDORAS_CHAIN_DONE");
			}
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new HallSongErrand());
			Item hand = Gem(player, "stable", null), innkeeper = Gem(player, "tavern", "innkeeper"), hobitinHand = Gem(player, "hobitin", null);
			player.SetRealSkillValue((int) SkillName.Musicianship, 1000);
			Report("onlyEdoras", (Build(player, new EoredMusterErrand(), hobitinHand, "Hobitin") == null) && (Build(player, new HallSongErrand(), Gem(player, "hobitintavern", "innkeeper"), "Hobitin") == null)
				&& (Build(player, new EoredMusterErrand(), innkeeper, "Edoras") == null) && (Build(player, new HallSongErrand(), hand, "Edoras") == null)
				&& (Build(player, new EoredMusterErrand(), hand, "Edoras") != null) && (Build(player, new HallSongErrand(), innkeeper, "Edoras") != null));

			//the muster: on foot a banner doesn't count
			Fresh(player);
			Errand muster = Build(player, new EoredMusterErrand(), hand, "Edoras");
			bool onFoot = false;
			if ((muster != null) && Errands.Accept(player, muster)) {
				player.Dismount();
				List<Thing> banners = Errands.ThingsOf(player, muster);
				Beside(player, banners[0]);
				Errands.UseThing(banners[0], player);
				onFoot = !player.Flag_Riding && !banners[0].IsDeleted && (Errands.ThingsOf(player, muster).Count == EoredMusterErrand.Banners) && (muster.index == 0)
					&& Math.Abs((muster.Expires - Errands.Now).TotalMinutes - 20) < 1;
			}
			Report("onFootABannerDoesntCount", onFoot);

			//the song: not asked of one who can't play; the verse learned; no instrument, no song; then played in the hall
			Fresh(player);
			player.SetRealSkillValue((int) SkillName.Musicianship, 100);
			bool notAsked = Build(player, new HallSongErrand(), innkeeper, "Edoras") == null;
			player.SetRealSkillValue((int) SkillName.Musicianship, 1000);
			Errand song = Build(player, new HallSongErrand(), innkeeper, "Edoras");
			bool sung = false, noInstrument = false, rollMatters = false, pausedAfterATry = false;
			if ((song != null) && Errands.Accept(player, song)) {
				bool verse = Talk(player, song) && (song.index == 1);
				List<Thing> stool = Errands.ThingsOf(player, song);
				CityVenue hall = Residents.WorkplaceOf(innkeeper);
				bool inHall = (stool.Count == 1) && hall.Contains(stool[0].X, stool[0].Y, stool[0].Z);
				Beside(player, stool[0]);
				Errands.UseThing(stool[0], player);
				noInstrument = verse && inHall && !stool[0].IsDeleted && (song.index == 1);
				Item lute = (Item) ((ItemDef) ThingDef.GetByDefname("i_lute")).Create(player.Backpack);
				player.SetRealSkillValue((int) SkillName.Musicianship, 0);
				Errands.UseThing(stool[0], player); //the roll at 0 fails (but 1 in 1026)
				rollMatters = !stool[0].IsDeleted;
				player.SetRealSkillValue((int) SkillName.Musicianship, 1000);
				player.SetTag(HallSongErrand.TriedAtTK, Globals.TimeInSeconds); //a try just now: the next one waits
				Errands.UseThing(stool[0], player);
				pausedAfterATry = !stool[0].IsDeleted;
				for (int i = 0; (i < 10) && !stool[0].IsDeleted; i++) { //Erebor's roll fails now and then even at 100.0
					player.RemoveTag(HallSongErrand.TriedAtTK);
					Errands.UseThing(stool[0], player);
				}
				sung = (lute is Musical) && stool[0].IsDeleted && (song.index == 2) && Talk(player, song) && !Errands.Active(player).Contains(song);
			}
			Report("theSongNeedsMusicAndAnInstrument", notAsked && noInstrument && rollMatters);
			Report("theSongIsPlayedInTheHall", sung && pausedAfterATry);
		}
	}
}
