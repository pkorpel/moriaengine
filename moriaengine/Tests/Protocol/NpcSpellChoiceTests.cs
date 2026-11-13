using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1033 - server side in scripts/Tests/NpcSpellChoiceTests.cs.</summary>
	[Collection(NpcSpellChoiceCollection.Name)]
	public class NpcSpellChoiceTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private static readonly string[] Checks = {
			"everyMonsterSpellHasAKind", "guardHitsAreFull", "guardReflectsFirst", "guardCastReflection",
			"guardAtFullHealthCastsNothing", "guardAt80PercentCastsNothing", "guardAt60PercentHeals",
			"noCureWithoutPoison", "mageGotPoisoned", "poisonedMageCures",
			"fireResistantTargetGetsNoFire", "shockResistantTargetGetsNoLightning",
			"freshTargetMixesControlSpells", "hurtTargetIsNotCursed", "targetIsParalyzed", "paralyzedTargetIsNotParalyzedAgain",
			"poisonedTargetIsNotPoisonedAgain", "summonIsDispelled", "targetReflects", "reflectionGetsTheWeakestDamage",
			"noDrainOnANonMage", "mageGetsDrained", "emptyMageIsLeftAlone", "twoSummonsAppeared", "noThirdSummon", "aCastTakesTheCastersPower", /*#1420*/
		};

		private readonly GameServerPairFixture fixture;

		public NpcSpellChoiceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void MonstersCastWhatTheFightNeeds() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			bool sawDone;
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter("Chooser");
				game.SendSpeech(".TestNpcSpellChoice");
				sawDone = this.fixture.WaitForGameOutput("SPELL_CHOICE_DONE", TimeSpan.FromSeconds(30));
			}

			string output = this.fixture.GameOutputSnapshot();
			Assert.True(sawDone, "The scenario never finished. Game output:\n" + output);
			foreach (string check in Checks) {
				string marker = "SPELL_CHOICE: " + check + "=";
				int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
				Assert.True(idx >= 0, "Check '" + check + "' never reported. Game output:\n" + output);
				Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0,
					"Check '" + check + "' failed. Game output:\n" + output);
			}
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NpcSpellChoiceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcSpellChoice";
	}
}
