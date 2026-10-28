using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#797 - e_npc_magicshield's absorb formula (MonsterSpecialAttacks.AbsorbedByMagicShield).</summary>
	public class MagicShieldFormulaTests {
		[Fact]
		public void NonMysticSwingLosesTheShieldPercent() {
			Assert.Equal(90, MonsterSpecialAttacks.AbsorbedByMagicShield(100, 90, 0, 200), 6);
			Assert.Equal(60, MonsterSpecialAttacks.AbsorbedByMagicShield(100, 60, 0, 200), 6);
		}

		[Fact]
		public void MysticLevelPiercesTheShieldProportionally() {
			//archive: shieldForce - shieldForce * level / 200
			Assert.Equal(45, MonsterSpecialAttacks.AbsorbedByMagicShield(100, 90, 100, 200), 6);
			Assert.Equal(0, MonsterSpecialAttacks.AbsorbedByMagicShield(100, 90, 200, 200), 6);
			Assert.Equal(0, MonsterSpecialAttacks.AbsorbedByMagicShield(100, 90, 350, 200), 6); //capped at 200
		}

		[Fact]
		public void StrengthIsClampedToZeroToHundredPercent() {
			Assert.Equal(100, MonsterSpecialAttacks.AbsorbedByMagicShield(100, 150, 0, 200), 6);
			Assert.Equal(0, MonsterSpecialAttacks.AbsorbedByMagicShield(100, -10, 0, 200), 6);
		}
	}
}
