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

		private static readonly TagKey lastCrimeTK = TagKey.Acquire("_rep_lastcrime_");
		private static readonly TagKey lastHarmTK = TagKey.Acquire("_rep_lastharm_");

		[SteamFunction]
		public static void TestReputationCore(Character self) {
			try {
				Core(self);
			} finally {
				Reputation.NowOverride = null; //a static: never leave it set for whatever runs next
				self.Account.PLevel = 7;
				Console.WriteLine("REPUTATION_CORE_DONE");
			}
		}

		private static void Core(Character self) {
			Reputation.NowOverride = null;
			self.RemoveTag(lastCrimeTK);
			self.RemoveTag(lastHarmTK);
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

			//in the enemy city bad standing comes back to the enemy start, not to 0
			Reputation.Change(self, "Barad-dur", -150);
			Reputation.NowOverride = DateTime.UtcNow.AddDays(30);
			Report("enemyStandingDriftsToTheEnemyStart", Reputation.Of(self, "Barad-dur") == -600);
			Reputation.NowOverride = DateTime.UtcNow.AddDays(-30);
			Report("aClockBackDoesntDeepenIt", Reputation.Of(self, "Barad-dur") == -750);
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
			int suspectPrice = ReputationSettings.instance.priceSuspect;
			ReputationSettings.instance.priceSuspect = 0;
			Reputation.Set(self, Edoras, -200);
			Report("aZeroPriceSettingIsNeverFree", (Reputation.PriceFor(self, 100) == 100) && !Reputation.Refuses(self));
			ReputationSettings.instance.priceSuspect = suspectPrice;
			//citizenship: a stranger of the realm qualifies by default; a GM can raise the bar
			int citizenLevel = ReputationSettings.instance.citizenshipMinLevel;
			Reputation.Set(self, Edoras, 0);
			bool knownMay = Reputation.MayBecomeCitizen(self, Edoras);
			ReputationSettings.instance.citizenshipMinLevel = (int) ReputationLevel.Vazeny;
			bool knownMayNot = !Reputation.MayBecomeCitizen(self, Edoras);
			Reputation.Set(self, Edoras, 250);
			Report("citizenshipFollowsTheSetting", knownMay && knownMayNot && Reputation.MayBecomeCitizen(self, Edoras));
			ReputationSettings.instance.citizenshipMinLevel = citizenLevel;
			Report("nothingCostsNothing", Reputation.PriceFor(self, 0) == 0);
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

			//a crime against your own realm where it happened, counted once a minute; against the enemy realm, none
			NPC innocent = (NPC) ThingDef.GetByDefname("c_resident_man").Create(self.P());
			innocent.Realm = Realm.Gondor;
			Reputation.Set(self, Edoras, 0);
			Reputation.OnCrime(self, innocent);
			int afterOne = Reputation.Of(self, Edoras);
			Reputation.OnCrime(self, innocent);
			Report("aCrimeCosts", afterOne == -ReputationSettings.instance.crimeLoss);
			Report("blowAfterBlowIsOneCrime", Reputation.Of(self, Edoras) == afterOne);
			self.RemoveTag(lastCrimeTK);
			innocent.Realm = Realm.Mordor;
			Reputation.Set(self, Edoras, 0);
			Reputation.OnCrime(self, innocent);
			Reputation.OnMurder(innocent, self);
			Report("fightingTheEnemyRealmIsNoCrime", Reputation.Of(self, Edoras) == 0);
			innocent.Realm = Realm.Gondor;
			Reputation.OnMurder(innocent, self);
			Report("aMurderCosts", Reputation.Of(self, Edoras) == -ReputationSettings.instance.murderLoss);
			//looting: the city where it happens, not where the dead owner's ghost has gone
			Reputation.Set(self, Edoras, 0);
			self.RemoveTag(lastCrimeTK);
			innocent.P(new Point4D(10, 10, 0, 250));
			Reputation.OnCrime(self, innocent, self);
			Report("aCrimeCountsWhereItHappened", Reputation.Of(self, Edoras) == -ReputationSettings.instance.crimeLoss);
			innocent.Delete();
			self.RemoveTag(lastCrimeTK);
			Notoriety.ClearCriminal(self);
			Reputation.Set(self, Edoras, 0);
			//an outcast is distrusted everywhere, not an enemy
			self.Realm = Realm.GondorOutcast;
			Report("anOutcastIsASuspect", (Reputation.Of(self, "Harad") == ReputationSettings.instance.outcastStart) &&
				(Reputation.Level(self, "Harad") == ReputationLevel.Podezrely));
			self.Realm = Realm.Gondor;
		}

		/// <summary>A plain Gondor player, for .povest from the player allowlist.</summary>
		[SteamFunction]
		public static void TestReputationAsPlayer(Character self) {
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
		}

		[SteamFunction]
		public static void TestReputationResidents(Character self) {
			try {
				ResidentsCore(self);
			} finally {
				self.Realm = Realm.Gondor;
				self.Account.PLevel = 7;
				Console.WriteLine("REPUTATION_RESIDENTS_DONE");
			}
		}

		private static void ResidentsCore(Character self) {
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
			self.RemoveTag(lastHarmTK);
			int before = Reputation.Of(self, Edoras);
			DamageManager.CauseDamage(self, baker, DamageType.Blunt, 3);
			int afterHit = Reputation.Of(self, Edoras);
			DamageManager.CauseDamage(self, baker, DamageType.Blunt, 3);
			Report("hurtingAResidentCosts", afterHit == before - ReputationSettings.instance.harmResidentLoss); //hurting an NPC is no crime (Notoriety)
			Report("butOnceAMinute", Reputation.Of(self, Edoras) == afterHit);
			//a pet set on a resident is its owner's doing; a raider of the enemy realm isn't judged by the city
			self.RemoveTag(lastHarmTK);
			NPC pet = (NPC) ThingDef.GetByDefname("c_resident_man").Create(self.P());
			pet.ControlMaster = self;
			int beforePet = Reputation.Of(self, Edoras);
			DamageManager.CauseDamage(pet, baker, DamageType.Blunt, 3);
			Report("aPetsBlowIsItsOwners", Reputation.Of(self, Edoras) == beforePet - ReputationSettings.instance.harmResidentLoss);
			pet.Delete();
			self.RemoveTag(lastHarmTK);
			Realm bakerRealm = baker.Realm;
			baker.Realm = Realm.Gondor;
			self.Realm = Realm.Mordor;
			int beforeRaid = Reputation.Of(self, Edoras);
			DamageManager.CauseDamage(self, baker, DamageType.Blunt, 3);
			Report("theEnemyRealmIsntJudged", Reputation.Of(self, Edoras) == beforeRaid);
			self.Realm = Realm.Gondor;
			baker.Realm = bakerRealm;
			int beforeKill = Reputation.Of(self, Edoras);
			baker.CauseDeath(self);
			Report("killingOneCostsMore", Reputation.Of(self, Edoras) == beforeKill - ReputationSettings.instance.killResidentLoss);
			Notoriety.ClearCriminal(self);
			Reputation.Set(self, Edoras, 0);
		}
	}
}
