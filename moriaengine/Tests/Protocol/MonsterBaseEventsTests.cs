using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - what e_character and e_monster did that wasn't here.</summary>
	[Collection(MonsterBaseEventsCollection.Name)]
	public class MonsterBaseEventsTests {
		private readonly GameServerPairFixture fixture;

		public MonsterBaseEventsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void BaseEvents() {
			using var client = CommandPermissionTests.Connect("basetest", "Base Tester");

			//a monster's body goes by its kind, not its own name
			string corpse = this.Ask(client, ".TestBaseCorpse", "BASE_CORPSE:");
			string kind = Regex.Match(corpse, @"kind=(.*?)\|").Groups[1].Value;
			Assert.Contains("corpse=Body of " + kind + "|", corpse);

			Assert.Contains("summonGone=True plainAlive=True plainInvis=True", this.Ask(client, ".TestBaseInvisibility", "BASE_INVIS:"));
			Assert.Contains("on=True off=False", this.Ask(client, ".TestBaseTurn", "BASE_TURN:"));
			Assert.Contains("monsterRefused=True petRefused=False playerRefused=False", this.Ask(client, ".TestBaseTelepad", "BASE_PAD:"));

			//a GM clicking sees what's on someone
			string click = this.Ask(client, ".TestBaseClickTarget", "BASE_CLICK:");
			client.SendSingleClick(uint.Parse(Regex.Match(click, @"uid=#(\d+)").Groups[1].Value));
			DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
			string line;
			do {
				line = client.ReadSystemMessage(deadline - DateTime.UtcNow);
			} while (!line.Contains("[hidden]"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MonsterBaseEventsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterBaseEvents";
	}
}
