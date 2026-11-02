using System;
using System.Collections.Generic;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1011 - Erebor's .whoami.</summary>
	[Collection(WhoAmICollection.Name)]
	public class WhoAmITests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public WhoAmITests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static List<string> Open(TestGameClient client) {
			client.SendSpeech(".whoami");
			return client.ReadGumpDialogWithStrings(Wait).Strings;
		}

		[Fact]
		public void ItTellsWhoYouAreAndKeepsCount() {
			using var client = CommandPermissionTests.Connect("whotest", "Who Tester");
			this.Ask(client, ".TestWhoReady", "WHO_READY:");
			this.Ask(client, ".TestWhoPlayer", "WHO_PLAYER:");

			//a player can ask, and a new character's record is clean
			List<string> lines = Open(client);
			Assert.Contains("Mag Who Tester of Rohan", lines);
			Assert.Contains("Gondor", lines);
			Assert.Contains("0krat", lines);
			Assert.Contains("0 dni", lines);
			Assert.Contains("0.00", lines);
			Assert.DoesNotContain("Prsten moci", lines);
			Assert.DoesNotContain("Naposledy zabit od", lines);
			Assert.DoesNotContain("Funkce", lines);

			this.Ask(client, ".TestWhoKillRabbit", "WHO_KILLED:");
			this.Ask(client, ".TestWhoKillRabbit", "WHO_KILLED:");
			Assert.Contains("dead=True", this.Ask(client, ".TestWhoDie", "WHO_DIED:"));
			this.Ask(client, ".TestWhoAge \"2\"", "WHO_AGED:");
			this.Ask(client, ".TestWhoPlayed \"7500\"", "WHO_PLAYED:");
			lines = Open(client);
			Assert.Contains("2", lines);
			Assert.Contains("1krat", lines);
			Assert.Contains("2 dni", lines);
			Assert.Contains("2 hod 5 min", lines);
			Assert.Contains("Killer Rabbit", lines);

			//level, rank and guild, each with its label
			this.Ask(client, ".TestWhoGuild", "WHO_GUILD:");
			lines = Open(client);
			Assert.Contains("Uroven", lines);
			Assert.Contains("12", lines);
			Assert.Contains("Hodnost", lines);
			Assert.Contains("Master", lines);
			Assert.Contains("Guilda", lines);
			Assert.Contains("Who Guild", lines);

			//a ring's progress, for Mordor
			this.Ask(client, ".TestWhoMordor", "WHO_MORDOR:");
			Assert.Contains("0 z 5 zabiti za poslednich 3 dni", Open(client));

			//the First Nazgul has an office and no more use for the quest
			this.Ask(client, ".TestWhoFirst", "WHO_FIRST:");
			lines = Open(client);
			Assert.Contains("Prvni Nazgul", lines);
			Assert.DoesNotContain("Prsten moci", lines);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class WhoAmICollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WhoAmI";
	}
}
