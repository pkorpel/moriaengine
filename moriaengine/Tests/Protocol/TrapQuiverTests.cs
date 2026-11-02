using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's player trap and quiver.</summary>
	[Collection(TrapQuiverCollection.Name)]
	public class TrapQuiverTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public TrapQuiverTests(GameServerPairFixture servers) {
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
		public void ATrapBitesOnceAndAQuiverHoldsArrows() {
			using var client = CommandPermissionTests.Connect("traptest", "Trap Tester");
			string ready = this.Ask(client, ".TestTrapReady", "TRAP_READY:");

			client.SendDoubleClick(Uid(ready, "trap"));
			Expect(client, "Co chces vlozit do pasti");
			client.SendTargetUid(Uid(ready, "spare"));
			Expect(client, "Natahl jsi past");

			//it bites once
			Assert.DoesNotContain("lost=0", this.Ask(client, ".TestTrapStep", "TRAP_STEPPED:"));
			Assert.Contains("lost=0", this.Ask(client, ".TestTrapStep", "TRAP_STEPPED:"));

			client.SendDoubleClick(Uid(ready, "pack"));
			Thread.Sleep(300);
			client.SendDoubleClick(Uid(ready, "quiver"));
			var dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Toulec", dialog.Strings);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TrapQuiverCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TrapsAndQuivers";
	}
}
