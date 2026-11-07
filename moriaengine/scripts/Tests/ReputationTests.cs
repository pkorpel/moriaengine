using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1243 - reputation checked on the server; Tests/Protocol/ReputationTests.cs drives these and .povest through a client.</summary>
	public static class ReputationTests {
		private const string Edoras = "Edoras";

		private static void Report(string name, bool ok) {
			Console.WriteLine("REPUTATION_CHECK: " + name + "=" + ok);
		}

		private static Point4D EdorasSpot() {
			StaticRegion area = StaticRegion.GetByDefname("a_Edoras");
			ImmutableRectangle rect = area.Rectangles[0];
			int[] spot = WorldSpots.NearestStandable(Map.GetMap(0), rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2), 20);
			return new Point4D(spot[0], spot[1], spot[2], 0);
		}

		[SteamFunction]
		public static void TestReputationCore(Character self) {
			Reputation.NowOverride = null;
			self.Account.PLevel = 1; //a player: GMs pay list price and aren't judged
			self.Realm = Realm.Gondor;
			self.Go(EdorasSpot());
			Report("standingInEdoras", Reputation.CityAt(self) == Edoras);
			Report("ownRealmStartsKnown", (Reputation.Of(self, Edoras) == 0) && (Reputation.Level(self, Edoras) == ReputationLevel.Znamy));
			Report("theEnemyRealmStartsAsEnemy", (Reputation.Of(self, "Barad-dur") == -600) && (Reputation.Level(self, "Barad-dur") == ReputationLevel.Nepritel));
			Report("keysAreWords", Reputation.Word("Barad-dur") == "barad_dur");

			Reputation.Change(self, Edoras, 250);
			Report("aDeedRaisesIt", (Reputation.Of(self, Edoras) == 250) && (Reputation.Level(self, Edoras) == ReputationLevel.Vazeny));
			Reputation.Change(self, Edoras, 5000);
			Report("itStopsAtAThousand", (Reputation.Of(self, Edoras) == 1000) && (Reputation.Level(self, Edoras) == ReputationLevel.Hrdina));
			Reputation.NowOverride = DateTime.UtcNow.AddDays(31);
			Report("aHeroWithNoRecentDeedShowsHonoured", Reputation.Level(self, Edoras) == ReputationLevel.Cteny);
			Reputation.NowOverride = null;

			Reputation.Set(self, Edoras, -1000);
			Reputation.NowOverride = DateTime.UtcNow.AddDays(4);
			Report("badStandingDrifts", Reputation.Of(self, Edoras) == -800);
			Reputation.NowOverride = DateTime.UtcNow.AddDays(40);
			Report("backToZeroNotBeyond", Reputation.Of(self, Edoras) == 0);
			Reputation.NowOverride = null;

			Reputation.Change(self, "Hobitin", 100, "ho_farmers");
			Report("aFactionRises", Reputation.OfFaction(self, "ho_farmers") == 100);
			Report("itsRivalFallsByHalf", Reputation.OfFaction(self, "ho_innkeepers") == -50);

			//prices where one stands
			Reputation.Set(self, Edoras, 0);
			Report("knownPaysListPrice", Reputation.PriceFor(self, 100) == 100);
			Reputation.Set(self, Edoras, 250);
			Report("respectedPaysLess", Reputation.PriceFor(self, 100) == 95);
			Reputation.Set(self, Edoras, -200);
			Report("aSuspectPaysMore", Reputation.PriceFor(self, 100) == 150);
			Reputation.Set(self, Edoras, -900);
			Report("anEnemyIsRefused", Reputation.Refuses(self) && (Reputation.PriceFor(self, 100) == 0));
			self.Account.PLevel = 7;
			Report("aGmIsntJudged", !Reputation.Refuses(self) && (Reputation.PriceFor(self, 100) == 100));
			self.Account.PLevel = 1;

			//guards of your realm stop protecting an enemy of their city
			NPC guard = (NPC) ThingDef.GetByDefname("c_vguard_gondor_hala_pesak").Create(self.P());
			guard.Realm = Realm.Gondor;
			Reputation.Set(self, Edoras, 0);
			Notoriety.ClearCriminal(self);
			bool protectedKnown = GuardSpeech.IsProtectedBy(guard, self);
			Reputation.Set(self, Edoras, -900);
			bool protectedEnemy = GuardSpeech.IsProtectedBy(guard, self);
			guard.Delete();
			Report("guardsProtectAKnownCitizen", protectedKnown);
			Report("butNotTheCitysEnemy", !protectedEnemy);

			//a crime where you stand, counted once a minute
			Reputation.Set(self, Edoras, 0);
			Notoriety.FlagCriminal(self);
			int afterOne = Reputation.Of(self, Edoras);
			Notoriety.FlagCriminal(self);
			Report("aCrimeCosts", afterOne == -ReputationSettings.instance.crimeLoss);
			Report("blowAfterBlowIsOneCrime", Reputation.Of(self, Edoras) == afterOne);
			Notoriety.ClearCriminal(self);
			Reputation.Set(self, Edoras, 0);
			self.Account.PLevel = 7;
			Console.WriteLine("REPUTATION_CORE_DONE");
		}

		/// <summary>A plain Gondor player, for .povest from the player allowlist.</summary>
		[SteamFunction]
		public static void TestReputationAsPlayer(Character self) {
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
		}

		[SteamFunction]
		public static void TestReputationResidents(Character self) {
			self.Account.PLevel = 7;
			List<FoundRoom> rooms = RoomScan.Candidates(Edoras, self.MovementSettings);
			VenueDraft draft = VenueDraft.OfRoom(rooms[0], rooms[0].M, Edoras);
			draft.SetType(VenueType.Get("bakery").IndexOf());
			string problem;
			CityVenue bakery = draft.Save(out problem);
			Residents.FillRoles(bakery);
			NPC baker = Residents.BodyOf(Residents.OfVenue(bakery.VenueId)[0]);
			self.Go(new Point4D(baker.X, baker.Y, baker.Z, baker.M));
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
			Reputation.Set(self, Edoras, 0);
			Notoriety.ClearCriminal(self);
			int before = Reputation.Of(self, Edoras);
			DamageManager.CauseDamage(self, baker, DamageType.Blunt, 3);
			int afterHit = Reputation.Of(self, Edoras);
			DamageManager.CauseDamage(self, baker, DamageType.Blunt, 3);
			Report("hurtingAResidentCosts", afterHit <= before - ReputationSettings.instance.harmResidentLoss);
			Report("butOnceAMinute", Reputation.Of(self, Edoras) == afterHit);
			int beforeKill = Reputation.Of(self, Edoras);
			baker.CauseDeath(self);
			Report("killingOneCostsMore", Reputation.Of(self, Edoras) == beforeKill - ReputationSettings.instance.killResidentLoss);
			Notoriety.ClearCriminal(self);
			Reputation.Set(self, Edoras, 0);
			self.Account.PLevel = 7;
			Console.WriteLine("REPUTATION_RESIDENTS_DONE");
		}
	}
}
