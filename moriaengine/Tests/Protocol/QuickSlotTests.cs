using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's .setequip, .equip and .heal slots.</summary>
	[Collection(QuickSlotCollection.Name)]
	public class QuickSlotTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public QuickSlotTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private static void Expect(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		[Fact]
		public void SlotsRememberAndAct() {
			using var client = CommandPermissionTests.Connect("slottest", "Slot Tester");
			string ready = CommandPermissionTests.Ask(this.fixture, client, ".TestSlotsReady", "SLOTS_READY:");
			uint npc = uint.Parse(Regex.Match(ready, @"npc=#(\d+)").Groups[1].Value), item = uint.Parse(Regex.Match(ready, @"item=#(\d+)").Groups[1].Value);

			client.SendSpeech(".equip3");
			Expect(client, "Neni zaznamenan zadny predmet");
			client.SendSpeech(".heal3");
			Expect(client, "Nemas nastaveny equip3");
			client.SendSpeech(".samheal3");
			Expect(client, "Pouze saman vi jak vyuzit krvave bandaze");

			client.SendSpeech(".setequip3");
			Expect(client, "Zamer vec nebo tvora");
			client.SendTargetUid(item);
			Expect(client, "Predmet byl uspesne zaznamenan");
			client.SendSpeech(".heal3");
			Expect(client, "Cil ulozeny jako equip3 je predmet");
			client.SendSpeech(".equip3");
			Expect(client, "Predmet musis mit u sebe");

			client.SendSpeech(".setequip3");
			Thread.Sleep(300);
			client.SendTargetUid(npc);
			Expect(client, "Charakter byl uspesne zaznamenan");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class QuickSlotCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "QuickSlots";
	}
}
