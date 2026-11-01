using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's lockpicking: a lock is a container's type, a pick wears out, the answer takes 2 s.</summary>
	[Collection(LockpickingCollection.Name)]
	public class LockpickingTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public LockpickingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static uint Uid(string report, string name) {
			return uint.Parse(Regex.Match(report, name + @"=(\d+)").Groups[1].Value);
		}

		private static void WaitForMessage(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		private uint Held(TestGameClient client) {
			return Uid(this.Ask(client, ".TestLockpickState", "LOCKPICK_STATE:"), "held");
		}

		private void Pick(TestGameClient client, uint target) {
			client.SendDoubleClick(this.Held(client));
			WaitForMessage(client, "Co chces odemknout");
			client.SendTargetUid(target);
		}

		[Fact]
		public void ALockHoldsUntilPicked() {
			using var client = CommandPermissionTests.Connect("lockpicktest", "Lock Tester");
			string setup = this.Ask(client, ".TestLockpickSetup", "LOCKPICK_SETUP:");
			uint locked = Uid(setup, "locked");
			client.SendDoubleClick(Uid(setup, "pack")); //a client can only reach into what it has open
			Thread.Sleep(300);

			//a lock keeps you out, Sphere's 501281
			client.SendDoubleClick(locked);
			Assert.Contains("00-07-A6-21", BitConverter.ToString(client.ReadPacketPayload(0xC1, Wait)));

			//a key lets you in
			this.Ask(client, ".TestLockpickKey", "LOCKPICK_KEY:");
			client.SendDoubleClick(locked);
			client.ReadPacketPayload(0x24, Wait);
			this.Ask(client, ".TestLockpickKey", "LOCKPICK_KEY:");

			this.Pick(client, Uid(setup, "plain"));
			WaitForMessage(client, "nelze pouzit paklic");
			this.Pick(client, Uid(setup, "faraway"));
			WaitForMessage(client, "moc daleko");
			Assert.Contains("i_lockpick_3=1", this.Ask(client, ".TestLockpickState", "LOCKPICK_STATE:"));

			//a try takes a use off the pick at once, the lock answers after 2 s, one job at a time
			this.Pick(client, locked);
			Thread.Sleep(300);
			Assert.Contains("i_lockpick=0 i_lockpick_2=1 i_lockpick_3=0", this.Ask(client, ".TestLockpickState", "LOCKPICK_STATE:"));
			this.Pick(client, locked);
			WaitForMessage(client, "trpelivost");
			WaitForMessage(client, "Zamek odolal");
			Assert.Contains("type=t_container_locked picked=False", this.Ask(client, ".TestLockpickState", "LOCKPICK_STATE:"));

			this.Ask(client, ".TestLockpickEasy", "LOCKPICK_EASY:");
			this.Pick(client, locked);
			WaitForMessage(client, "Odemkl jsi zamek");
			string state = this.Ask(client, ".TestLockpickState", "LOCKPICK_STATE:");
			Assert.Contains("i_lockpick=1 i_lockpick_2=0", state);
			Assert.Contains("type=t_container picked=True", state);

			//it's open now
			client.SendDoubleClick(locked);
			client.ReadPacketPayload(0x24, Wait);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class LockpickingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Lockpicking";
	}
}
