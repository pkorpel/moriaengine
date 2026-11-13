using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1409 - the movement holds are plugins refusing @denyMove; a frozen player's real step is refused and taken
	/// once unfrozen. Server side: scripts/Tests/MovementHoldsTests.cs.</summary>
	[Collection(MovementHoldsCollection.Name)]
	public class MovementHoldsTests : LivingWorldTestBase {
		public MovementHoldsTests(GameServerPairFixture servers)
			: base(servers, "movementholds", "HOLDS") {
		}

		[Fact]
		public void TheHoldsArePluginsRefusingTheStep() {
			using (TestGameClient game = this.Login("Drzeny")) {
				this.Run(game, ".TestHoldsFreeze", "HOLDS_FROZEN");
				this.AssertChecks("frozenIsAPlugin");
				game.SendMoveRequest(2, false, 0); //east
				var (accepted, _, _, _) = game.ReadMoveResult(TimeSpan.FromSeconds(10));
				Assert.False(accepted, "A frozen player's step was taken.");
				this.Run(game, ".TestHoldsUnfreeze", "HOLDS_UNFROZEN");
				this.AssertChecks("unfrozen");
				game.SendMoveRequest(2, false, 0); //a refused step starts the sequence again
				var (taken, _, _, _) = game.ReadMoveResult(TimeSpan.FromSeconds(10));
				Assert.True(taken, "An unfrozen player's step was refused.");

				this.Run(game, ".TestHoldsOthers", "HOLDS_OTHERS");
				this.AssertChecks("immobile", "lootHeld", "entangled");
				Thread.Sleep(1500);
				this.Run(game, ".TestHoldsAfter", "HOLDS_DONE");
				this.AssertChecks("lootHoldRanOut", "entangledHolds", "blowsFree", "leavingTheTileFrees", "spellsFreeAfterSix");
				this.Run(game, ".TestHitTriggers", "HOLDS_HIT_DONE"); //#1411
				this.AssertChecks("hitThenGetHit", "cancelledHit");
			}
		}

		/// <summary>#1414 - @stepTaken after a real step, heard by trigger groups and plugins.</summary>
		[Fact]
		public void AStepTakenIsATrigger() {
			using (TestGameClient game = this.Login("Krokovy")) {
				this.Run(game, ".TestStepTaken", "HOLDS_STEP_DONE");
				this.AssertChecks("stepTakenWalk", "stepTakenRun", "teleportNotAStep", "gmStepNotRaised", "deletedByItsStep");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class MovementHoldsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MovementHolds";
	}
}
