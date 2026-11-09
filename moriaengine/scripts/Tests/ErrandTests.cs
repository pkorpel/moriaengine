using System;
using System.Collections.Generic;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1250 - the errand engine checked on the server; Tests/Protocol/ErrandTests.cs drives these and the dialogs through a
	/// client. A test template (four steps: a letter to a second resident, then the giver wants two apples, comes back in the
	/// evening, and asks a question) stands in for the real ones of step 7.</summary>
	public static class ErrandTests {
		private static readonly TagKey giverTK = TagKey.Acquire("_errandtest_giver_");
		private static readonly TagKey otherTK = TagKey.Acquire("_errandtest_other_");
		internal const string FetchDef = "i_fruit_apple";
		internal const string LetterDef = "i_scroll_blank";

		private static void Report(string name, bool ok) {
			Console.WriteLine("ERRAND_CHECK: " + name + "=" + ok);
		}

		/// <summary>The test's template: deliver a letter to `other`, bring the giver two apples, see them at evening, answer.</summary>
		internal sealed class TestTemplate : ErrandTemplate {
			public override string Key {
				get { return "test_errand"; }
			}

			public override string Name {
				get { return "Zkusebni ukol"; }
			}

			public override ErrandTier Tier {
				get { return ErrandTier.Medium; }
			}

			/// <summary>Whom the letter goes to: the test names them, so the client knows whose dialog to open.</summary>
			internal static Item receiver;

			public override bool TryBuild(ErrandContext context, Errand errand) {
				Item other = context.PickOther(gem => (receiver == null) || (gem == receiver) || (context.GiverGem == receiver));
				if (other == null) {
					return false;
				}
				Item giver = context.GiverGem;
				errand.offer = "Odnes tenhle dopis " + ErrandContext.Who(other) + ".";
				errand.thanks = "Dekuji ti.";
				errand.steps.Add(Errands.Deliver(other, LetterDef, "Odnes dopis: " + ErrandContext.Who(other), "Nesu ti dopis od " + context.GiverName + ".", "Diky za dopis."));
				errand.steps.Add(Errands.Fetch(giver, FetchDef, 2, "Dones " + context.GiverName + " dve jablka", "Tady jsou jablka.", "Dobra jablka."));
				errand.steps.Add(Errands.Wait(giver, 18, 22, "Prijd za " + context.GiverName + " vecer", "Prisel jsem vecer.", "Presne vcas."));
				errand.steps.Add(Errands.Choose(giver, "Odpovez " + context.GiverName, new[] { "Pomohu rad.", "Chci zaplatit vic." }, new[] { "To jsem rad.", "Hm, uvidime." }));
				return true;
			}
		}

		/// <summary>A brewery in Edoras with its three people; only the test template is offered; I stand by the first.</summary>
		[SteamFunction]
		public static void TestErrandSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				List<FoundRoom> rooms = RoomScan.Candidates("Edoras", player.MovementSettings);
				VenueDraft draft = VenueDraft.OfRoom(rooms[0], rooms[0].M, "Edoras");
				draft.SetType(VenueType.Get("brewery").IndexOf());
				string problem;
				CityVenue brewery = draft.Save(out problem);
				if (brewery == null) {
					throw new SEException("the brewery wasn't saved: " + problem);
				}
				Residents.FillRoles(brewery);
				List<Item> people = Residents.OfVenue(brewery.VenueId);
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new TestTemplate());
				TestTemplate.receiver = people[1];
				foreach (Item gem in people) {
					NPCAiTimer.Freeze(Residents.BodyOf(gem), 600); //they stay where the player talks to them
				}
				Item giver = people[0];
				player.SetTag(giverTK, giver);
				player.SetTag(otherTK, people[1]);
				NPC body = Residents.BodyOf(giver);
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				player.RemoveTag(Errands.LogTK);
				Report("aGiverCanOffer", Errands.WhyNot(player, giver, "Edoras") == null);
				Console.WriteLine("ERRAND_SETUP: giver=" + Residents.BodyOf(giver).Uid + " other=" + Residents.BodyOf(people[1]).Uid +
					" othername=" + Residents.NameOf(people[1]));
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("ERRAND_SETUP_DONE");
			}
		}

		/// <summary>Args "giver" or "other": stand beside that resident (talking needs reach).</summary>
		[SteamFunction]
		public static void TestErrandGoTo(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			Item gem = (Item) player.GetTag(sa.Args.Trim().Trim('"') == "other" ? otherTK : giverTK);
			Near(player, Residents.BodyOf(gem));
			Console.WriteLine("ERRAND_WENT: done");
		}

		//the errand's letters in the pack: its own items, which Errands.CountIn leaves out on purpose
		private static int Letters(Player player) {
			return CountAll(player.Backpack, ThingDef.GetByDefname(LetterDef));
		}

		private static int CountAll(Container container, ThingDef def) {
			int count = 0;
			foreach (AbstractItem item in container) {
				if (item.Def == def) {
					count += item.Amount;
				} else if (item is Container inside) {
					count += CountAll(inside, def);
				}
			}
			return count;
		}

		//#1312 - CanTalk from a tile this far, in the resident's sight, in any of the 8 directions; null if no such tile
		private static bool? TalksFrom(Player player, NPC npc, int distance) {
			for (int dir = 0; dir < 8; dir++) {
				int dx = new[] { 0, 1, 1, 1, 0, -1, -1, -1 }[dir], dy = new[] { -1, -1, 0, 1, 1, 1, 0, -1 }[dir];
				player.Go(new Point4D((ushort) (npc.X + (dx * distance)), (ushort) (npc.Y + (dy * distance)), npc.Z, npc.M));
				if ((Point2D.GetSimpleDistance(player, npc) == distance) && player.CanSeeLOS(npc).Allow) {
					return Errands.CanTalk(player, npc);
				}
			}
			return null;
		}

		private static void Near(Player player, NPC npc) {
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		/// <summary>After the client took the errand from the giver and gave the letter to the other resident.</summary>
		[SteamFunction]
		public static void TestErrandAfterClient(Character self) {
			Player player = (Player) self;
			List<Errand> active = Errands.Active(player);
			Errand errand = (active.Count == 1) ? active[0] : null;
			Report("theClientTookIt", (errand != null) && (errand.key == "test_errand"));
			Report("theLetterWasDelivered", (errand != null) && (errand.index == 1) && (Letters(player) == 0));
			string apple = Errands.ItemName(FetchDef);
			Report("theFetchNamesTheItem", (errand != null) && (apple.Length > 0) && errand.Current.task.EndsWith(" (" + apple + ")", StringComparison.Ordinal)
				&& Errands.Wanted(errand) == "2x " + apple); //#1312
			Console.WriteLine("ERRAND_AFTER_DONE");
		}

		/// <summary>#1312 - the account's plevel and max plevel (what a typed command is checked against), so a player
		/// command is tried as a plain player.</summary>
		[SteamFunction]
		public static void TestErrandPlevel(Character self, int plevel) {
			typeof(AbstractAccount).GetField("maxPlevel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self.Account, (byte) plevel);
			self.Account.PLevel = (byte) plevel;
			Console.WriteLine("ERRAND_PLEVEL: " + self.Account.PLevel + " max=" + self.Account.MaxPLevel);
		}

		/// <summary>The rest of the steps, the pay, the limits, running out and giving up, on the server.</summary>
		[SteamFunction]
		public static void TestErrandRules(Character self) {
			Player player = (Player) self;
			try {
				Rules(player);
			} finally {
				VenueClock.Override = null;
				Errands.NowOverride = null;
				Console.WriteLine("ERRAND_RULES_DONE");
			}
		}

		private static void Rules(Player player) {
			Item giver = (Item) player.GetTag(giverTK);
			Item other = (Item) player.GetTag(otherTK);
			NPC giverBody = Residents.BodyOf(giver);
			NPC otherBody = Residents.BodyOf(other);
			Errand errand = Errands.Active(player)[0];

			Report("theGiverGivesNoSecond", Errands.WhyNot(player, giver, "Edoras") != null);
			Near(player, otherBody);
			Report("theWrongResidentCant", !Errands.Do(player, errand, otherBody));
			Near(player, giverBody);
			Report("noApplesNoStep", !Errands.Do(player, errand, giverBody) && (errand.index == 1));
			ItemDef apple = (ItemDef) ThingDef.GetByDefname(FetchDef);
			Item apples = (Item) apple.Create(player.Backpack);
			apples.Amount = 3;
			player.Go(new Point4D((ushort) (giverBody.X + 8), giverBody.Y, giverBody.Z, giverBody.M));
			Report("notFromAfar", !Errands.Do(player, errand, giverBody) && (errand.index == 1));
			Report("fourTilesAwayTalks", TalksFrom(player, giverBody, 4) == true); //#1312
			Report("fiveTilesAwayDoesnt", TalksFrom(player, giverBody, 5) == false);
			Near(player, giverBody);
			Report("twoApplesTaken", Errands.Do(player, errand, giverBody) && (Errands.CountIn(player.Backpack, apple) == 1) && (errand.index == 2));
			VenueClock.Override = 10;
			Report("notInTheMorning", !Errands.Do(player, errand, giverBody));
			VenueClock.Override = 19;
			Report("butInTheEvening", Errands.Do(player, errand, giverBody) && (errand.index == 3));

			long goldBefore = GoldUtils.CountGoldOn(player);
			int friendBefore = Residents.Friendship(player, Residents.IdOf(giver));
			int repBefore = Reputation.Of(player, "Edoras");
			Report("theAnswerIsKept", Errands.Do(player, errand, giverBody, 1) && (errand.steps[3].chosen == 1) && errand.IsDone);
			Report("itsDoneAndPaid", (Errands.Active(player).Count == 0) && (GoldUtils.CountGoldOn(player) > goldBefore) &&
				(Residents.Friendship(player, Residents.IdOf(giver)) == friendBefore + ErrandSettings.instance.friendMedium) &&
				(Reputation.Of(player, "Edoras") == repBefore + ErrandSettings.instance.repMedium));

			//the giver's one a day: a new game day lets them offer again, and the same template again pays less
			ErrandLog log = Errands.Log(player);
			Report("oneADayFromAGiver", Errands.WhyNot(player, giver, "Edoras") != null);
			log.day = -1;
			Errand second = Errands.Make(player, giver);
			Report("aNewDayANewOne", (second != null) && Errands.Accept(player, second));
			Report("theSameAgainPaysLess", second.payPercent == 100 - ErrandSettings.instance.repeatPenalty);
			Report("theLetterIsInThePack", Letters(player) == 1);

			//giving up takes the letter back
			Errands.GiveUp(player, second);
			Report("givingUpTakesTheLetterBack", (Errands.Active(player).Count == 0) && (Letters(player) == 0));

			//the city's enemy gets none
			int standing = Reputation.Of(player, "Edoras");
			log.day = -1;
			Reputation.Set(player, "Edoras", -900);
			Report("anEnemyOfTheCityGetsNone", (Errands.WhyNot(player, giver, "Edoras") != null) && (Errands.Make(player, giver) == null));
			Reputation.Set(player, "Edoras", standing);
			//#1312 - no errands in the enemy realm's towns, nor are they listed; outcasts and those of no realm see all
			Realm realm = player.Realm;
			bool mordorRefused, gondorNotRefused;
			List<string> mordorSees, gondorSees, outcastSees;
			try {
				player.Realm = Realm.Mordor;
				mordorRefused = (Errands.WhyNot(player, giver, "Edoras") == Errands.EnemyRealmRefusal) && (Errands.Make(player, giver) == null);
				mordorSees = Reputation.CitiesOf(player);
				player.Realm = Realm.Gondor;
				gondorNotRefused = Errands.WhyNot(player, giver, "Edoras") != Errands.EnemyRealmRefusal;
				gondorSees = Reputation.CitiesOf(player);
				player.Realm = Realm.GondorOutcast;
				outcastSees = Reputation.CitiesOf(player);
			} finally {
				player.Realm = realm;
			}
			Report("anEnemyRealmGetsNone", mordorRefused && gondorNotRefused);
			Report("enemyCitiesArentListed", !mordorSees.Contains("Edoras") && mordorSees.Contains("Barad-dur") && !gondorSees.Contains("Umbar") && gondorSees.Contains("Edoras")
				&& outcastSees.Contains("Edoras") && outcastSees.Contains("Umbar"));

			//an errand runs out
			log.day = -1;
			Errand third = Errands.Make(player, other);
			Errands.Accept(player, third);
			Report("errandsAreNumbered", (third.id > second.id) && (Errands.ById(player, third.id) == third));
			Errands.NowOverride = DateTime.UtcNow.AddHours(5);
			Report("anErrandRunsOut", (Errands.Active(player).Count == 0) && (Letters(player) == 0));
			Errands.NowOverride = null;

			//no more than maxActive at once
			log = Errands.Log(player);
			log.day = -1;
			int max = ErrandSettings.instance.maxActive;
			ErrandSettings.instance.maxActive = 1;
			Errands.Accept(player, Errands.Make(player, giver));
			Report("aCapOnTakenErrands", Errands.WhyNot(player, other, "Edoras") != null);
			ErrandSettings.instance.maxActive = max;
			foreach (Errand left in new List<Errand>(Errands.Active(player))) {
				Errands.GiveUp(player, left);
			}
		}
	}
}
