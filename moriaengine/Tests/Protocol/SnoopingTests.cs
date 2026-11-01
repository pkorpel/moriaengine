using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Snooping beats the mark's Dexterity and may be seen, Stealing is off, as on Erebor.</summary>
	[Collection(SnoopingCollection.Name)]
	public class SnoopingTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public SnoopingTests(GameServerPairFixture servers) {
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

		[Fact]
		public void SnoopingAgainstDexterityAndNoStealing() {
			using var client = CommandPermissionTests.Connect("snooptest", "Snoop Tester");

			//no skill against 100 Dex: it fails, as a flat difficulty needn't
			string hard = this.Ask(client, ".TestSnoopReady \"0 100\"", "SNOOP_READY:");
			client.SendDoubleClick(uint.Parse(Regex.Match(hard, @"pack=(\d+)").Groups[1].Value));
			WaitForMessage(client, "Sacovani se nezdarilo");
			Assert.Contains("plugin=False", this.Ask(client, ".TestSnoopState", "SNOOP_STATE:"));

			//full skill against 1 Dex: the pack opens, the mark may notice, and it costs karma if so
			string easy = this.Ask(client, ".TestSnoopReady \"1000 1\"", "SNOOP_READY:");
			client.SendDoubleClick(uint.Parse(Regex.Match(easy, @"pack=(\d+)").Groups[1].Value));
			client.ReadPacketPayload(0x24, Wait);
			string state = this.Ask(client, ".TestSnoopState", "SNOOP_STATE:");
			Assert.Contains("plugin=True", state);
			Assert.Matches("karma=(0|-10)", state);

			//taking from it gets nowhere
			string steal = this.Ask(client, ".TestSnoopSteal", "SNOOP_STEAL:");
			Assert.Contains("success=False stillTheirs=True", steal);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SnoopingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Snooping";
	}
}
