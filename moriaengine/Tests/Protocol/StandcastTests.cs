using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1386 - Standcast as Erebor's lasting toggle doing its work in its plugin. Server side:
	/// scripts/Tests/StandcastTests.cs.</summary>
	[Collection(StandcastCollection.Name)]
	public class StandcastTests : LivingWorldTestBase {
		public StandcastTests(GameServerPairFixture servers)
			: base(servers, "standcast", "STANDCAST") {
		}

		[Fact]
		public void TheToggleFreezesBoostsAndEndsUnderTheShield() {
			using (TestGameClient game = this.Login("Stojici")) {
				this.Run(game, ".TestStandcastStart", "STANDCAST_STARTED");
				this.AssertChecks("offNoBonus", "toggleOn", "freezeOnCast", "stepRefused", "firstTargetOnly",
					"recastLongerPenalty", "recastAlmostDone", "recastEarly", "areaHalfEveryTarget");
				this.WaitForExplosion(game);
				this.Run(game, ".TestStandcastAfter", "STANDCAST_SECOND_CAST");
				this.AssertChecks("staysOn", "freezeEnded", "stepAllowed", "groundCastRuns", "groundNoFreeze", "groundNoBonus");
				this.WaitForExplosion(game); //the second one, without Standcast
				this.Run(game, ".TestStandcastPower", "STANDCAST_WALK_READY");
				this.AssertChecks("realCastBoosted", "shieldUp", "shieldEndsIt", "noToggleUnderShield", "toggleOff");

				game.SendMoveRequest(2, false, 0); //east, into the sheep, while frozen
				var (accepted, _, _, _) = game.ReadMoveResult(System.TimeSpan.FromSeconds(10));
				Assert.False(accepted, "A frozen caster's step was accepted.");
				this.Run(game, ".TestStandcastWalked", "STANDCAST_WALKED");
				this.AssertChecks("frozenNoShove");
				this.Run(game, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			}

			//the server's first account is an admin: a player's own .standcast needs a plain one
			PlainAccount plain = new PlainAccount(this.fixture);
			using (TestGameClient game = plain.LogIn("Prosty")) {
				plain.Do(game, ".TestStandcastPlain", "STANDCAST_WARRIOR");
				game.SendSpeech(".standcast");
				plain.Do(game, ".TestStandcastAfterWarrior", "STANDCAST_MAGE");
				game.SendSpeech(".standcast");
				plain.Do(game, ".TestStandcastAfterMage", "STANDCAST_DONE");
				this.AssertChecks("notForWarrior", "plainCommandOn");
				plain.Do(game, ".TestEnergyShieldPlain", "STANDCAST_SHIELD_PLAIN");
				game.SendSpeech(".energyshield");
				plain.Do(game, ".TestEnergyShieldPlainAfter", "STANDCAST_SHIELD_PLAIN_DONE");
				this.AssertChecks("plainCommandShield");
			}
		}

		/// <summary>#1401 - Erebor's energy shield. Server side: EnergyShieldTests in scripts/Tests/EnergyShieldTests.cs.</summary>
		[Fact]
		public void TheEnergyShieldIsErebors() {
			using (TestGameClient game = this.Login("Stitonos")) {
				this.Run(game, ".TestEnergyShield", "STANDCAST_SHIELD_DONE");
				this.AssertChecks("notForWarrior", "needsPoints", "sixtyManaNothing", "upForManaAndAmethyst", "energyAndDuration", "tableEnds",
					"recallRefused", "gateAllowed", "spellTwice", "monsterDamageOnce", "playerDamageWhole", "noResponseWhole", "curseAndBlowBackWhole", /*#1519*/ "rinaldAndFrostWhole", /*#1524*/ "standcastRefusedUnderIt",
					"manaShieldAfterEnergy");
				game.SendMoveRequest(2, false, 0); //east, shield up
				var (accepted, _, _, _) = game.ReadMoveResult(System.TimeSpan.FromSeconds(10));
				Assert.True(accepted, "A step with the energy shield up was refused.");
				this.Run(game, ".TestEnergyShieldStepped", "STANDCAST_SHIELD_STEPPED");
				this.AssertChecks("stepKeepsIt", "partialAbsorb");
				Thread.Sleep(1500);
				this.Run(game, ".TestEnergyShieldRest", "STANDCAST_SHIELD_RESTING");
				this.AssertChecks("restsAfterTheTick", "restSpellWhole", "noNewWhileResting", "recallInRest", "standcastInRest", "cancelRests");
				Thread.Sleep(2500);
				this.Run(game, ".TestEnergyShieldAfter", "STANDCAST_SHIELD_AFTER");
				this.AssertChecks("restEnds");
			}
		}

		/// <summary>#1391 - casting Gate Travel freezes a player as Erebor's e_allplayers. Server side: GateFreezeTests in
		/// scripts/Tests/StandcastTests.cs.</summary>
		[Fact]
		public void CastingAGateFreezesThePlayer() {
			using (TestGameClient game = this.Login("Brankar")) {
				this.Run(game, ".TestGateFreeze", "STANDCAST_GATE_DONE");
				this.AssertChecks("refusedNoFreeze", "gateFreezes", "gateEarlyRecast", "gateLongLeft", "gateWithStandcast", "denyOutNoFreeze", "npcNotFrozen");
			}
		}

		/// <summary>The explosion landed (cast again by the server after a fizzle) and its freeze is over.</summary>
		private void WaitForExplosion(TestGameClient game) {
			for (int i = 0; i < 40; i++) {
				this.Run(game, ".TestStandcastPowerReady", "STANDCAST_POWER_");
				if (this.LastLine("STANDCAST_POWER_").StartsWith("STANDCAST_POWER_READY", System.StringComparison.Ordinal)) {
					return;
				}
				Thread.Sleep(500);
			}
			Assert.Fail("The explosion never landed. Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private sealed class PlainAccount : LivingWorldTestBase {
			internal PlainAccount(GameServerPairFixture servers)
				: base(servers, "standcastplain", "STANDCAST") {
			}

			internal TestGameClient LogIn(string name) {
				return this.Login(name);
			}

			internal void Do(TestGameClient game, string command, string doneMarker) {
				this.Run(game, command, doneMarker);
			}
		}
	}

	[CollectionDefinition(Name)]
	public class StandcastCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Standcast";
	}
}
