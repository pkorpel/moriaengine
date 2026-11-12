using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1370 - Thing.CancellableTrigger called itself instead of its base: any call on an item or a character overflowed
	/// the stack and took the server down. Now a trigger group's 1 cancels it and its absence lets it continue. Server side:
	/// scripts/Tests/CancellableTriggerTests.cs.</summary>
	[Collection(CancellableTriggerCollection.Name)]
	public class CancellableTriggerTests : LivingWorldTestBase {
		public CancellableTriggerTests(GameServerPairFixture servers)
			: base(servers, "cancellabletests", "CANCELLABLE") {
		}

		[Fact]
		public void ACancellableTriggerOnAThingAsksItsGroupsAndReturns() {
			using (TestGameClient game = this.Login("Rusitel")) {
				this.Run(game, ".TestCancellableTrigger", "CANCELLABLE_TRIGGER:");
				string line = this.LastLine("CANCELLABLE_TRIGGER:");
				Assert.Contains("dagger=Continue/Cancel", line);
				Assert.Contains("rabbit=Continue/Cancel", line);
			}
		}

		/// <summary>#1463 - a cancellable @denyDeath at the top of CauseDeath. Server side: TestDenyDeath in
		/// scripts/Tests/CancellableTriggerTests.cs. The account's fifth character: the next Fact needs another account.</summary>
		[Fact]
		public void DenyDeathKeepsACharacterAlive() {
			using (TestGameClient game = this.Login("Umirajici")) {
				this.Run(game, ".TestDenyDeath", "CANCELLABLE_DENYDEATH:");
				string line = this.LastLine("CANCELLABLE_DENYDEATH:");
				Assert.True(line.Contains("kept=True asked=True killer=True noDeath=True beforeAnyStep=True diesWithout=True"), line + "\n" + this.fixture.GameOutputSnapshot());
			}
		}

		/// <summary>#1372 - an item's and an account's Trigger and CancellableTrigger let a trigger group's exception through;
		/// their Try versions don't. Server side: TestThrowingTrigger in scripts/Tests/CancellableTriggerTests.cs.</summary>
		[Fact]
		public void OnlyTheTryTriggersSwallowAnException() {
			using (TestGameClient game = this.Login("Hazejici")) {
				this.Run(game, ".TestThrowingTrigger", "CANCELLABLE_THROW:");
				string line = this.LastLine("CANCELLABLE_THROW:");
				Assert.True(line.Contains("itemTrigger=True itemCancellable=True itemTry=False itemTryCancellable=False"), line + "\n" + this.fixture.GameOutputSnapshot());
				Assert.True(line.Contains("accountTrigger=True accountCancellable=True accountTry=False accountTryCancellable=False"), line + "\n" + this.fixture.GameOutputSnapshot());
			}
		}

		/// <summary>#1387 - a plugin removing itself mid-trigger no longer stops the trigger for the plugins after it.
		/// Server side: scripts/Tests/PluginLoopTests.cs.</summary>
		[Fact]
		public void APluginRemovingItselfLetsTheOthersHearTheTrigger() {
			using (TestGameClient game = this.Login("Odchazejici")) {
				this.Run(game, ".TestPluginLoop", "CANCELLABLE_LOOP_DONE");
				foreach (string way in new[] { "Trigger", "TryTrigger", "CancellableTrigger", "TryCancellableTrigger" }) {
					this.AssertChecks("selfRemoved" + way, "otherRemoved" + way, "selfAndNextRemoved" + way, "readdedForgetsItsOldNext" + way); //#1468
				}
				this.AssertChecks("talentOutlivesDeath", "effectEndsAtDeath", "deletePluginsLeavesAMovedOne", "deletePluginsReaddedGoes", "deletePluginsAddedGoes"); //#1479
			}
		}

		/// <summary>#1389 - a trigger group removed during a trigger doesn't run, nor do the groups of a holder deleted
		/// during it. Server side: TriggerGroupLoopTests in scripts/Tests/PluginLoopTests.cs.</summary>
		[Fact]
		public void TriggerGroupsRemovedDuringATriggerDontRun() {
			using (TestGameClient game = this.Login("Odebirany")) {
				this.Run(game, ".TestTriggerGroupLoop", "CANCELLABLE_TGLOOP_DONE");
				foreach (string way in new[] { "Trigger", "TryTrigger", "CancellableTrigger", "TryCancellableTrigger" }) {
					this.AssertChecks("tgSelfRemoved" + way, "tgBothRemoved" + way);
				}
				this.AssertChecks("tgHolderDeletedTryTrigger", "tgHolderDeletedCancellableTrigger", "tgHolderDeletedTryCancellableTrigger");
				this.AssertChecks("dupeGroupsFound", "dupeFirstGroupRemoved", "dupeReaddedRunsOnce"); //#1394
			}
		}
	}

	[CollectionDefinition(Name)]
	public class CancellableTriggerCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CancellableTrigger";
	}
}
