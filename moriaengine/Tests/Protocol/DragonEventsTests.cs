using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - the dragons' events. Server side: scripts/Tests/DragonEventsTests.cs.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class DragonEventsTests {
		private readonly GameServerPairFixture fixture;

		public DragonEventsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"" + what + "\"", "DRAGON_TEST: " + what + " ");
		}

		[Fact]
		public void BreathBombSpinAurasCageBloodAndBrain() {
			using var client = CommandPermissionTests.Connect("dragontests", "Dragon Fighter");
			this.Do(client, "ready");
			//the breath: the brown dragon's own 120 from its .def, a strong tile, the black fire's stamina drain after
			string cone = this.Do(client, "cone");
			Assert.Contains("tuned=120 ", cone);
			Assert.True(!cone.Contains("lost=0 "), cone);
			Assert.Contains("lingering=4 breathAbility=True sees=True", cone);
			//a dragon resist above the damage: nothing
			Assert.Contains("lost=0 lingering=none", this.Do(client, "resist"));
			//the bomb on the player: cold fire
			string bomb = this.Do(client, "bomb");
			Assert.DoesNotContain("lost=0 ", bomb);
			Assert.Contains("lingering=2", bomb);
			//the spin: a turn clockwise to the east, the player burnt with no lingering effect, the dragon held still
			string spin = this.Do(client, "spin");
			Assert.Contains("facing=East ", spin);
			Assert.DoesNotContain("lost=0 ", spin);
			Assert.Contains("lingering=none frozen=True", spin);
			//the aura, then its first tick a second later
			Assert.Contains("reached=1 aura=True", this.Do(client, "aura"));
			Thread.Sleep(1500);
			string tick = this.Do(client, "auratick");
			Assert.DoesNotContain("lost=0 ", tick);
			Assert.DoesNotContain("stamLost=0 ", tick);
			//the cage: a mage can't slip out; fire east, the step refused; a click weakens it, the next one a second later puts it out
			Assert.Contains("caged=1 fire=True refused=True clicked=True standing=True", this.Do(client, "cage"));
			Thread.Sleep(1200);
			Assert.Contains("clicked=True gone=True open=True", this.Do(client, "cageclick"));
			//the blood: 3 puddles below 85%, 6 more past 65% and 45% at once
			Assert.Contains("first=3 later=9", this.Do(client, "blood"));
			//the brain's stamp: it rears up, a second later the ground shakes
			Assert.Contains("brain=True frozen=True", this.Do(client, "stamp"));
			Thread.Sleep(1500);
			string stamped = this.Do(client, "stamped");
			Assert.DoesNotContain("lost=0 ", stamped);
			Assert.Contains("paralysed=True", stamped);
		}
	}

	/// <summary>#1022 PR 2 - the summoning bosses.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class SummoningBossTests {
		private readonly GameServerPairFixture fixture;

		public SummoningBossTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestBoss \"" + what + "\"", "BOSS_TEST: " + what + " ");
		}

		[Fact]
		public void SummonsSightDeathsAndForms() {
			using var client = CommandPermissionTests.Connect("dragontests", "Boss Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			//the fire lord: columns, two phoenixes and an eye of its own; dying, it blasts whoever fought it
			string lord = this.Do(client, "firelord");
			Assert.Contains("phoenixes=2 eyes=1 columns=True", lord);
			Assert.True(!lord.Contains("deathBlast=0 "), lord);
			//the cajun: its three children and then a third one more around the attacker
			Assert.Contains("children=4 third=2", this.Do(client, "cajun"));
			//the elder drone: three drones, then home
			Assert.Contains("drones=3 home=True", this.Do(client, "drone"));
			//the serpent druid: snakes of its own; below half the serpent's look, and no more snakes
			Assert.Contains("snakes=3 serpent=True model=True snakesAfter=3", this.Do(client, "druid"));
			//SwARPUS: three worms on first sight, none the next look; poison shrugged off, a heal brings two
			Assert.Contains("sightThreeEach=True noneAgain=True poisonRefused=True healRefused=False spellWorms=2", this.Do(client, "worms"));
			//the guardian: a golem for a mage; dying, a blast, no body and no experience
			string guardian = this.Do(client, "guardian");
			Assert.Contains("golems=1 vanishes=True", guardian);
			Assert.Contains("exp=0", guardian);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class DragonEventsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DragonEvents";
	}
}
