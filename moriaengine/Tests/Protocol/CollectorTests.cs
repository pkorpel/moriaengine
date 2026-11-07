using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1247 - Erebor's t_collector bag: a converted bag reads its settings from its def, a player double clicks it, is
	/// asked what to aim at, aims at a body and the count goes up; then the rules on the server (a fail, a kept body, repeated
	/// collecting, the wrong body, a player's body, a full bag, the colour, a collection source). Server side:
	/// scripts/Tests/CollectorTests.cs.</summary>
	[Collection(CollectorCollection.Name)]
	public class CollectorTests : LivingWorldTestBase {
		public CollectorTests(GameServerPairFixture servers)
			: base(servers, "collectortests", "COLLECTOR") {
		}

		[Fact]
		public void ABagCountsWhatItsOwnerTakes() {
			using (TestGameClient game = this.Login("Sberac")) {
				this.Run(game, ".TestCollectorSetup", "COLLECTOR_SETUP_DONE");
				this.AssertChecks("theConverterMadeACollector", "itsSettingsComeFromTheDef", "aClickTellsTheCount");
				string targets = this.LastLine("COLLECTOR_TARGETS:");
				uint bag = (uint) Number(targets, "bag");
				uint corpse = (uint) Number(targets, "corpse");

				//the player's own double click: the bag asks what to aim at, the cursor takes the body
				game.SendDoubleClick(bag);
				Assert.NotNull(ReadMessageContaining(game, "Kteremu grizlimu seberes stehno?"));
				game.SendTargetUid(corpse);
				Assert.NotNull(ReadMessageContaining(game, "Ve vacku je 1 stehen grizliho"));
				this.Run(game, ".TestCollectorAfterTarget", "COLLECTOR_AFTER_DONE");
				this.AssertChecks("aimingAtTheBodyCounts", "theBodyIsUsedUp");

				this.Run(game, ".TestCollectorRules", "COLLECTOR_RULES_DONE");
				this.AssertChecks("aFailCountsNothingAndSpendsTheBody", "aKeptBodyIsMarkedTakenOnce", "aRepeatedOneCanBeTakenAgain",
					"theWrongBodyIsRefused", "aPlayersBodySpoilsTheBag", "aFullBagTakesNoMore", "onlyTheRightColourCounts",
					"aSourceGivesThenWaitsThenEmpties", "aPlayerIsNeverRemoved", "aFailedItemIsMarkedEvenWhenRepeated", "questsFindTheBag");
			}
		}

		private static string ReadMessageContaining(TestGameClient game, string text) {
			DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
			while (true) {
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero) {
					return null;
				}
				string message;
				try {
					message = game.ReadSystemMessage(remaining);
				} catch (TimeoutException) {
					return null;
				}
				if (message.Contains(text, StringComparison.Ordinal)) {
					return message;
				}
			}
		}
	}

	[CollectionDefinition(Name)]
	public class CollectorCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Collector";
	}
}
