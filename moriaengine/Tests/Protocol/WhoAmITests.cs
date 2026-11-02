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

		private static List<string> Four(TestGameClient client) {
			client.SendSpeech(".whoami");
			DateTime deadline = DateTime.UtcNow + Wait;
			string line;
			do {
				line = client.ReadSystemMessage(deadline - DateTime.UtcNow);
			} while (!line.StartsWith("Jsem ", StringComparison.Ordinal));
			return new List<string> { line, client.ReadSystemMessage(Wait), client.ReadSystemMessage(Wait), client.ReadSystemMessage(Wait) };
		}

		[Fact]
		public void ItTellsWhoYouAreAndKeepsCount() {
			using var client = CommandPermissionTests.Connect("whotest", "Who Tester");
			string ready = this.Ask(client, ".TestWhoReady", "WHO_READY:");
			this.Ask(client, ".TestWhoPlayer", "WHO_PLAYER:");

			//a player can ask, and a new character's record is clean
			List<string> lines = Four(client);
			Assert.StartsWith("Jsem Mag Who Tester of Rohan, ", lines[0]);
			Assert.Equal("Premohl jsem 0 nepratel a 0 monster.", lines[1]);
			Assert.Equal("Ja sam jsem byl porazen 0krat.", lines[2]);
			Assert.Equal("Ziji jiz 0 dni.", lines[3]);

			this.Ask(client, ".TestWhoKillRabbit", "WHO_KILLED:");
			this.Ask(client, ".TestWhoKillRabbit", "WHO_KILLED:");
			Assert.Contains("dead=True", this.Ask(client, ".TestWhoDie", "WHO_DIED:"));
			this.Ask(client, ".TestWhoAge \"2\"", "WHO_AGED:");
			lines = Four(client);
			Assert.Equal("Premohl jsem 0 nepratel a 2 monster.", lines[1]);
			Assert.Equal("Ja sam jsem byl porazen 1krat.", lines[2]);
			Assert.Equal("Ziji jiz 2 dni.", lines[3]);

			Assert.Contains("True", this.Ask(client, ".TestWhoFemale", "WHO_FEMALE:"));
			lines = Four(client);
			Assert.Equal("Premohla jsem 0 nepratel a 2 monster.", lines[1]);
			Assert.Equal("Ja sama jsem byla porazena 1krat.", lines[2]);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class WhoAmICollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WhoAmI";
	}
}
