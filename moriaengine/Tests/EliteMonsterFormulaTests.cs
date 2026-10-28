using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #794 - Erebor's elite-monster formulas (EliteNPC.scp), without a server.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class EliteMonsterFormulaTests {
		[Theory]
		[InlineData(1000, 200)]  //2%
		[InlineData(1500, 300)]
		[InlineData(2500, 500)]  //5%, the cap
		[InlineData(9000, 500)]
		public void SpawnChanceIsAShareOfExperienceUpToFivePercent(int experience, int per10000) {
			Assert.Equal(per10000, EliteMonster.SpawnChance(experience, new EliteSettings()));
		}

		[Theory]
		[InlineData(1000, 500)]
		[InlineData(1100, 526)]  //500 + 100*2667/10000
		[InlineData(1500, 633)]
		[InlineData(2500, 900)]  //cap
		[InlineData(5000, 900)]
		public void PowerGrowsWithExperience(int experience, int power) {
			Assert.Equal(power, EliteMonster.PowerFromExperience(experience, new EliteSettings()));
		}

		[Theory]
		[InlineData(500, 300)]
		[InlineData(700, 400)]
		[InlineData(900, 500)]
		public void ExperienceBonusIs300To500Percent(int power, int percent) {
			Assert.Equal(percent, EliteMonster.ExpBonusPercent(power, new EliteSettings()));
		}

		[Theory]
		[InlineData(60, 0)]
		[InlineData(199, 0)]
		[InlineData(200, 200)]
		[InlineData(250, 450)]
		[InlineData(400, 1200)]
		[InlineData(800, 1200)]  //counted up to level 400
		public void LevelBonusOnlyFromLevel200(int level, int bonus) {
			Assert.Equal(bonus, EliteMonster.LevelBonus(level, new EliteSettings()));
		}

		[Theory]
		[InlineData(100, 900, 0, 10)]   //absorbs 90%
		[InlineData(100, 500, 0, 50)]
		[InlineData(100, 900, 450, 55)]
		[InlineData(100, 900, 1200, 100)] //a bonus above the power absorbs nothing, never heals
		public void ElitesAbsorbPowerPermilleOfAPlayersSwing(double damage, int power, int bonus, double expected) {
			Assert.Equal(expected, EliteMonster.AbsorbedDamage(damage, power, bonus), 6);
		}
	}
}
