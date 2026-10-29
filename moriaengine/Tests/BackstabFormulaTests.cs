using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#799 - e_backstub's ambush bonus (MonsterSpecialAttacks.BackstabBonus).</summary>
	public class BackstabFormulaTests {
		[Fact]
		public void ArchiveFormulaIsThreeFifthsOfTheMissingHits() {
			Assert.Equal(540, MonsterSpecialAttacks.BackstabBonus(1000, 100, 600, 1000), 6);
		}

		[Fact]
		public void DamageReducerScalesIt() {
			Assert.Equal(270, MonsterSpecialAttacks.BackstabBonus(1000, 100, 600, 500), 6); //tag.backstub_damageReducer=500
		}

		[Fact]
		public void NeverNegativeWhenTheHitAlreadyExceedsMaxHits() {
			Assert.Equal(0, MonsterSpecialAttacks.BackstabBonus(100, 150, 600, 1000), 6);
		}
	}
}
