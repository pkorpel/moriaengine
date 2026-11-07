using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1258 - the armoury, the smithy, the temple and the Houses of Healing: the smith's dialog shows the smithy's lines to a
	/// client, which works the bellows; then every action and errand on the server. Server side: scripts/Tests/WorkVenueTests.cs.</summary>
	[Collection(WorkVenueCollection.Name)]
	public class WorkVenueTests : LivingWorldTestBase {
		public WorkVenueTests(GameServerPairFixture servers)
			: base(servers, "workvenuetests", "WORKVENUE") {
		}

		[Fact]
		public void TheCitysWorkplacesHaveWorkAndErrands() {
			using (TestGameClient game = this.Login("Kovar")) {
				this.Run(game, ".TestWorkVenueSetup", "WORKVENUE_SETUP_DONE");
				string setup = this.LastLine("WORKVENUE_SETUP:");

				game.SendDoubleClick((uint) Number(setup, "smith"));
				var talk = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Pomoct u mechu", talk.Strings);
				Assert.Contains("Prinest rudu", talk.Strings);
				game.SendGumpResponse(talk.GumpUid, Number(setup, "bellows"));
				this.Run(game, ".TestWorkVenueAfterClient", "WORKVENUE_CLIENT_DONE");
				this.AssertChecks("theClientWorkedTheBellows");

				this.Run(game, ".TestWorkVenueRules", "WORKVENUE_RULES_DONE");
				this.AssertChecks("theSmithyOffers", "theBellowsTakeStaminaAndRest", "theBellowsHaveADailyCap", "theSmithBuysOreUpToADay", "theTempleOffers", "anOfferingOnceADay",
					"aVigilAtNightToItsEnd", "leavingBreaksTheVigil", "patientsWantDressing", "dressingNeedsSkillABandageAndADay", "eachAskedOnlyByItsRole",
					"aRequisitionWantsTwoIronWeapons", "theTheftEndsTwoWays", "theBladeGoesToTheBarracks", "aGivenUpBladeIsTakenBack", "thePilgrimsRightWayPaysMore", "noSensibleWayIsCalledWrong", "aFeverNightToItsEnd");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class WorkVenueCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WorkVenue";
	}
}
