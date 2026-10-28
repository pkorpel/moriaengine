using System;
using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #791 - Erebor's creaturestart gold and magic-item formulas (MonsterLoot), without a server.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class MonsterLootFormulaTests {
		[Theory]
		[InlineData(180, false, 135)] //75% outside dungeons
		[InlineData(180, true, 162)]  //90% in dungeons
		[InlineData(12, false, 9)]    //c_orc's low end
		[InlineData(1, false, 0)]
		[InlineData(0, false, 0)]
		[InlineData(-5, true, 0)]
		public void GoldIsAShareOfTheExperienceReward(int experience, bool dungeon, int expectedGold) {
			Assert.Equal(expectedGold, MonsterLoot.GoldFromExperience(experience, dungeon, 750, 900));
		}

		[Theory]
		[InlineData(0, false)]
		[InlineData(1, false)]
		[InlineData(2, true)]
		[InlineData(9, false)]
		[InlineData(10, true)]
		[InlineData(15, true)]
		[InlineData(16, false)]
		public void DungeonsAreMapPlanes2And10To15(byte m, bool dungeon) {
			Assert.Equal(dungeon, MonsterLoot.IsDungeonMapPlane(m));
		}

		[Fact]
		public void NoMagicItemAtOrBelowTheMinimumGold() {
			LootSettings settings = new LootSettings();
			Random random = new Random(1);
			for (int i = 0; i < 1000; i++) {
				Assert.False(MonsterLoot.RollMagicItem(30, settings, random));
			}
		}

		[Fact]
		public void MagicItemChanceGrowsWithGold() {
			LootSettings settings = new LootSettings();
			Random random = new Random(791);
			int poor = 0, rich = 0;
			for (int i = 0; i < 200000; i++) {
				if (MonsterLoot.RollMagicItem(300, settings, random)) {
					poor++;
				}
				if (MonsterLoot.RollMagicItem(6000, settings, random)) {
					rich++;
				}
			}
			//(300-30)/(120000-30) ~ 0.225% -> ~450 of 200000; (6000-30)/(105000-30) ~ 5.7% -> ~11400
			Assert.InRange(poor, 300, 600);
			Assert.InRange(rich, 10600, 12200);
		}

		[Fact]
		public void RollInclusiveIncludesBothEnds() {
			Random random = new Random(3);
			bool sawMin = false, sawMax = false;
			for (int i = 0; i < 2000; i++) {
				int r = MonsterLoot.RollInclusive(random, 1, 3);
				Assert.InRange(r, 1, 3);
				sawMin |= r == 1;
				sawMax |= r == 3;
			}
			Assert.True(sawMin && sawMax);
			Assert.Equal(5, MonsterLoot.RollInclusive(random, 5, 5));
		}
	}
}
