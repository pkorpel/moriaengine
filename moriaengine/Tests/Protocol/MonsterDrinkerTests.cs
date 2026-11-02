using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - potions, berserk, disarming, battle summons, the vampire's leech and acid.</summary>
	[Collection(MonsterDrinkerCollection.Name)]
	public class MonsterDrinkerTests {
		private readonly GameServerPairFixture fixture;

		public MonsterDrinkerTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void TheyDoWhatEreborsDid() {
			using var client = CommandPermissionTests.Connect("drinkers", "Drink Tester");
			this.Ask(client, ".TestDrinkReady", "DRINK_READY:");

			//the first blow never counts
			Assert.Contains("first=100 second=433", this.Ask(client, ".TestDrinkHeal", "DRINK_HEAL:"));

			Assert.Contains("phys=900 fire=900 speed=5000 int=1000 drunk=True back=100,100,200,0", this.Ask(client, ".TestDrinkPotions", "DRINK_POTIONS:"));

			//two stages down the resists add up, and calming down gives it all back
			string berserk = this.Ask(client, ".TestDrinkBerserk", "DRINK_BERSERK:");
			Assert.Contains("stage=2 dex=350 speed=900 fire=400", berserk);
			Assert.Contains("stage=4 speed=6000 fire=-200", berserk);
			Assert.Contains("calm=1 dexBack=True speed=100 fire=500", berserk);

			Assert.Contains("held=True kept=True lost=True pack=True", this.Ask(client, ".TestDrinkKnockout", "DRINK_KNOCK:"));
			Assert.Contains("above=True", this.Ask(client, ".TestDrinkLeech", "DRINK_LEECH:"));
			Assert.Contains("more=True summoned=True", this.Ask(client, ".TestDrinkSummon", "DRINK_SUMMON:"));
			Assert.Contains("hurt=True", this.Ask(client, ".TestDrinkAcid", "DRINK_ACID:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MonsterDrinkerCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterDrinker";
	}
}
