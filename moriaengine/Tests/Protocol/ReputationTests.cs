using System;
using System.Collections.Generic;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1243 - reputation: starts by realm, levels, the hero's recent deed, drift, faction rivalry, prices and refusal, guards,
	/// crimes once a minute, residents hurt and killed; and .povest from a plain player. Server side: scripts/Tests/ReputationTests.cs.</summary>
	[Collection(ReputationCollection.Name)]
	public class ReputationTests : LivingWorldTestBase {
		public ReputationTests(GameServerPairFixture servers)
			: base(servers, "reputationtests", "REPUTATION") {
		}

		[Fact]
		public void TheCitiesRememberWhatYouDo() {
			using (TestGameClient game = this.Login("Wanderer")) {
				this.Run(game, ".TestReputationCore", "REPUTATION_CORE_DONE");
				this.AssertChecks("standingInEdoras", "ownRealmStartsKnown", "theEnemyRealmStartsAsEnemy", "keysAreWords", "aDeedRaisesIt", "itStopsAtAThousand",
					"aHeroWithNoRecentDeedShowsHonoured", "badStandingDrifts", "backToZeroNotBeyond", "aFactionRises", "itsRivalFallsByHalf", "knownPaysListPrice",
					"respectedPaysLess", "aSuspectPaysMore", "anEnemyIsRefused", "aGmIsntJudged", "guardsProtectAKnownCitizen", "butNotTheCitysEnemy", "aCrimeCosts",
					"blowAfterBlowIsOneCrime");
				this.Run(game, ".TestReputationResidents", "REPUTATION_RESIDENTS_DONE");
				this.AssertChecks("hurtingAResidentCosts", "butOnceAMinute", "killingOneCostsMore");

				//.povest is a player command: open it as one
				game.SendSpeech(".TestReputationAsPlayer");
				System.Threading.Thread.Sleep(500);
				game.SendSpeech(".povest");
				var dialog = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(dialog.Strings, s => s.Contains("Povest ve mestech"));
				Assert.Contains(dialog.Strings, s => s.Contains("Edoras"));
				Assert.Contains(dialog.Strings, s => s.Contains("Barad-dur"));
				Assert.Contains(dialog.Strings, s => s.Contains("Nepritel"));
				Assert.Contains(dialog.Strings, s => s.Contains("Gorbagova tlupa"));
			}
		}
	}

	[CollectionDefinition(Name)]
	public class ReputationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Reputation";
	}
}
