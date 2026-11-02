using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - the necromancer's book and spells.</summary>
	[Collection(NecromancyCollection.Name)]
	public class NecromancyTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public NecromancyTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		private static uint Uid(string line, string name) {
			return uint.Parse(Regex.Match(line, name + @"=#(\d+)").Groups[1].Value);
		}

		[Fact]
		public void TheBookHoldsNecroSpellsAndTheyHit() {
			using var client = CommandPermissionTests.Connect("necrotest", "Necro Tester");
			string ready = this.Ask(client, ".TestNecroReady", "NECRO_READY:");
			Assert.Contains("has=False", ready);
			client.SendDoubleClick(Uid(ready, "pack"));
			Thread.Sleep(300);

			//a scroll dropped on the book teaches it
			foreach (string scroll in new[] { "firebolt", "frostbolt" }) {
				client.SendPickUpItem(Uid(ready, scroll), 1);
				Thread.Sleep(200);
				client.SendDropItem(Uid(ready, scroll), 0, 0, 0, (int) Uid(ready, "book"));
				Thread.Sleep(200);
			}
			string report = this.Ask(client, ".TestNecroReport", "NECRO_REPORT:");
			Assert.Contains("firebolt=True frostbolt=True", report);

			//it opens the necromancer's own page, its back arrow the plain book
			client.SendDoubleClick(Uid(ready, "book"));
			var dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Firebolt", dialog.Strings);
			Assert.Contains("Neni v knize", dialog.Strings);

			//a button casts, as does the command; a word of the spell then the cursor
			client.SendGumpResponse(dialog.GumpUid, 1);
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Thread.Sleep(3500);
			report = this.Ask(client, ".TestNecroReport", "NECRO_REPORT:");
			Assert.Matches(@"mana=(278|2[89]\d)", report);

			client.SendSpeech(".frostbolt");
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Thread.Sleep(2500);
			report = this.Ask(client, ".TestNecroReport", "NECRO_REPORT:");
			Assert.DoesNotContain("hits=5000", report);

			//a Mystic heals only himself
			this.Ask(client, ".TestNecroMakeMystic", "NECRO_MYSTIC:");
			this.Ask(client, ".TestNecroCast \"s_heal\"", "NECRO_CAST:");
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Expect(client, "Toto kouzlo muzes pouzit jen na sebe");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NecromancyCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Necromancy";
	}
}
