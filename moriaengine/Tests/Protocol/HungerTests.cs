using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#924 - players hunger as in Sphere, and Erebor's hunger gets in their way.</summary>
	[Collection(HungerCollection.Name)]
	public class HungerTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public HungerTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void WaitForMessage(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		private static int Number(string report, string name) {
			return int.Parse(Regex.Match(report, name + @"=(-?\d+)").Groups[1].Value);
		}

		[Fact]
		public void PlayersHungerEatAndSuffer() {
			using var client = CommandPermissionTests.Connect("hungertest", "Hunger Tester");
			Assert.Contains("food=15", this.Ask(client, ".TestHungerFood \"\"", "HUNGER_FOOD:")); //a new character is full

			string ready = this.Ask(client, ".TestHungerReady \"15\"", "HUNGER_READY:");
			uint fish = (uint) long.Parse(Regex.Match(ready, @"fish=(\d+)").Groups[1].Value);
			client.SendDoubleClick((uint) long.Parse(Regex.Match(ready, @"pack=(\d+)").Groups[1].Value));
			Thread.Sleep(300);

			client.SendDoubleClick(fish);
			WaitForMessage(client, "too full");
			this.Ask(client, ".TestHungerFood \"0\"", "HUNGER_FOOD:");
			client.SendDoubleClick(fish);
			WaitForMessage(client, "extremely hungry");
			Assert.Contains("food=1 ", this.Ask(client, ".TestHungerFood \"\"", "HUNGER_FOOD:") + " ");

			//at 7 hunger is no bother, at none it is 7 times in 10
			Assert.Contains("hungry=0", this.Ask(client, ".TestHungerRolls \"7\"", "HUNGER_ROLLS:"));
			int rolls = Number(this.Ask(client, ".TestHungerRolls \"0\"", "HUNGER_ROLLS:"), "hungry");
			Assert.InRange(rolls, 600, 800);
			string skills = this.Ask(client, ".TestHungerHiding", "HUNGER_HIDING:");
			Assert.InRange(Number(skills, "spoke"), 10, 55); //a starving player's skill succeeds 3 times in 10
			Assert.InRange(Number(skills, "hidden"), 85, 100); //#1095 - but hiding takes no notice of hunger, as on Erebor: 1000 against rand(1025)

			//no regeneration on nothing, it comes back with a meal
			this.Ask(client, ".TestHungerFood \"0\"", "HUNGER_FOOD:");
			int hurt = Number(this.Ask(client, ".TestHungerHurtMe", "HUNGER_HITS:"), "hits");
			Thread.Sleep(1500);
			Assert.Equal(hurt, Number(this.Ask(client, ".TestHungerHits", "HUNGER_HITS:"), "hits"));
			client.SendDoubleClick(fish);
			WaitForMessage(client, "extremely hungry");
			Thread.Sleep(1500);
			Assert.True(Number(this.Ask(client, ".TestHungerHits", "HUNGER_HITS:"), "hits") > hurt);

			string swing = this.Ask(client, ".TestHungerSwing", "HUNGER_SWING:");
			Assert.Contains("lost=10 ", swing); //drawing the blow
			Assert.Contains("landedLost=0", swing); //#1396 - not again when it lands
			//#1396 - a plugin on the player, and a starving cast hurts through its @spellCast
			string plugin = this.Ask(client, ".TestHungerPlugin", "HUNGER_PLUGIN:");
			Assert.Contains("onPlugin=True", plugin);
			Assert.Contains("castLost=10", plugin);

			//the clock, and what it says under 40%
			this.Ask(client, ".TestHungerFood \"6\"", "HUNGER_FOOD:");
			this.Ask(client, ".TestHungerFast", "HUNGER_FAST:");
			WaitForMessage(client, "You are hungry|You are very hungry");
			Assert.Contains("Jidlo=", this.Ask(client, ".TestHungerSlow", "HUNGER_SLOW:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HungerCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Hunger";
	}
}
