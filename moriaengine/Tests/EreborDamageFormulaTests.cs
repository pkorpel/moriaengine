using System;
using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #860 - Erebor's swing formulas (combat.scp), hand-worked from its integer maths.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class EreborDamageFormulaTests {
		[Theory]
		[InlineData(49, 40, 250, 1.0, 1.35, 26)] //iron longsword, armor 40: armor counts 30
		[InlineData(75, 80, 250, 1.0, 1.35, 20)] //mithril longsword, a titan
		[InlineData(49, 80, 250, 1.0, 1.35, 0)]  //armor above the attack, never a heal
		[InlineData(49, 40, 250, 1.05, 1.35, 24)] //(31500) -> koef 18, the high armor roll
		[InlineData(50, 28, 0, 1.0, 0.9, 20)]    //PvP
		public void PlayerSwing(double attack, double armor, double piercing, double roll, double damageMod, double expected) {
			Assert.Equal(expected, CombatCalculator.CalculateDamageCust(attack, armor, piercing, roll, damageMod));
		}

		[Theory]
		[InlineData(80, 65, 0, 1.0, 54)]    //titan vs full iron plate: armor takes 32.5%
		[InlineData(80, 65, 0, 0.85, 46)]   //the low random factor
		[InlineData(20, 65, 0, 1.0, 14)]
		[InlineData(80, 65, 500, 1.0, 67)]  //piercing halves the armor
		[InlineData(80, 200, 0, 1.0, 0)]    //armor 200 stops everything
		[InlineData(80, 300, 0, 1.0, 0)]    //and more doesn't heal
		public void MonsterHitsPlayer(double attack, double armor, double piercing, double random, double expected) {
			Assert.Equal(expected, CombatCalculator.CalculateDamageMvP(attack, armor, piercing, 200, random, 1.0));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(100, 1000)]
		[InlineData(50, 707)]
		public void SqrtIntIsAHundredTimesTheRoot(int value, int expected) {
			Assert.InRange(CombatCalculator.SqrtInt(value), expected - 1, expected + 1);
		}

		[Theory]
		[InlineData(998, 76, 2.6)]   //Dex 100, longsword: hitspeed 13
		[InlineData(998, 200, 1.0)]  //Dex 100, bare hands: 5
		[InlineData(1367, 150, 1.0)] //a titan: 5
		[InlineData(547, 150, 2.4)]  //stamina 60: 12
		[InlineData(707, 60, 4.8)]   //Mystic with 100 mana: 24
		public void SwingTakesTwoHitspeeds(int root, double weaponSpeed, double seconds) {
			Assert.Equal(seconds, CombatCalculator.CalculateSwingDelay(root, weaponSpeed, 1.0, 10).TotalSeconds, 6);
		}

		[Fact]
		public void ZeroSpeedFallsBackToFifty() {
			Assert.Equal(2 * (0xfffff / 50) / 10.0, CombatCalculator.CalculateSwingDelay(0, 76, 1.0, 10).TotalSeconds, 6);
		}
	}
}
