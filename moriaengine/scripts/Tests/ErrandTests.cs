using System;
using System.Collections.Generic;
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

			public override bool TryBuild(ErrandContext context, Errand errand) {
				Item other = context.PickOther();
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

		/// <summary>After the client took the errand from the giver and gave the letter to the other resident.</summary>
		[SteamFunction]
		public static void TestErrandAfterClient(Character self) {
			Player player = (Player) self;
			List<Errand> active = Errands.Active(player);
			Errand errand = (active.Count == 1) ? active[0] : null;
			Report("theClientTookIt", (errand != null) && (errand.key == "test_errand"));
			Report("theLetterWasDelivered", (errand != null) && (errand.index == 1) && (Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname(LetterDef)) == 0));
			Console.WriteLine("ERRAND_AFTER_DONE");
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
			Report("theWrongResidentCant", !Errands.Do(player, errand, otherBody));
			Report("noApplesNoStep", !Errands.Do(player, errand, giverBody) && (errand.index == 1));
			ItemDef apple = (ItemDef) ThingDef.GetByDefname(FetchDef);
			Item apples = (Item) apple.Create(player.Backpack);
			apples.Amount = 3;
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
			Report("theLetterIsInThePack", Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname(LetterDef)) == 1);

			//giving up takes the letter back
			Errands.GiveUp(player, second);
			Report("givingUpTakesTheLetterBack", (Errands.Active(player).Count == 0) && (Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname(LetterDef)) == 0));

			//an errand runs out
			log.day = -1;
			Errand third = Errands.Make(player, other);
			Errands.Accept(player, third);
			Errands.NowOverride = DateTime.UtcNow.AddHours(5);
			Report("anErrandRunsOut", (Errands.Active(player).Count == 0) && (Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname(LetterDef)) == 0));
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
