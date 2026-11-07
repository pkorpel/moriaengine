using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1252 - the first eight errand templates checked on the server; Tests/Protocol/ErrandTemplateTests.cs drives these and
	/// asks a tavern's people for an errand through a client. Edoras gets a tavern, a smithy and a temple with their people.</summary>
	public static class ErrandTemplateTests {
		private static readonly TagKey venuesTK = TagKey.Acquire("_errandtpl_venues_");
		private static readonly string[] keys = { "letter", "askaround", "market", "ingredients", "lost", "debt", "walk", "shift" };

		private static void Report(string name, bool ok) {
			Console.WriteLine("ERRANDTPL_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestErrandTemplatesSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				List<FoundRoom> rooms = RoomScan.Candidates("Edoras", player.MovementSettings);
				string[] types = { "tavern", "smithy", "temple" };
				for (int i = 0; i < types.Length; i++) {
					VenueDraft draft = VenueDraft.OfRoom(rooms[i], rooms[i].M, "Edoras");
					draft.SetType(VenueType.Get(types[i]).IndexOf());
					string problem;
					CityVenue venue = draft.Save(out problem);
					if (venue == null) {
						throw new SEException("venue " + types[i] + " wasn't saved: " + problem);
					}
					Residents.FillRoles(venue);
					foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
						NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
					}
					player.SetTag(TagKey.Acquire("_errandtpl_" + types[i] + "_"), venue);
				}
				player.RemoveTag(Errands.LogTK);
				Item innkeeper = Gem(player, "tavern", "innkeeper");
				NPC body = Residents.BodyOf(innkeeper);
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				int registered = 0;
				foreach (string key in keys) {
					if (ErrandTemplates.Get(key) != null) {
						registered++;
					}
				}
				Report("allEightAreRegistered", registered == keys.Length);
				Console.WriteLine("ERRANDTPL_SETUP: innkeeper=" + body.Uid);
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("ERRANDTPL_SETUP_DONE");
			}
		}

		private static CityVenue Venue(Player player, string type) {
			return (CityVenue) player.GetTag(TagKey.Acquire("_errandtpl_" + type + "_"));
		}

		private static Item Gem(Player player, string venueType, string role) {
			foreach (Item gem in Residents.OfVenue(Venue(player, venueType).VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					return gem;
				}
			}
			return null;
		}

		private static void Near(Player player, Item gem) {
			NPC npc = Residents.BodyOf(gem);
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		//a template built for this giver, as Errands.Make would build it
		private static Errand Build(Player player, string key, Item giver) {
			ErrandTemplate template = ErrandTemplates.Get(key);
			Errand errand = new Errand { key = key, name = template.Name, giverId = Residents.IdOf(giver), city = "Edoras", tier = (int) template.Tier };
			return template.TryBuild(new ErrandContext(player, giver, "Edoras"), errand) ? errand : null;
		}

		//a clean slate for limits and the repeat penalty, so each errand here pays its own way
		private static void Fresh(Player player) {
			ErrandLog log = Errands.Log(player);
			foreach (Errand left in new List<Errand>(log.active)) {
				Errands.GiveUp(player, left);
			}
			log.day = -1;
			log.lastKey = "";
			log.streak = 0;
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
			foreach (string text in texts) {
				foreach (char c in text ?? "") {
					if (c > 127) {
						return false;
					}
				}
			}
			return true;
		}

		[SteamFunction]
		public static void TestErrandTemplatesRules(Character self) {
			Player player = (Player) self;
			try {
				Rules(player);
			} finally {
				Fresh(player);
				Console.WriteLine("ERRANDTPL_RULES_DONE");
			}
		}

		private static void Rules(Player player) {
			Item innkeeper = Gem(player, "tavern", "innkeeper");
			Item smith = Gem(player, "smithy", "smith");
			Item patron = Gem(player, "tavern", "patron");

			//each builds for a fitting giver, with its own steps, in plain Czech
			Errand letter = Build(player, "letter", innkeeper);
			Report("letterGoesToSomeoneElse", (letter != null) && (letter.steps[0].Kind == ErrandStepKind.Deliver) && (letter.steps[0].residentId != letter.giverId));
			Errand ask = Build(player, "askaround", innkeeper);
			Report("askingAroundChainsPeople", (ask != null) && (ask.steps.Count >= 3) && (ask.steps[ask.steps.Count - 1].residentId == ask.giverId));
			Errand market = Build(player, "market", innkeeper);
			Report("aMarketRunFetches", (market != null) && (market.steps[0].Kind == ErrandStepKind.Fetch) && (market.steps[0].count > 0));
			Errand ingredients = Build(player, "ingredients", smith);
			Report("aSmithWantsIngots", (ingredients != null) && (ingredients.steps[0].itemDef == "i_ingot_iron"));
			Errand herbs = Build(player, "ingredients", Gem(player, "temple", "priest"));
			Report("aGuestHasNoTradeToSupply", Build(player, "ingredients", Gem(player, "temple", "pilgrim")) == null);
			Report("aTempleWantsHerbs", (herbs != null) && (herbs.steps[0].itemDef == "i_reag_ginseng"));
			Errand shift = Build(player, "shift", innkeeper);
			Report("aShiftCarriesMugsToTheTavernsPeople", (shift != null) && (shift.steps[0].itemDef == "i_mug") &&
				(Residents.WorkplaceOf(Residents.ById(shift.steps[0].residentId)) == Venue(player, "tavern")));
			bool plain = true;
			foreach (string key in keys) {
				Errand built = Build(player, key, (key == "ingredients") ? smith : ((key == "walk") ? patron : innkeeper));
				plain &= (built != null) && Plain(built);
			}
			Report("allTextsArePlainAscii", plain);

			//lost and found: the item lies in a venue, and bringing it back ends the errand
			Fresh(player);
			Errand lost = Build(player, "lost", innkeeper);
			Errands.Accept(player, lost);
			Item placed = lost.steps[0].placed;
			CityVenue where = (placed == null) ? null : VenueRegistry.ById(lost.steps[0].venueId);
			Report("theLostThingLiesInTheVenue", (placed != null) && (where != null) && (VenueRegistry.At(placed) == where) && (placed.TopObj() == placed));
			placed.Cont = player.Backpack;
			Near(player, innkeeper);
			Report("bringingItBackEndsIt", Errands.Do(player, lost, Residents.BodyOf(innkeeper)) && lost.IsDone && placed.IsDeleted);
			Fresh(player);
			Errand lostAgain = Build(player, "lost", innkeeper);
			Errands.Accept(player, lostAgain);
			Item lying = lostAgain.steps[0].placed;
			Report("onlyItsPlayerMayPickItUp", Errands.MayPickUp(lying, player) && !Errands.MayPickUp(lying, Residents.BodyOf(patron)));
			lying.Cont = player.Bank; //worn or banked, it still goes when the errand does
			Errands.GiveUp(player, lostAgain);
			Report("givingUpClearsTheLostThing", (lying != null) && lying.IsDeleted);

			//the debt: three answers, three outcomes
			long[] gained = new long[3];
			int friendBefore = 0, friendAfter = 0;
			int karmaBefore = 0, karmaAfter = 0;
			for (int answer = 0; answer < 3; answer++) {
				Fresh(player);
				Errand debt = Build(player, "debt", innkeeper);
				Errands.Accept(player, debt);
				Item debtor = Residents.ById(debt.steps[0].residentId);
				if (answer == 1) {
					friendBefore = Residents.Friendship(player, Residents.IdOf(debtor));
				}
				if (answer == 2) {
					karmaBefore = player.Karma;
				}
				long before = GoldUtils.CountGoldOn(player);
				Near(player, debtor);
				Errands.Do(player, debt, Residents.BodyOf(debtor), answer);
				Near(player, innkeeper);
				Errands.Do(player, debt, Residents.BodyOf(innkeeper));
				gained[answer] = GoldUtils.CountGoldOn(player) - before;
				if (answer == 1) {
					friendAfter = Residents.Friendship(player, Residents.IdOf(debtor));
				}
				if (answer == 2) {
					karmaAfter = player.Karma;
				}
			}
			Report("forgivingPaysLessThreateningMore", (gained[1] < gained[0]) && (gained[2] > gained[0]));
			Report("aForgivenDebtorBecomesAFriend", friendAfter == Math.Min(Residents.MaxFriendship, friendBefore + 2));
			Report("threateningCostsKarma", karmaAfter < karmaBefore);

			//a slow walk: the old one follows, must be beside the player at the end, and goes back to their day
			Fresh(player);
			Errand walk = Build(player, "walk", patron);
			Errands.Accept(player, walk);
			Item walker = Residents.ById(walk.steps[0].residentId);
			NPC walkerBody = Residents.BodyOf(walker);
			Near(player, walker);
			Errands.Do(player, walk, walkerBody);
			Report("theOldOneFollows", walkerBody.FollowTarget == player);
			Item host = Residents.ById(walk.steps[1].residentId);
			NPC hostBody = Residents.BodyOf(host);
			walkerBody.Go(new Point4D((ushort) (hostBody.X + 12), hostBody.Y, hostBody.Z, hostBody.M));
			Near(player, host);
			Report("notWithoutThem", !Errands.Do(player, walk, hostBody) && !walk.IsDone);
			walkerBody.Go(new Point4D(hostBody.X, hostBody.Y, hostBody.Z, hostBody.M));
			Report("arrivingTogetherEndsIt", Errands.Do(player, walk, hostBody) && walk.IsDone);
			Report("andTheyGoBackToTheirDay", walkerBody.FollowTarget == null);

			//a walk that runs out lets its follower go at the schedule's next look, whoever reads the errands
			Fresh(player);
			Errand lapsed = Build(player, "walk", patron);
			Errands.Accept(player, lapsed);
			Item lapsedWalker = Residents.ById(lapsed.steps[0].residentId);
			NPC lapsedBody = Residents.BodyOf(lapsedWalker);
			Near(player, lapsedWalker);
			Errands.Do(player, lapsed, lapsedBody);
			bool following = lapsedBody.FollowTarget == player;
			Errands.Log(player).active.Remove(lapsed); //gone without a release, as a deleted character's or a lost log's would be
			ResidentSchedule.Apply(lapsedWalker, lapsedBody, false);
			Report("aWalkWithNoErrandLetsGo", following && (lapsedBody.FollowTarget == null));
		}
	}
}
