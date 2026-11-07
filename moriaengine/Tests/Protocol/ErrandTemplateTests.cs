using System;
using System.Linq;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1252 - the first eight errand templates: a tavern's innkeeper offers one of them to a client; then every template
	/// built for a fitting giver, the lost item in a venue, the debt's three answers and a slow walk, on the server.
	/// Server side: scripts/Tests/ErrandTemplateTests.cs.</summary>
	[Collection(ErrandTemplateCollection.Name)]
	public class ErrandTemplateTests : LivingWorldTestBase {
		private static readonly string[] names = { "Dopis", "Vyptavani", "Nakup", "Suroviny", "Ztracena vec", "Dluh", "Doprovod", "Smena" };

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
					"aTempleWantsHerbs", "aShiftCarriesMugsToTheTavernsPeople", "allTextsArePlainAscii", "theLostThingLiesInTheVenue",
					"bringingItBackEndsIt", "givingUpClearsTheLostThing", "forgivingPaysLessThreateningMore", "aForgivenDebtorBecomesAFriend",
					"threateningCostsKarma", "theOldOneFollows", "notWithoutThem", "arrivingTogetherEndsIt", "andTheyGoBackToTheirDay");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ErrandTemplateCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ErrandTemplate";
	}
}
