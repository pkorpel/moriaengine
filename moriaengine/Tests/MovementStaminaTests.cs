using Xunit;
using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	/// <summary>
	/// #779 - Sphere 0.99z8's walking stamina math (EreborNext Calc_DropStamWhileMoving /
	/// Calc_GetSCurve / Calc_GetBellCurve / Calc_MaxCarryWeight), and #778's strangler-vine hold rules.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class MovementStaminaTests {
		[Fact]
		public void SCurveMatchesSpheresDocumentedExample() {
			//sphere.ini: "If this is set to 100 ... the chance at 90% load is 25%"
			Assert.Equal(250, MovementStamina.StaminaLossChance(90, false, 100, 50));
			Assert.Equal(500, MovementStamina.StaminaLossChance(100, false, 100, 50));
			Assert.Equal(750, MovementStamina.StaminaLossChance(110, false, 100, 50));
		}

		[Fact]
		public void LightLoadAlmostNeverCostsStaminaHeavyLoadAlmostAlways() {
			Assert.True(MovementStamina.StaminaLossChance(20, false, 150, 50) <= 1);
			Assert.True(MovementStamina.StaminaLossChance(250, false, 150, 50) >= 999);
		}

		[Fact]
		public void RunningCountsAsHeavierLoad() {
			Assert.Equal(MovementStamina.StaminaLossChance(150, false, 150, 50),
				MovementStamina.StaminaLossChance(100, true, 150, 50));
			Assert.True(MovementStamina.StaminaLossChance(100, true, 150, 50) > MovementStamina.StaminaLossChance(100, false, 150, 50));
		}

		[Theory]
		[InlineData(0, 500)]
		[InlineData(5, 375)]
		[InlineData(-5, 375)]
		[InlineData(10, 250)]
		[InlineData(15, 188)]
		[InlineData(20, 125)]
		[InlineData(1000, 0)]
		public void BellCurveHalvesEveryVariance(int diff, int expected) {
			Assert.Equal(expected, MovementStamina.BellCurve(diff, 10));
		}

		[Fact]
		public void SCurveIsMonotonic() {
			int previous = -1;
			for (int diff = -100; diff <= 100; diff++) {
				int chance = MovementStamina.SCurve(diff, 10);
				Assert.True(chance >= previous, "S-curve dropped at " + diff);
				Assert.InRange(chance, 0, 1000);
				previous = chance;
			}
		}

		[Fact]
		public void MaxCarryWeightIsFourStonesPerStrPlusThirty() {
			Assert.Equal(430, MovementStamina.MaxCarryWeight(100));
			Assert.Equal(30, MovementStamina.MaxCarryWeight(0));
			Assert.Equal(30, MovementStamina.MaxCarryWeight(-5));
		}

		[Fact]
		public void LoadPercentIsCarriedOverMax() {
			Assert.Equal(100, MovementStamina.LoadPercent(430, 100));
			Assert.Equal(201, MovementStamina.LoadPercent(865, 100)); //just over the 200% cap - can't move
			Assert.Equal(0, MovementStamina.LoadPercent(0, 100));
		}

		[Fact]
		public void VineLetsGoAfterItsHitsRunOut() {
			int hits = 5;
			for (int i = 0; i < 4; i++) {
				hits = MonsterSpecialAttacks.HitsLeftAfterHit(hits);
				Assert.True(hits > 0);
			}
			Assert.Equal(0, MonsterSpecialAttacks.HitsLeftAfterHit(hits));
			Assert.Equal(0, MonsterSpecialAttacks.HitsLeftAfterHit(0));
		}

		[Theory]
		[InlineData(true, true, 5, 5, 0, 5, 5, 0, true)]
		[InlineData(false, true, 5, 5, 0, 5, 5, 0, false)] //vine died
		[InlineData(true, false, 5, 5, 0, 5, 5, 0, false)] //victim died
		[InlineData(true, true, 5, 5, 0, 6, 5, 0, false)]  //victim was moved off the vine (teleport, knockback)
		[InlineData(true, true, 5, 5, 0, 5, 5, 1, false)]  //different map
		public void HoldLastsWhileBothAliveOnTheSameTile(bool vineAlive, bool victimAlive, int vx, int vy, int vm, int tx, int ty, int tm, bool expected) {
			Assert.Equal(expected, MonsterSpecialAttacks.IsHoldIntact(vineAlive, victimAlive, vx, vy, vm, tx, ty, tm));
		}
	}
}
