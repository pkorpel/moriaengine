using System;
using System.Linq;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1252 - the first eight errand templates: a tavern's innkeeper offers one of them to a client; then every template
	/// built for a fitting giver, the lost item in a venue, the debt's three answers and a slow walk, on the server.
	/// Server side: scripts/Tests/ErrandTemplateTests.cs.</summary>
	[Collection(ErrandTemplateCollection.Name)]
	public class ErrandTemplateTests : LivingWorldTestBase {
		private static readonly string[] names = { "Dopis", "Vyptavani", "Nakup", "Suroviny", "Ztracena vec", "Dluh", "Doprovod", "Smena",
			//#1291/#1293 - the templates added since that a tavern's innkeeper can also offer
			"Hadka", "Tajny ctitel", "Pozvanka", "Milostny dopis", "Pivo pro strazne", "Zakazka", "Kuryr", "Rvacka",
			//#1317 - and the rest a worker outside the war camp may offer (#1291, #1293b were missing: an innkeeper's random pick failed the test now and then)
			"Vypujcene knihy", "Znameni", "Scitani", "Slova mrtvych", "Cerny pasazer", "Uteklo", "Lampar", "Krysy ve sklepe", "Zlodej", "Ztracena zasilka",
			//#1317 - the wild's riddles (any tavern), the singing hermit (Edoras), the hall song (an Edoras tavern, for a player who plays)
			"Hadanky ve tme", "Zpevavy poustevnik", "Pisen pro sin" };

		public ErrandTemplateTests(GameServerPairFixture servers)
			: base(servers, "errandtpltests", "ERRANDTPL") {
		}

		[Fact]
		public void ResidentsOfferTheEightKindsOfErrands() {
			using (TestGameClient game = this.Login("Posluha")) {
				this.Run(game, ".TestErrandTemplatesSetup", "ERRANDTPL_SETUP_DONE");
				this.AssertChecks("allEightAreRegistered");
				uint innkeeper = (uint) Number(this.LastLine("ERRANDTPL_SETUP:"), "innkeeper");

				game.SendDoubleClick(innkeeper);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(talk.GumpUid, 1);
				var offer = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(offer.Strings, s => names.Contains(s));
				game.SendGumpResponse(offer.GumpUid, 0);

				this.Run(game, ".TestErrandTemplatesRules", "ERRANDTPL_RULES_DONE");
				this.AssertChecks("letterGoesToSomeoneElse", "askingAroundChainsPeople", "aMarketRunFetches", "aSmithWantsIngots",
					"aTempleWantsHerbs", "aGuestHasNoTradeToSupply", "aShiftCarriesMugsToTheTavernsPeople", "allTextsArePlainAscii", "theLostThingLiesInTheVenue",
					"bringingItBackEndsIt", "onlyItsPlayerMayPickItUp", "givingUpClearsTheLostThing", "forgivingPaysLessThreateningMore", "aForgivenDebtorBecomesAFriend",
					"threateningCostsKarma", "theOldOneFollows", "notWithoutThem", "arrivingTogetherEndsIt", "andTheyGoBackToTheirDay", "aWalkWithNoErrandLetsGo");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ErrandTemplateCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ErrandTemplate";
	}
}
