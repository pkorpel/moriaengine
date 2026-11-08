using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1291 - the talking errand templates on the server: each built for a fitting giver and played to its end, with its
	/// choices' effects. Tests/Protocol/TalkErrandTests.cs asks a town hall clerk for the census through a client first.</summary>
	public static class TalkErrandTests {
		private static readonly string[] types = { "tavern", "library", "townhall", "barracks", "temple" };
		private static readonly TagKey femaleTK = TagKey.Acquire("_res_female_"); //Residents' own key

		private static void Report(string name, bool ok) {
			Console.WriteLine("TALKERRANDS_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string type) {
			return TagKey.Acquire("_talkerrands_" + type + "_");
		}

		/// <summary>Five open-air venues in Edoras with their people (frozen); only the census offered; the hero beside the clerk.</summary>
		[SteamFunction]
		public static void TestTalkErrandsSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D p = WorldEvents.CentreOf("Edoras");
				Map map = Map.GetMap(p.M);
				//walkable spots near the centre, each 6 tiles from the others, so the venues' 5x5 squares don't overlap
				List<int[]> spots = new List<int[]>();
				foreach (int[] tile in WorldEvents.Reachable(map, new[] { p.X, p.Y, p.Z }, 25)) {
					if ((WorldSpots.Distance(tile, p.X, p.Y) >= 3) && spots.TrueForAll(o => WorldSpots.Distance(tile, o[0], o[1]) >= 6)) {
						spots.Add(tile);
					}
				}
				if (spots.Count < types.Length) {
					throw new SEException("Only " + spots.Count + " spots for the test venues near Edoras's centre.");
				}
				for (int i = 0; i < types.Length; i++) {
					int[] spot = spots[i];
					VenueDraft draft = new VenueDraft();
					draft.SetType(VenueType.Get(types[i]).IndexOf());
					draft.M = p.M;
					draft.City = "Edoras";
					draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
					CityVenue venue = draft.Save(out string problem);
					if (venue == null) {
						throw new SEException(types[i] + " wasn't saved: " + problem);
					}
					Residents.FillRoles(venue);
					foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
						NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
					}
					player.SetTag(VenueKey(types[i]), venue);
				}
				//a mess hall in Barad-dur's war camp, whose people give orders, not the talking errands
				Point4D camp = WorldEvents.CentreOf("Barad-dur");
				int[] campSpot = WorldSpots.NearestStandable(Map.GetMap(camp.M), camp.X, camp.Y + 8, 6);
				VenueDraft messDraft = new VenueDraft();
				messDraft.SetType(VenueType.Get("messhall").IndexOf());
				messDraft.M = camp.M;
				messDraft.City = "Barad-dur";
				messDraft.Rects.Add(new ImmutableRectangle(campSpot[0] - 2, campSpot[1] - 2, campSpot[0] + 2, campSpot[1] + 2));
				CityVenue mess = messDraft.Save(out string messProblem);
				if (mess == null) {
					throw new SEException("The camp's mess hall wasn't saved: " + messProblem);
				}
				Residents.FillRoles(mess);
				player.SetTag(VenueKey("messhall"), mess);
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new CensusErrand());
				player.RemoveTag(Errands.LogTK);
				Item clerkGem = Gem(player, "townhall", "scribe");
				if (clerkGem == null) {
					throw new SEException("The test town hall has no scribe.");
				}
				NPC clerk = Residents.BodyOf(clerkGem);
				player.Go(new Point4D(clerk.X, clerk.Y, clerk.Z, clerk.M));
				Console.WriteLine("TALKERRANDS_SETUP: clerk=" + clerk.Uid);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		[SteamFunction]
		public static void TestTalkErrandsState(Character self) {
			int census = 0;
			foreach (Errand errand in Errands.Active((Player) self)) {
				if (errand.key == "census") {
					census++;
				}
			}
			Console.WriteLine("TALKERRANDS_STATE: census=" + census);
		}

		private static Item Gem(Player player, string type, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(type));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					return gem;
				}
			}
			return null;
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city = "Edoras") {
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

		/// <summary>Plays the errand: each step at its resident (choosing `choice` at a choice), bringing what a fetch wants.</summary>
		private static bool Play(Player player, Errand errand, int choice) {
			Errands.Accept(player, errand);
			for (int guard = 0; (guard < 12) && !errand.IsDone; guard++) {
				ErrandStep step = errand.Current;
				if (step.Kind == ErrandStepKind.Fetch) {
					for (int i = 0; i < step.count; i++) {
						ThingDef.GetByDefname(step.itemDef).Create(player.Backpack);
					}
				}
				NPC body = Residents.BodyOf(Residents.ById(step.residentId));
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				if (!Errands.Do(player, errand, body, (step.Kind == ErrandStepKind.Choose) ? choice : -1)) {
					return false;
				}
			}
			return errand.IsDone;
		}

		private static bool Plain(Errand errand) {
			List<string> texts = new List<string> { errand.name, errand.offer, errand.thanks };
			foreach (ErrandStep step in errand.steps) {
				texts.Add(step.task);
				texts.Add(step.say);
				texts.Add(step.reply);
				texts.AddRange(step.choices);
				texts.AddRange(step.choiceReplies);
			}
			return texts.TrueForAll(t => (t ?? "").TrueForAllChars(c => c <= 127));
		}

		[SteamFunction]
		public static void TestTalkErrandsChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				player.Realm = Realm.Gondor;
				TalkErrands.Bootstrap(); //the setup left only the census for the client: the finish looks its template up by key
				int baseFriend = ErrandSettings.instance.friendSmall; //what finishing a small errand gives its giver anyway
				Item innkeeper = Gem(player, "tavern", "innkeeper"), librarian = Gem(player, "library", "librarian"), clerk = Gem(player, "townhall", "scribe");
				List<Errand> built = new List<Errand>();

				//the quarrel: the verdict for the other side raises their friendship
				Fresh(player);
				Errand quarrel = Build(player, new QuarrelErrand(), innkeeper);
				string other = (quarrel == null) ? null : quarrel.steps[0].residentId;
				int otherBefore = (other == null) ? 0 : Residents.Friendship(player, other);
				Report("quarrel", (quarrel != null) && (quarrel.steps[1].Kind == ErrandStepKind.Choose) && Play(player, quarrel, 1)
					&& (Residents.Friendship(player, other) == Math.Min(Residents.MaxFriendship, otherBefore + 2)));
				built.Add(quarrel);

				//the secret admirer: the wine handed over, the secret kept pleases the giver
				Fresh(player);
				Errand admirer = Build(player, new AdmirerErrand(), innkeeper);
				int giverBefore = Residents.Friendship(player, Residents.IdOf(innkeeper));
				Report("admirer", (admirer != null) && (admirer.steps[0].Kind == ErrandStepKind.Deliver) && (admirer.steps[0].itemDef == TalkErrands.WineDef)
					&& Play(player, admirer, 0) && (Residents.Friendship(player, Residents.IdOf(innkeeper)) == Math.Min(Residents.MaxFriendship, giverBefore + 1 + baseFriend)));
				built.Add(admirer);

				//the invitation: three guests, one of them wanting something brought
				Fresh(player);
				Errand invitation = Build(player, new InvitationErrand(), innkeeper);
				Report("invitation", (invitation != null) && (invitation.steps.FindAll(st => st.Kind == ErrandStepKind.Fetch).Count == 1) && (invitation.steps.Count == 4)
					&& Play(player, invitation, 0));
				built.Add(invitation);

				//the love letter: sold out, the rival's bribe on top of the pay, and the giver's friendship falls
				Residents.AddFriendship(player, Residents.IdOf(innkeeper), Residents.MaxFriendship);
				long[] gained = new long[2];
				Errand letter = null;
				for (int answer = 0; answer < 2; answer++) {
					Fresh(player);
					letter = Build(player, new LoveLetterErrand(), innkeeper);
					long before = GoldUtils.CountGoldOn(player);
					Play(player, letter, answer);
					gained[answer] = GoldUtils.CountGoldOn(player) - before;
				}
				Report("loveletter", (letter != null) && letter.IsDone && (gained[1] >= gained[0] + LoveLetterErrand.RivalBribe)
					&& (Residents.Friendship(player, Residents.IdOf(innkeeper)) == Math.Min(Residents.MaxFriendship, Residents.MaxFriendship - 3 + baseFriend))); //at the cap after answer 0, then sold out
				built.Add(letter);

				//return the books: only the library gives it; two borrowers, then the pile back
				Fresh(player);
				Errand books = Build(player, new BooksErrand(), librarian);
				Report("books", (Build(player, new BooksErrand(), innkeeper) == null) && (books != null) && (books.steps.Count == 3)
					&& (books.steps[2].itemDef == TalkErrands.BookDef) && Play(player, books, 0));
				built.Add(books);

				//the omen: only an old woman asks; three odd things brought
				Item crone = librarian;
				crone.SetTag(femaleTK, true);
				Fresh(player);
				Errand omen = Build(player, new OmenErrand(), crone);
				Report("omen", (Build(player, new OmenErrand(), clerk) == null) && (omen != null) && (omen.steps.Count == 3)
					&& omen.steps.TrueForAll(st => (st.Kind == ErrandStepKind.Fetch) && Array.IndexOf(TalkErrands.OmenDefs, st.itemDef) >= 0) && Play(player, omen, 0));
				built.Add(omen);

				//ale for the guards: from the tavern to a guard, a short deadline
				Fresh(player);
				Errand ale = Build(player, new AleErrand(), innkeeper);
				bool aleBuilt = ale != null;
				if (aleBuilt) {
					Errands.Accept(player, ale);
				}
				bool quick = aleBuilt && ((ale.Expires - DateTime.UtcNow).TotalMinutes <= 15.5) && (BasicErrands.VenueKey(Residents.ById(ale.steps[0].residentId)) == "barracks");
				if (aleBuilt) {
					Errands.GiveUp(player, ale);
				}
				Fresh(player);
				ale = Build(player, new AleErrand(), innkeeper);
				Report("ale", quick && (Build(player, new AleErrand(), clerk) == null) && Play(player, ale, 0));
				built.Add(ale);

				//census: only the town hall; households of other workplaces
				Fresh(player);
				Errand census = Build(player, new CensusErrand(), clerk);
				List<CityVenue> places = new List<CityVenue>();
				bool spread = census != null;
				if (spread) {
					for (int i = 0; i < census.steps.Count - 1; i++) {
						CityVenue at = Residents.WorkplaceOf(Residents.ById(census.steps[i].residentId));
						spread &= !places.Contains(at) && (at != Residents.WorkplaceOf(clerk));
						places.Add(at);
					}
				}
				Report("census", spread && (census.steps.Count >= 4) && (Build(player, new CensusErrand(), innkeeper) == null) && Play(player, census, 0));
				built.Add(census);

				//the war camp gives orders, not these; every text plain
				Item campCook = null;
				foreach (Item gem in Residents.OfVenue(((CityVenue) player.GetTag(VenueKey("messhall"))).VenueId)) {
					campCook = campCook ?? gem;
				}
				Report("notInTheCamp", (campCook != null) && (Build(player, new LetterErrand(), campCook, "Barad-dur") != null) //people there to pick from
					&& (Build(player, new QuarrelErrand(), campCook, "Barad-dur") == null) && (Build(player, new LoveLetterErrand(), campCook, "Barad-dur") == null));
				Report("plainCzech", built.TrueForAll(e => (e != null) && Plain(e)));
			} finally {
				Fresh(player);
				account.PLevel = plevel;
				Console.WriteLine("TALKERRANDS_CHAIN_DONE");
			}
		}
	}

	internal static class TalkErrandTestText {
		internal static bool TrueForAllChars(this string text, Predicate<char> test) {
			foreach (char c in text) {
				if (!test(c)) {
					return false;
				}
			}
			return true;
		}
	}
}
